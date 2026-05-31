using System.Collections.ObjectModel;

namespace DivaModManager.Core.Models;

public sealed class GameConfig
{
    public string? Launcher { get; set; }
    public string? GamePath { get; set; }
    public bool LauncherOption { get; set; }
    public int LauncherOptionIndex { get; set; }
    public bool LauncherOptionConverted { get; set; }
    public bool FirstOpen { get; set; }
    public string? ModsFolder { get; set; }
    public string? ModLoaderVersion { get; set; }
    public string? CurrentLoadout { get; set; }
    public Dictionary<string, ObservableCollection<Mod>> Loadouts { get; set; } = new();
}

