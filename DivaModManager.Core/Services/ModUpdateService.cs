using System.Text.Json;
using DivaModManager.Core.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace DivaModManager.Core.Services;

public sealed class ModUpdateService
{
    private readonly InstalledModScanner _scanner = new();
    private readonly UpdateRequestBuilder _requestBuilder = new();
    private readonly ArchiveExtractionService _archiveExtractor = new();

    public IReadOnlyList<InstalledModUpdateSource> ScanInstalledMods(string modsFolder)
        => _scanner.Scan(modsFolder);

    public Dictionary<string, List<string>> BuildGameBananaRequests(string modsFolder)
        => _requestBuilder.BuildGameBananaRequests(_scanner.Scan(modsFolder));

    public string BuildDivaModArchiveRequest(string modsFolder)
        => _requestBuilder.BuildDivaModArchiveRequest(_scanner.Scan(modsFolder));

    public void ExtractDownloadedArchive(string archivePath, string outputDirectory)
        => _archiveExtractor.ExtractArchive(archivePath, outputDirectory);

    public async Task<List<string>> CheckForUpdatesAsync(string modsFolder)
    {
        var sources = _scanner.Scan(modsFolder);
        var gbRequests = _requestBuilder.BuildGameBananaRequests(sources);
        var dmaRequest = _requestBuilder.BuildDivaModArchiveRequest(sources);
        
        var modsWithUpdates = new List<string>();
        using var client = new HttpClient();
        
        // GameBanana
        foreach (var typeGroup in gbRequests)
        {
            foreach (var url in typeGroup.Value)
            {
                try 
                {
                    var responseString = await client.GetStringAsync(url);
                    var records = JsonSerializer.Deserialize<List<GameBananaAPIV4>>(responseString);
                    if (records != null)
                    {
                        foreach (var record in records)
                        {
                            var source = sources.FirstOrDefault(x => x.GameBananaId == record.Link?.Segments.LastOrDefault()?.Trim('/'));
                            if (source != null && record.DateUpdated > source.Metadata?.lastupdate)
                            {
                                modsWithUpdates.Add(source.ModName);
                            }
                        }
                    }
                }
                catch { /* Ignore errors for individual requests */ }
            }
        }

        // DMA
        if (dmaRequest.Contains("post_id="))
        {
            try
            {
                var responseString = await client.GetStringAsync(dmaRequest);
                // DMA response format might differ, but for simplicity:
                var dmaFeed = JsonSerializer.Deserialize<DivaModArchiveModList>(responseString);
                if (dmaFeed?.Posts != null)
                {
                    foreach (var post in dmaFeed.Posts)
                    {
                        var source = sources.FirstOrDefault(x => x.DivaModArchivePostId == post.ID);
                        if (source != null && post.Time > source.Metadata?.lastupdate)
                        {
                            modsWithUpdates.Add(source.ModName);
                        }
                    }
                }
            }
            catch { }
        }
        
        return modsWithUpdates;
    }

    public Metadata? ReadMetadata(string modPath)
    {
        var metadataPath = Path.Combine(modPath, "mod.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Metadata>(File.ReadAllText(metadataPath));
        }
        catch
        {
            return null;
        }
    }
}
