using System.Text.Json;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class InstalledModScanner
{
    public IReadOnlyList<InstalledModUpdateSource> Scan(string modsFolder)
    {
        var result = new List<InstalledModUpdateSource>();
        if (string.IsNullOrWhiteSpace(modsFolder) || !Directory.Exists(modsFolder))
        {
            return result;
        }

        foreach (var modPath in Directory.GetDirectories(modsFolder))
        {
            var metadataPath = Path.Combine(modPath, "mod.json");
            Metadata? metadata = null;
            if (File.Exists(metadataPath))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metadataPath));
                }
                catch
                {
                    // Ignore malformed mod.json
                }
            }

            var updateSource = new InstalledModUpdateSource
            {
                ModPath = modPath,
                ModName = Path.GetFileName(modPath),
                Metadata = metadata,
                Homepage = metadata?.homepage
            };

            if (metadata?.homepage != null && TryParseGameBanana(metadata.homepage, out var type, out var id))
            {
                updateSource.Kind = InstalledModSourceKind.GameBanana;
                updateSource.GameBananaType = type;
                updateSource.GameBananaId = id;
            }
            else if (metadata?.id != null)
            {
                updateSource.Kind = InstalledModSourceKind.DivaModArchive;
                updateSource.DivaModArchivePostId = metadata.id;
            }

            result.Add(updateSource);
        }

        return result;
    }

    private static bool TryParseGameBanana(Uri homepage, out string type, out string id)
    {
        type = string.Empty;
        id = string.Empty;

        if (!string.Equals(homepage.Host, "gamebanana.com", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(homepage.Host, "www.gamebanana.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = homepage.Segments;
        if (segments.Length != 3)
        {
            return false;
        }

        var rawType = segments[1].Trim('/');
        if (rawType.Length < 2)
        {
            return false;
        }

        type = char.ToUpperInvariant(rawType[0]) + rawType[1..^1];
        id = segments[2].Trim('/');
        return !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(id);
    }
}