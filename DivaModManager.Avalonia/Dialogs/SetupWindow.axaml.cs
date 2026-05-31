  using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DivaModManager.Core.Models;
using System;
using System.Collections.ObjectModel;

namespace DivaModManager.Avalonia.Dialogs;

public partial class SetupWindow : Window
{
    private GameConfig? _gameConfig;
    private Config? _appConfig;

    public SetupWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialize the dialog with configuration
    /// </summary>
    public void Initialize(Config appConfig, GameConfig gameConfig)
    {
        _appConfig = appConfig;
        _gameConfig = gameConfig;

        // Load existing values
        if (!string.IsNullOrEmpty(gameConfig.GamePath))
        {
            if (this.FindControl<TextBox>("GamePathTextBox") is TextBox gamePath)
                gamePath.Text = gameConfig.GamePath;
        }

        if (!string.IsNullOrEmpty(gameConfig.ModsFolder))
        {
            if (this.FindControl<TextBox>("ModsFolderTextBox") is TextBox modsFolder)
                modsFolder.Text = gameConfig.ModsFolder;
        }

        if (!string.IsNullOrEmpty(gameConfig.ModLoaderVersion))
        {
            if (this.FindControl<TextBox>("ModLoaderVersionTextBox") is TextBox version)
                version.Text = gameConfig.ModLoaderVersion;
        }

        // Populate loadout combobox
        if (this.FindControl<ComboBox>("DefaultLoadoutComboBox") is ComboBox loadoutCombo)
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<string>(gameConfig.Loadouts.Keys);
            loadoutCombo.ItemsSource = items;
            if (!string.IsNullOrEmpty(gameConfig.CurrentLoadout))
                loadoutCombo.SelectedItem = gameConfig.CurrentLoadout;
        }

        // Wire up button handlers
        if (this.FindControl<Button>("BrowseGameButton") is Button browseGame)
            browseGame.Click += BrowseGamePath_Click;
        if (this.FindControl<Button>("BrowseModsButton") is Button browseMods)
            browseMods.Click += BrowseModsPath_Click;
        if (this.FindControl<Button>("SaveButton") is Button save)
            save.Click += Save_Click;
        if (this.FindControl<Button>("CancelButton") is Button cancel)
            cancel.Click += Cancel_Click;
    }

    private async void BrowseGamePath_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Game Executable",
                AllowMultiple = false,
                FileTypeFilter = new[] {
                    new FilePickerFileType("Executable") { Patterns = new[] { "*.exe" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });

            if (files.Count > 0)
            {
                var selectedPath = files[0].Path.LocalPath;
                if (this.FindControl<TextBox>("GamePathTextBox") is TextBox gamePath)
                    gamePath.Text = selectedPath;
                
                UpdateStatusMessage($"Game path selected: {Path.GetFileName(selectedPath)}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"Error selecting game path: {ex.Message}");
        }
    }

    private async void BrowseModsPath_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Mods Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var selectedPath = folders[0].Path.LocalPath;
                if (this.FindControl<TextBox>("ModsFolderTextBox") is TextBox modsFolder)
                    modsFolder.Text = selectedPath;

                UpdateStatusMessage($"Mods folder selected: {Path.GetFileName(selectedPath)}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"Error selecting mods folder: {ex.Message}");
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_gameConfig == null)
            {
                UpdateStatusMessage("Error: Game configuration not loaded");
                return;
            }

            var gamePath = this.FindControl<TextBox>("GamePathTextBox")?.Text;
            var modsFolder = this.FindControl<TextBox>("ModsFolderTextBox")?.Text;
            var version = this.FindControl<TextBox>("ModLoaderVersionTextBox")?.Text;
            var defaultLoadout = this.FindControl<ComboBox>("DefaultLoadoutComboBox")?.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(gamePath))
            {
                UpdateStatusMessage("Error: Invalid game executable path");
                return;
            }

            if (string.IsNullOrWhiteSpace(modsFolder) || !Directory.Exists(modsFolder))
            {
                UpdateStatusMessage("Error: Invalid mods folder path");
                return;
            }

            _gameConfig.GamePath = gamePath;
            _gameConfig.ModsFolder = modsFolder;
            _gameConfig.ModLoaderVersion = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version;
            _gameConfig.CurrentLoadout = string.IsNullOrWhiteSpace(defaultLoadout) ? "Default" : defaultLoadout;
            _gameConfig.FirstOpen = false;

            Close(true);
        }
        catch (Exception ex)
        {
            UpdateStatusMessage($"Error saving configuration: {ex.Message}");
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void UpdateStatusMessage(string message)
    {
        if (this.FindControl<TextBlock>("StatusTextBlock") is TextBlock status)
            status.Text = message;
    }
}
