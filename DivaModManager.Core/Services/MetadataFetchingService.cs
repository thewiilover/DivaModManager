using System.Text.Json;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class MetadataFetchingService
{
    private readonly HttpClient _client = new();

    public async Task<Metadata?> FetchFromGameBananaUrlAsync(string url)
    {
        if (!TryParseGameBananaUrl(url, out var type, out var id))
        {
            return null;
        }

        try
        {
            var apiUrl = $"https://gamebanana.com/apiv6/{type}/{id}?_csvProperties=_aSubmitter,_sDescription,_aPreviewMedia,_sProfileUrl," +
                $"_aSuperCategory,_aCategory,_tsDateUpdated";
            
            var responseString = await _client.GetStringAsync(apiUrl);
            var record = JsonSerializer.Deserialize<GameBananaAPIV4>(responseString);
            
            if (record?.Owner == null)
                return null;

            var metadata = new Metadata
            {
                submitter = record.Owner.Name,
                description = record.Description,
                preview = record.Image,
                homepage = record.Link,
                avi = record.Owner.Avatar,
                upic = record.Upic,
                cat = record.CategoryName,
                caticon = record.Category?.Icon,
                lastupdate = record.DateUpdated == default ? null : record.DateUpdated
            };

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Metadata?> FetchFromDivaModArchiveUrlAsync(string url)
    {
        if (!TryParseDivaModArchiveUrl(url, out var postId))
        {
            return null;
        }

        try
        {
            var apiUrl = $"https://divamodarchive.com/api/v1/posts/{postId}";
            var responseString = await _client.GetStringAsync(apiUrl);
            var post = JsonSerializer.Deserialize<DivaModArchivePost>(responseString);
            
            if (post?.Authors == null || post.Images == null || post.Authors.Count == 0)
                return null;

            var metadata = new Metadata
            {
                id = post.ID,
                submitter = post.Authors[0].Name,
                description = post.Text,
                preview = post.Images.Count > 0 ? post.Images[0] : null,
                homepage = post.Link,
                avi = post.Authors[0].Avatar,
                cat = post.PostType,
                lastupdate = post.Time == default ? null : post.Time
            };

            return metadata;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseGameBananaUrl(string url, out string type, out string id)
    {
        type = string.Empty;
        id = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
            !Uri.TryCreate("http://" + url, UriKind.Absolute, out uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.DnsSafeHost;
        if (host != "www.gamebanana.com" && host != "gamebanana.com")
        {
            return false;
        }

        if (uri.Segments.Length != 3)
        {
            return false;
        }

        type = char.ToUpper(uri.Segments[1][0]) + uri.Segments[1].Substring(1, uri.Segments[1].Length - 3);
        id = uri.Segments[2];
        return !string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(id);
    }

    private static bool TryParseDivaModArchiveUrl(string url, out string postId)
    {
        postId = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
            !Uri.TryCreate("http://" + url, UriKind.Absolute, out uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.DnsSafeHost;
        if (host != "www.divamodarchive.com" && host != "divamodarchive.com")
        {
            return false;
        }

        if (uri.Segments.Length != 3)
        {
            return false;
        }

        postId = uri.Segments[2];
        return !string.IsNullOrWhiteSpace(postId);
    }
}

// TODO: These should be in a separate Structures file
// Temporary structures for API responses
public class GameBananaAPIV4
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Uri? Image { get; set; }
    public Uri? Link { get; set; }
    public GameBananaOwner? Owner { get; set; }
    public Uri? Upic { get; set; }
    public string? CategoryName { get; set; }
    public GameBananaCategory? Category { get; set; }
    public DateTime DateUpdated { get; set; }
    public List<GameBananaItemFile>? Files { get; set; }
    public List<GameBananaUpdate>? Updates { get; set; }
    public bool? HasUpdates { get; set; }
    public List<GameBananaItemFile>? AllFiles { get; set; }
    public GameBananaGame? Game { get; set; }
    public object? AlternateFileSources { get; set; }
}

public class GameBananaOwner
{
    public string? Name { get; set; }
    public Uri? Avatar { get; set; }
}

public class GameBananaCategory
{
    public Uri? Icon { get; set; }
}

public class GameBananaItemFile
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public string? DownloadUrl { get; set; }
}

public class GameBananaUpdate
{
    public DateTime DateAdded { get; set; }
}

public class GameBananaGame
{
    public string? Name { get; set; }
}

// GameBananaRecord and DivaModArchivePost are now defined in DivaModManager.Core.Models namespace







