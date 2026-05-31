using System;

namespace DivaModManager.Core.Models;

public enum InstalledModSourceKind
{
    Unknown,
    GameBanana,
    DivaModArchive
}

public sealed class InstalledModUpdateSource
{
    public string ModPath { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public Metadata? Metadata { get; set; }
    public InstalledModSourceKind Kind { get; set; }
    public string? GameBananaType { get; set; }
    public string? GameBananaId { get; set; }
    public int? DivaModArchivePostId { get; set; }
    public Uri? Homepage { get; set; }
}
