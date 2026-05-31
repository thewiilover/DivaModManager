namespace DivaModManager.Core.Models;

public sealed class Config
{
    public string CurrentGame { get; set; } = string.Empty;
    public Dictionary<string, GameConfig> Configs { get; set; } = new();
    public double? LeftGridWidth { get; set; }
    public double? RightGridWidth { get; set; }
    public double? TopGridHeight { get; set; }
    public double? BottomGridHeight { get; set; }
    public double? Height { get; set; }
    public double? Width { get; set; }
    public bool Maximized { get; set; }
}
