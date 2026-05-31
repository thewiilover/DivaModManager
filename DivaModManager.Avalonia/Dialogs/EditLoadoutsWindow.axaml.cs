using Avalonia.Controls;
using Avalonia.Interactivity;
using DivaModManager.Core.Models;
using System.Collections.ObjectModel;

namespace DivaModManager.Avalonia.Dialogs;

public partial class EditLoadoutsWindow : Window
{
    private GameConfig? _gameConfig;
    private string? _currentLoadout;
    private Dictionary<string, ObservableCollection<Mod>> _loadouts = new();

    public EditLoadoutsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialize the dialog with game configuration
    /// </summary>
    public void Initialize(GameConfig gameConfig, string currentLoadout)
    {
        _gameConfig = gameConfig;
        _currentLoadout = currentLoadout;
        _loadouts = new Dictionary<string, ObservableCollection<Mod>>(gameConfig.Loadouts);

        RefreshLoadoutsList();
        SelectCurrentLoadout();

        // Wire up button handlers
        if (this.FindControl<Button>("NewLoadoutButton") is Button newBtn)
            newBtn.Click += NewLoadout_Click;
        if (this.FindControl<Button>("DeleteLoadoutButton") is Button delBtn)
            delBtn.Click += DeleteLoadout_Click;
        if (this.FindControl<Button>("ApplyButton") is Button applyBtn)
            applyBtn.Click += Apply_Click;
        if (this.FindControl<Button>("CancelButton") is Button cancelBtn)
            cancelBtn.Click += Cancel_Click;
        if (this.FindControl<Button>("CloseButton") is Button closeBtn)
            closeBtn.Click += Close_Click;
        if (this.FindControl<ComboBox>("LoadoutComboBox") is ComboBox loadoutCombo)
            loadoutCombo.SelectionChanged += LoadoutCombo_SelectionChanged;
    }

    private void RefreshLoadoutsList()
    {
        var comboBox = this.FindControl<ComboBox>("LoadoutComboBox");
        if (comboBox != null)
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<string>(_loadouts.Keys);
            comboBox.ItemsSource = items;
        }
    }

    private void SelectCurrentLoadout()
    {
        var comboBox = this.FindControl<ComboBox>("LoadoutComboBox");
        if (comboBox != null && !string.IsNullOrEmpty(_currentLoadout))
        {
            comboBox.SelectedItem = _currentLoadout;
        }
    }

    private void LoadoutCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var comboBox = this.FindControl<ComboBox>("LoadoutComboBox");
        if (comboBox?.SelectedItem is string selectedLoadout && _loadouts.ContainsKey(selectedLoadout))
        {
            var listBox = this.FindControl<ListBox>("ModListBox");
            if (listBox != null)
            {
                listBox.ItemsSource = _loadouts[selectedLoadout];
            }
        }
    }

    private async void NewLoadout_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var inputDialog = new TextInputDialog("New Loadout", "Enter loadout name:");
            await inputDialog.ShowDialog(this);

            string? newName = inputDialog.Result;
            if (!string.IsNullOrWhiteSpace(newName) && !_loadouts.ContainsKey(newName))
            {
                _loadouts[newName] = new ObservableCollection<Mod>();
                RefreshLoadoutsList();
                System.Console.WriteLine($"Created new loadout: {newName}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating loadout: {ex.Message}");
        }
    }

    private void DeleteLoadout_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var comboBox = this.FindControl<ComboBox>("LoadoutComboBox");
            if (comboBox?.SelectedItem is string selectedLoadout && selectedLoadout != _currentLoadout)
            {
                if (_loadouts.Remove(selectedLoadout))
                {
                    RefreshLoadoutsList();
                    System.Console.WriteLine($"Deleted loadout: {selectedLoadout}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error deleting loadout: {ex.Message}");
        }
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_gameConfig != null)
            {
                _gameConfig.Loadouts = _loadouts;
                System.Console.WriteLine("Loadouts applied successfully");
                Close();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error applying loadouts: {ex.Message}");
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
