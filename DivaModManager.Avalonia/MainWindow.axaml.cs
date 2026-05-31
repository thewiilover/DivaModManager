using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DivaModManager.Avalonia.ViewModels;
using DivaModManager.Avalonia.Dialogs;
using DivaModManager.Core.Models;
using DivaModManager.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace DivaModManager.Avalonia;

public sealed partial class MainWindow : Window
{
    private Mod? _draggedMod;
    private Point _dragStart;
    private MainWindowViewModel? _viewModel;
    private Config? _appConfig;

    public MainWindow()
    {
        InitializeComponent();
        var modGrid = this.FindControl<DataGrid>("ModGrid");
        if (modGrid != null)
        {
            modGrid.AddHandler(DragDrop.DragOverEvent, ModGrid_DragOver);
            modGrid.AddHandler(DragDrop.DropEvent, ModGrid_Drop);
            modGrid.AddHandler(PointerPressedEvent, ModGrid_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            modGrid.AddHandler(PointerMovedEvent, ModGrid_PointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        }

        Loaded += (sender, e) => OnLoaded((object)sender, e);
    }

    private void OnLoaded(object _, RoutedEventArgs __)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            _viewModel = vm;
            System.Console.WriteLine($"[DEBUG] MainWindow Loaded. Mods count: {vm.Mods.Count}");
            foreach (var mod in vm.Mods)
            {
                System.Console.WriteLine($"[DEBUG] Loaded mod: {mod.name} (enabled: {mod.enabled})");
            }
        }

        // Attach event handlers to tab items
        var tabControl = this.FindControl<TabControl>("TabControl");
        
        if (tabControl != null)
        {
            tabControl.SelectionChanged += OnTabSelectionChanged;
        }
    }

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var tabControl = this.FindControl<TabControl>("TabControl");
        if (tabControl?.SelectedItem is TabItem selectedTab && selectedTab.Header is string header)
        {
            if (header == "GameBanana Mods")
            {
                _viewModel?.LoadGameBananaMods();
            }
            else if (header == "DMA Mods")
            {
                _viewModel?.LoadDMAMods();
            }
        }
    }

    private void ModGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        // If clicking on a checkbox or a control that handles its own input, don't start drag
        if (e.Source is Control source && source.GetVisualAncestors().Any(a => a is CheckBox || a is Button))
        {
            return;
        }

        _dragStart = e.GetPosition(control);
        _draggedMod = GetRowMod(e.Source as Control);
        
        if (_draggedMod != null)
        {
            System.Console.WriteLine($"[DEBUG] PointerPressed on mod: {_draggedMod.name}");
        }
    }

    private async void ModGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedMod == null || sender is not Control control)
        {
            return;
        }

        var pos = e.GetPosition(control);
        if (Math.Abs(pos.Y - _dragStart.Y) < 5)
        {
            return;
        }

        var data = new DataObject();
        data.Set("mod", _draggedMod);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        _draggedMod = null;
    }

    private void ModGrid_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("mod"))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void ModGrid_Drop(object? sender, DragEventArgs e)
    {
        var dragged = e.Data.Contains("mod") ? e.Data.Get("mod") as Mod : null;
        var target = GetRowMod(e.Source as Control);

        if (target == null && sender is DataGrid grid)
        {
            // Try to find target from position if e.Source wasn't inside a row
            var pos = e.GetPosition(grid);
            // This is a bit more complex in Avalonia, but let's try hit testing
            var visual = grid.InputHitTest(pos) as Control;
            target = GetRowMod(visual);
        }

        System.Console.WriteLine($"[DEBUG] Drop: dragged={(dragged != null ? dragged.name : "null")}, target={(target != null ? target.name : "null")}");

        if (dragged == null || target == null || ReferenceEquals(dragged, target))
        {
            return;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            System.Console.WriteLine($"[DEBUG] Moving mod {dragged.name} to {target.name}");
            if (ModListReorder.MoveItem(vm.Mods, dragged, target))
            {
                System.Console.WriteLine($"[DEBUG] Move successful. New first mod: {vm.Mods[0].name}");
            }
            else
            {
                System.Console.WriteLine($"[DEBUG] Move failed in ModListReorder");
            }
        }
    }

    private static Mod? GetRowMod(Control? source)
    {
        var row = source?.GetVisualAncestors().OfType<DataGridRow>().FirstOrDefault();
        return row?.DataContext as Mod;
    }

    // Button Click Handlers
    private async void EditLoadouts_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var gameConfig = GetCurrentGameConfig();
            if (gameConfig == null)
            {
                ShowErrorMessage("Game configuration not found. Please run Setup first.");
                return;
            }

            var dialog = new EditLoadoutsWindow();
            dialog.Initialize(gameConfig, gameConfig.CurrentLoadout ?? "Default");
            await dialog.ShowDialog(this);
            SaveAppConfig();
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error opening Edit Loadouts: {ex.Message}");
        }
    }

    private async void Setup_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var appConfig = LoadAppConfig();
            var gameConfig = GetCurrentGameConfig();

            if (appConfig == null || gameConfig == null)
            {
                ShowErrorMessage("Failed to load configuration");
                return;
            }

            var dialog = new SetupWindow();
            dialog.Initialize(appConfig, gameConfig);
            var saved = await dialog.ShowDialog<bool>(this);

            if (saved)
            {
                SaveAppConfig();
                _viewModel?.LoadInstalledMods();
                ShowInfoMessage("Setup saved.");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error opening Setup: {ex.Message}");
        }
    }

    private async void Launch_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Mods == null || _viewModel.Mods.Count == 0)
            {
                ShowErrorMessage("No mods loaded. Please load mods first.");
                return;
            }

            var gameConfig = GetCurrentGameConfig();
            if (gameConfig == null)
            {
                ShowErrorMessage("Game configuration not found. Please run Setup first.");
                return;
            }

            // Rebuild mod config before launching
            var enabledMods = _viewModel.Mods.ToList();
            bool buildSuccess = ModLoader.Build(gameConfig.ModsFolder ?? "", enabledMods, 
                _viewModel.SelectedGame, _viewModel.SelectedLoadout, _viewModel.SelectedLaunchMethod, _viewModel.Loadouts.ToList());
            
            if (!buildSuccess)
            {
                ShowErrorMessage("Failed to rebuild mod configuration.");
                return;
            }

            ShowInfoMessage("Mod configuration rebuilt successfully!");

            // Determine launch method
            var launchMethod = _viewModel?.SelectedLaunchMethod == "Steam" ? 
                LaunchMethod.Steam : LaunchMethod.Executable;

            // Launch game
            bool launchSuccess = await GameLauncher.LaunchGameAsync(gameConfig, launchMethod);
            if (launchSuccess)
            {
                ShowInfoMessage($"Game launched successfully via {launchMethod}!");
            }
            else
            {
                ShowErrorMessage($"Failed to launch game via {launchMethod}");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error launching game: {ex.Message}");
        }
    }

    private void CreateMod_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Open mod creation dialog
            var message = "Create Mod feature coming soon!";
            ShowInfoMessage(message);
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error creating mod: {ex.Message}");
        }
    }

    private async void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel?.Mods == null || _viewModel.Mods.Count == 0)
            {
                ShowErrorMessage("No mods loaded. Please load mods first.");
                return;
            }

            var gameConfig = GetCurrentGameConfig();
            if (gameConfig == null)
            {
                ShowErrorMessage("Game configuration not found. Please run Setup first.");
                return;
            }

            // Rebuild mod config
            var enabledMods = _viewModel.Mods.ToList();
            bool success = ModLoader.Build(gameConfig.ModsFolder ?? "", enabledMods,
                _viewModel?.SelectedGame, _viewModel?.SelectedLoadout, _viewModel?.SelectedLaunchMethod, _viewModel?.Loadouts.ToList());
            if (success)
            {
                ShowInfoMessage("Mods updated and configuration rebuilt successfully!");
            }
            else
            {
                ShowErrorMessage("Failed to rebuild mod configuration.");
            }

            // Implement actual mod update checking from GameBanana/DMA
            ShowInfoMessage("Checking for mod updates...");
            var updater = new ModUpdateService();
            var updates = await updater.CheckForUpdatesAsync(gameConfig.ModsFolder ?? "");
            
            if (updates.Count > 0)
            {
                ShowInfoMessage($"Updates available for: {string.Join(", ", updates)}");
            }
            else
            {
                ShowInfoMessage("No mod updates available.");
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error updating mods: {ex.Message}");
        }
    }

    private void GameBananaSearch_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.LoadGameBananaMods();
    }

    private void DMASearch_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.LoadDMAMods();
    }

    private void ConfigureMod_Click(object? sender, RoutedEventArgs e)
    {
        var mod = GetSelectedMod();
        if (mod == null) return;
        ShowInfoMessage($"Configure mod: {mod.name}");
        // TODO: Implement ConfigureModWindow
    }

    private async void RenameMod_Click(object? sender, RoutedEventArgs e)
    {
        var mod = GetSelectedMod();
        if (mod == null) return;

        var dialog = new TextInputDialog("Rename Mod Folder", "Enter new folder name:");
        if (await dialog.ShowDialog<bool>(this))
        {
            var newName = dialog.Result;
            if (!string.IsNullOrWhiteSpace(newName) && newName != mod.name)
            {
                var modsFolder = GetModsFolderPath();
                var oldPath = Path.Combine(modsFolder, mod.name);
                var newPath = Path.Combine(modsFolder, newName);

                if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
                {
                    try
                    {
                        Directory.Move(oldPath, newPath);
                        mod.name = newName;
                        _viewModel?.LoadInstalledMods();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage($"Failed to rename: {ex.Message}");
                    }
                }
            }
        }
    }

    private void OpenModFolder_Click(object? sender, RoutedEventArgs e)
    {
        var mod = GetSelectedMod();
        if (mod == null) return;

        var modsFolder = GetModsFolderPath();
        var folderPath = Path.Combine(modsFolder, mod.name);
        if (Directory.Exists(folderPath))
        {
            try
            {
                OpenPath(folderPath);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to open folder: {ex.Message}");
            }
        }
    }

    private async void FetchMetadata_Click(object? sender, RoutedEventArgs e)
    {
        var mod = GetSelectedMod();
        if (mod == null) return;
        
        var dialog = new TextInputDialog("Fetch Metadata", "Enter GameBanana or DMA URL:");
        if (await dialog.ShowDialog<bool>(this))
        {
            var url = dialog.Result;
            if (string.IsNullOrWhiteSpace(url)) return;

            var modsFolder = GetModsFolderPath();
            var modPath = Path.Combine(modsFolder, mod.name);
            
            ShowInfoMessage($"Fetching metadata for: {mod.name}");
            var fetcher = new MetadataFetchingService();
            
            var metadata = await fetcher.FetchFromGameBananaUrlAsync(url) 
                          ?? await fetcher.FetchFromDivaModArchiveUrlAsync(url);

            if (metadata != null)
            {
                var installer = new ModInstallerService();
                installer.SaveMetadata(modPath, metadata);
                ShowInfoMessage("Metadata fetched and saved.");
            }
            else
            {
                ShowErrorMessage("Could not fetch metadata from the provided URL.");
            }
        }
    }

    private void MoreInfo_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBananaRecord record)
        {
            if (record.Link != null)
            {
                try
                {
                    OpenPath(record.Link.ToString());
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Failed to open link: {ex.Message}");
                }
            }
        }
    }

    private async void InstallGameBanana_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBananaRecord record)
        {
            ShowInfoMessage($"Downloading and installing: {record.Title}");
            try
            {
                await _viewModel!.DownloadAndInstallGameBananaModAsync(record);
                ShowInfoMessage($"Successfully installed: {record.Title}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to install mod: {ex.Message}");
            }
        }
    }

    private async void InstallDMA_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is DivaModArchivePost post)
        {
            ShowInfoMessage($"Downloading and installing: {post.Name}");
            try
            {
                await _viewModel!.DownloadAndInstallDMAModAsync(post);
                ShowInfoMessage($"Successfully installed: {post.Name}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to install mod: {ex.Message}");
            }
        }
    }

    private async void DeleteMod_Click(object? sender, RoutedEventArgs e)
    {
        var mod = GetSelectedMod();
        if (mod == null) return;

        var modsFolder = GetModsFolderPath();
        var folderPath = Path.Combine(modsFolder, mod.name);
        if (Directory.Exists(folderPath))
        {
            try
            {
                await Task.Run(() => Directory.Delete(folderPath, true));
                _viewModel?.Mods.Remove(mod);
                ShowInfoMessage($"Deleted mod: {mod.name}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Failed to delete mod: {ex.Message}");
            }
        }
    }

    private Mod? GetSelectedMod()
    {
        var modGrid = this.FindControl<DataGrid>("ModGrid");
        return modGrid?.SelectedItem as Mod;
    }

    private string GetModsFolderPath()
    {
        var gameConfig = GetCurrentGameConfig();
        if (gameConfig?.ModsFolder != null)
        {
            return gameConfig.ModsFolder;
        }
        
        // Fallback to default path
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DivaModManager", "mods");
    }

    private GameConfig? GetCurrentGameConfig()
    {
        var appConfig = LoadAppConfig();
        if (appConfig == null)
        {
            return null;
        }

        // Initialize default game entry if missing.
        if (!appConfig.Configs.TryGetValue(appConfig.CurrentGame, out var gameConfig))
        {
            gameConfig = new GameConfig
            {
                CurrentLoadout = "Default",
                ModsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DivaModManager", "mods"),
                Loadouts = new Dictionary<string, ObservableCollection<Mod>>
                {
                    { "Default", _viewModel?.Mods ?? new ObservableCollection<Mod>() }
                }
            };
            appConfig.Configs[appConfig.CurrentGame] = gameConfig;
        }

        if (string.IsNullOrWhiteSpace(gameConfig.CurrentLoadout))
        {
            gameConfig.CurrentLoadout = "Default";
        }

        if (!gameConfig.Loadouts.ContainsKey(gameConfig.CurrentLoadout))
        {
            gameConfig.Loadouts[gameConfig.CurrentLoadout] = _viewModel?.Mods ?? new ObservableCollection<Mod>();
        }

        return gameConfig;
    }

    private Config? LoadAppConfig()
    {
        if (_appConfig != null)
        {
            return _appConfig;
        }

        var configPath = GetConfigPath();

        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                _appConfig = JsonSerializer.Deserialize<Config>(json);
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Failed reading config: {ex.Message}");
        }

        _appConfig ??= new Config
        {
            CurrentGame = "Project DIVA Mega Mix+",
            Configs = new Dictionary<string, GameConfig>()
        };

        if (string.IsNullOrWhiteSpace(_appConfig.CurrentGame))
        {
            _appConfig.CurrentGame = "Project DIVA Mega Mix+";
        }

        return _appConfig;
    }

    private void SaveAppConfig()
    {
        if (_appConfig == null)
        {
            return;
        }

        var configPath = GetConfigPath();
        try
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_appConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Failed saving config: {ex.Message}");
        }
    }

    private static string GetConfigPath()
    {
        // Use app-local config in dev/build runs and maintain WPF-compatible filename.
        return Path.Combine(AppContext.BaseDirectory, "Config.json");
    }

    private void ShowInfoMessage(string message)
    {
        // For now, we'll just print to console
        System.Console.WriteLine($"[INFO] {message}");
    }

    private void ShowErrorMessage(string message)
    {
        // For now, we'll just print to console
        System.Console.WriteLine($"[ERROR] {message}");
    }

    private void OpenPath(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                ArgumentList = { path },
                UseShellExecute = false
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}


