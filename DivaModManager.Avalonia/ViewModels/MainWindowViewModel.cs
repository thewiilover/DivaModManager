using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Net.Http;
using Tomlyn;
using Tomlyn.Model;
using DivaModManager.Core.Models;
using DivaModManager.Core.Services;

namespace DivaModManager.Avalonia.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly BrowserFeedService _browserFeedService = new();

    public ObservableCollection<Mod> Mods { get; } = new();
    public ObservableCollection<string> Games { get; } = new() { "Project DIVA Mega Mix+" };
    public ObservableCollection<string> Loadouts { get; } = new() { "Default" };
    public ObservableCollection<string> LaunchMethods { get; } = new() { "Executable", "Steam" };

    private string _selectedGame = "Project DIVA Mega Mix+";
    public string SelectedGame
    {
        get => _selectedGame;
        set
        {
            if (_selectedGame != value)
            {
                _selectedGame = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    private string _selectedLoadout = "Default";
    public string SelectedLoadout
    {
        get => _selectedLoadout;
        set
        {
            if (_selectedLoadout != value)
            {
                _selectedLoadout = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    private string _selectedLaunchMethod = "Executable";
    public string SelectedLaunchMethod
    {
        get => _selectedLaunchMethod;
        set
        {
            if (_selectedLaunchMethod != value)
            {
                _selectedLaunchMethod = value;
                OnPropertyChanged();
                SaveConfig();
            }
        }
    }

    private Mod? _selectedMod;
    public Mod? SelectedMod
    {
        get => _selectedMod;
        set
        {
            if (!ReferenceEquals(_selectedMod, value))
            {
                _selectedMod = value;
                OnPropertyChanged();
            }
        }
    }

    private string _statsText = string.Empty;
    public string StatsText
    {
        get => _statsText;
        set
        {
            if (_statsText != value)
            {
                _statsText = value;
                OnPropertyChanged();
            }
        }
    }

    private ObservableCollection<GameBananaRecord>? _gameBananaMods;
    public ObservableCollection<GameBananaRecord>? GameBananaMods
    {
        get => _gameBananaMods;
        set
        {
            if (!ReferenceEquals(_gameBananaMods, value))
            {
                _gameBananaMods = value;
                OnPropertyChanged();
            }
        }
    }

    private ObservableCollection<DivaModArchivePost>? _dmaMods;
    public ObservableCollection<DivaModArchivePost>? DMAPosts
    {
        get => _dmaMods;
        set
        {
            if (!ReferenceEquals(_dmaMods, value))
            {
                _dmaMods = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLoadingGameBanana;
    public bool IsLoadingGameBanana
    {
        get => _isLoadingGameBanana;
        set
        {
            if (_isLoadingGameBanana != value)
            {
                _isLoadingGameBanana = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLoadingDMA;
    public bool IsLoadingDMA
    {
        get => _isLoadingDMA;
        set
        {
            if (_isLoadingDMA != value)
            {
                _isLoadingDMA = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _gameBananaError;
    public string? GameBananaError
    {
        get => _gameBananaError;
        set
        {
            if (_gameBananaError != value)
            {
                _gameBananaError = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _dmaError;
    public string? DMAError
    {
        get => _dmaError;
        set
        {
            if (_dmaError != value)
            {
                _dmaError = value;
                OnPropertyChanged();
            }
        }
    }

    private string _gameBananaSearchText = string.Empty;
    public string GameBananaSearchText
    {
        get => _gameBananaSearchText;
        set
        {
            if (_gameBananaSearchText != value)
            {
                _gameBananaSearchText = value;
                OnPropertyChanged();
            }
        }
    }

    private GameBananaFeedFilter _selectedGameBananaFilter = GameBananaFeedFilter.Recent;
    public GameBananaFeedFilter SelectedGameBananaFilter
    {
        get => _selectedGameBananaFilter;
        set
        {
            if (_selectedGameBananaFilter != value)
            {
                _selectedGameBananaFilter = value;
                OnPropertyChanged();
            }
        }
    }

    private GameBananaFeedType _selectedGameBananaType = GameBananaFeedType.Mods;
    public GameBananaFeedType SelectedGameBananaType
    {
        get => _selectedGameBananaType;
        set
        {
            if (_selectedGameBananaType != value)
            {
                _selectedGameBananaType = value;
                OnPropertyChanged();
            }
        }
    }

    private string _dmaSearchText = string.Empty;
    public string DMASearchText
    {
        get => _dmaSearchText;
        set
        {
            if (_dmaSearchText != value)
            {
                _dmaSearchText = value;
                OnPropertyChanged();
            }
        }
    }

    private DMAFeedSort _selectedDMASort = DMAFeedSort.Latest;
    public DMAFeedSort SelectedDMASort
    {
        get => _selectedDMASort;
        set
        {
            if (_selectedDMASort != value)
            {
                _selectedDMASort = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLoadingMods;

    public MainWindowViewModel()
    {
        Mods.CollectionChanged += ModsOnCollectionChanged;
        LoadInstalledMods();
    }

    public void LoadInstalledMods()
    {
        _isLoadingMods = true;
        try
        {
            var modsFolder = GetModsFolderPath();
            
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                UpdateStats();
                return;
            }

            // Load priority list and other settings from config.toml
            var normalizedModsFolder = modsFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var configPath = Path.Combine(Path.GetDirectoryName(normalizedModsFolder) ?? normalizedModsFolder, "config.toml");
            var priorityList = new List<string>();
            var disabledList = new List<string>();
            if (File.Exists(configPath))
            {
                try
                {
                    var configString = File.ReadAllText(configPath);
                    if (Toml.TryToModel(configString, out TomlTable? config, out _))
                    {
                        if (config.TryGetValue("priority", out var priorityObj) && priorityObj is TomlArray priorityArray)
                        {
                            foreach (var item in priorityArray)
                            {
                                if (item is string s) priorityList.Add(s);
                            }
                        }

                        if (config.TryGetValue("disabled", out var disabledObj) && disabledObj is TomlArray disabledArray)
                        {
                            foreach (var item in disabledArray)
                            {
                                if (item is string s) disabledList.Add(s);
                            }
                        }

                        if (config.TryGetValue("loadouts", out var loadoutsObj) && loadoutsObj is TomlArray loadoutsArray)
                        {
                            Loadouts.Clear();
                            foreach (var item in loadoutsArray)
                            {
                                if (item is string s) Loadouts.Add(s);
                            }
                            if (Loadouts.Count == 0) Loadouts.Add("Default");
                        }

                        if (config.TryGetValue("selected_game", out var gameObj) && gameObj is string game)
                        {
                            _selectedGame = game;
                            OnPropertyChanged(nameof(SelectedGame));
                        }

                        if (config.TryGetValue("selected_loadout", out var loadoutObj) && loadoutObj is string loadout)
                        {
                            _selectedLoadout = loadout;
                            OnPropertyChanged(nameof(SelectedLoadout));
                        }

                        if (config.TryGetValue("selected_launch_method", out var methodObj) && methodObj is string method)
                        {
                            _selectedLaunchMethod = method;
                            OnPropertyChanged(nameof(SelectedLaunchMethod));
                        }
                    }
                }
                catch { }
            }

            var scanner = new InstalledModScanner();
            var installedMods = scanner.Scan(modsFolder).ToList();

            foreach (var mod in Mods)
            {
                mod.PropertyChanged -= Mod_PropertyChanged;
            }
            Mods.Clear();
            
            bool discoveredNewMods = false;
            // First, add mods in the order they appear in the priority list
            foreach (var modName in priorityList)
            {
                var installedMod = installedMods.FirstOrDefault(m => m.ModName.Equals(modName, StringComparison.OrdinalIgnoreCase));
                if (installedMod != null)
                {
                    Mods.Add(new Mod
                    {
                        name = installedMod.ModName,
                        enabled = true
                    });
                    installedMods.Remove(installedMod);
                }
            }

            // Then add remaining mods (either new or explicitly disabled)
            foreach (var installedMod in installedMods)
            {
                bool isEnabled = true;
                if (disabledList.Any(d => d.Equals(installedMod.ModName, StringComparison.OrdinalIgnoreCase)))
                {
                    isEnabled = false;
                }
                else
                {
                    // New mod discovered that wasn't in priority or disabled list
                    discoveredNewMods = true;
                }

                Mods.Add(new Mod
                {
                    name = installedMod.ModName,
                    enabled = isEnabled
                });
            }

            UpdateStats();
            
            if (discoveredNewMods)
            {
                SaveConfig();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] Error loading installed mods: {ex.Message}");
            UpdateStats();
        }
        finally
        {
            _isLoadingMods = false;
        }
    }

    private string GetModsFolderPath()
    {
        var configPath = GetConfigPath();
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var appConfig = JsonSerializer.Deserialize<Config>(json);
                if (appConfig?.Configs.TryGetValue(appConfig.CurrentGame, out var gameConfig) == true
                    && !string.IsNullOrWhiteSpace(gameConfig.ModsFolder))
                {
                    return gameConfig.ModsFolder;
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DivaModManager", "mods");
    }

    private static string GetConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Config.json");
    }

    public async void LoadGameBananaMods()
    {
        await LoadGameBananaModsAsync();
    }

    public async Task LoadGameBananaModsAsync()
    {
        IsLoadingGameBanana = true;
        GameBananaError = null;

        try
        {
            await _browserFeedService.GetGameBananaFeed(
                page: 1,
                type: SelectedGameBananaType,
                filter: SelectedGameBananaFilter,
                categoryId: null,
                subcategoryId: null,
                perPage: 20,
                nsfw: false,
                search: string.IsNullOrWhiteSpace(GameBananaSearchText) ? null : GameBananaSearchText
            );

            if (_browserFeedService.HasError)
            {
                GameBananaError = $"Error loading GameBanana mods: {_browserFeedService.LastError?.Message}";
            }
            else
            {
                GameBananaMods = _browserFeedService.CurrentGameBananaFeed?.Records;
            }
        }
        catch (Exception e)
        {
            GameBananaError = $"Error: {e.Message}";
        }
        finally
        {
            IsLoadingGameBanana = false;
        }
    }

    public async void LoadDMAMods()
    {
        await LoadDMAModsAsync();
    }

    public async Task LoadDMAModsAsync()
    {
        IsLoadingDMA = true;
        DMAError = null;

        try
        {
            await _browserFeedService.GetDMAFeed(
                page: 1,
                sort: SelectedDMASort,
                filter: DMAFeedFilter.None,
                search: string.IsNullOrWhiteSpace(DMASearchText) ? null : DMASearchText,
                limit: 20
            );

            if (_browserFeedService.HasError)
            {
                DMAError = $"Error loading DMA mods: {_browserFeedService.LastError?.Message}";
            }
            else
            {
                DMAPosts = _browserFeedService.CurrentDMAFeed?.Posts;
            }
        }
        catch (Exception e)
        {
            DMAError = $"Error: {e.Message}";
        }
        finally
        {
            IsLoadingDMA = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void ModsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (Mod item in e.NewItems)
                        item.PropertyChanged += Mod_PropertyChanged;
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (Mod item in e.OldItems)
                        item.PropertyChanged -= Mod_PropertyChanged;
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                {
                    foreach (Mod item in e.OldItems)
                        item.PropertyChanged -= Mod_PropertyChanged;
                }
                if (e.NewItems != null)
                {
                    foreach (Mod item in e.NewItems)
                        item.PropertyChanged += Mod_PropertyChanged;
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                // We handle reset (Clear) manually in LoadInstalledMods
                break;
            case NotifyCollectionChangedAction.Move:
                // For Move, NewItems and OldItems are the same, so we don't need to change subscriptions
                break;
        }

        UpdateStats();

        if (!_isLoadingMods && e.Action != NotifyCollectionChangedAction.Reset)
        {
            SaveConfig();
        }
    }

    private void Mod_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isLoadingMods && e.PropertyName == nameof(Mod.enabled))
        {
            SaveConfig();
        }
    }

    private void SaveConfig()
    {
        var modsFolder = GetModsFolderPath();
        ModLoader.Build(modsFolder, Mods.ToList(), SelectedGame, SelectedLoadout, SelectedLaunchMethod, Loadouts.ToList());
    }

    public async Task DownloadAndInstallGameBananaModAsync(GameBananaRecord record)
    {
        if (record.AllFiles == null || record.AllFiles.Count == 0) return;

        var file = record.AllFiles[0];
        var modsFolder = GetModsFolderPath();
        var tempFile = Path.Combine(Path.GetTempPath(), file.FileName);

        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(file.DownloadUrl);
            await File.WriteAllBytesAsync(tempFile, data);

            var installer = new ModInstallerService();
            var metadata = new Metadata
            {
                description = record.Description,
                homepage = record.Link,
                lastupdate = record.DateUpdated
            };
            installer.InstallMod(tempFile, modsFolder, metadata);
            LoadInstalledMods();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    public async Task DownloadAndInstallDMAModAsync(DivaModArchivePost post)
    {
        if (post.Files == null || post.Files.Count == 0) return;

        var url = post.Files[0];
        var fileName = post.FileNames?.FirstOrDefault() ?? "mod.zip";
        var modsFolder = GetModsFolderPath();
        var tempFile = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(tempFile, data);

            var installer = new ModInstallerService();
            var metadata = new Metadata
            {
                description = post.Text,
                id = post.ID,
                lastupdate = post.Time
            };
            installer.InstallMod(tempFile, modsFolder, metadata);
            LoadInstalledMods();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private void UpdateStats()
    {
        StatsText = $"{Mods.Count} mods";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

