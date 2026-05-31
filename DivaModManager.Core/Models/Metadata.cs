using System;

namespace DivaModManager.Core.Models;

public sealed class Metadata
{
    public int? id { get; set; }
    public Uri? preview { get; set; }
    public string? submitter { get; set; }
    public Uri? avi { get; set; }
    public Uri? upic { get; set; }
    public Uri? caticon { get; set; }
    public string? cat { get; set; }
    public string? description { get; set; }
    public Uri? homepage { get; set; }
    public DateTime? lastupdate { get; set; }
}
