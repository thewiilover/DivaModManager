using System.Text.Json;
using System.Text.RegularExpressions;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class BrowserDownloadService
{
    private readonly MetadataFetchingService _metadataService = new();
    private readonly HttpClient _client = new();

    public async Task<GameBananaAPIV4?> FetchGameBananaDataAsync(string url, string dlId)
    {
        try
        {
            var responseString = await _client.GetStringAsync(url);
            var record = JsonSerializer.Deserialize<GameBananaAPIV4>(responseString);
            return record;
        }
        catch
        {
            return null;
        }
    }

    public async Task<DivaModArchivePost?> FetchDivaModArchiveDataAsync(string url)
    {
        try
        {
            var responseString = await _client.GetStringAsync(url);
            var post = JsonSerializer.Deserialize<DivaModArchivePost>(responseString);
            return post;
        }
        catch
        {
            return null;
        }
    }

    public bool TryParseGameBananaProtocol(string line, out string downloadUrl, out string modType, out string modId, out string dlId)
    {
        downloadUrl = string.Empty;
        modType = string.Empty;
        modId = string.Empty;
        dlId = string.Empty;

        try
        {
            line = line.Replace("divamodmanager:", "");
            var data = line.Split(',');
            
            // GameBanana 1-click install
            if (data.Length > 1)
            {
                downloadUrl = data[0];
                var match = Regex.Match(downloadUrl, @"\d*$");
                dlId = match.Value;
                modType = data[1];
                modId = data[2];
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool TryParseDivaModArchiveProtocol(string line, out string postId)
    {
        postId = string.Empty;

        try
        {
            line = line.Replace("divamodmanager:", "");
            var data = line.Split(',');
            
            // DivaModArchive 1-click install
            if (data.Length == 1)
            {
                postId = data[0].Replace("dma/", string.Empty);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Metadata?> FetchMetadataFromGameBananaAsync(string url)
    {
        return await _metadataService.FetchFromGameBananaUrlAsync(url);
    }

    public async Task<Metadata?> FetchMetadataFromDivaModArchiveAsync(string url)
    {
        return await _metadataService.FetchFromDivaModArchiveUrlAsync(url);
    }
}
