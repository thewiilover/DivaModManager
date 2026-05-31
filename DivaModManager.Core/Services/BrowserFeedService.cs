using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using DivaModManager.Core.Models;
using DivaModManager.Core.Services;

namespace DivaModManager.Core.Services;

public enum GameBananaFeedFilter
{
    Featured,
    Recent,
    Popular,
    None
}

public enum GameBananaFeedType
{
    Mods,
    WiPs,
    Sounds
}

public enum DMAFeedSort
{
    Latest,
    Downloads,
    Likes,
}

public enum DMAFeedFilter
{
    None,
    Song,
    Cover,
    Module,
    Ui,
    Plugin,
    Other
}

// GameBananaModList and DivaModArchiveModList are now defined in Core.Models namespace

public sealed class BrowserFeedService
{
    private readonly HttpClient _httpClient = new();
    private Dictionary<string, GameBananaModList>? _gamebananaFeed;
    private Dictionary<string, DivaModArchiveModList>? _dmaFeed;

    public bool HasError { get; private set; }
    public Exception? LastError { get; private set; }
    public GameBananaModList? CurrentGameBananaFeed { get; private set; }
    public DivaModArchiveModList? CurrentDMAFeed { get; private set; }

    public void ClearGameBananaCache()
    {
        _gamebananaFeed?.Clear();
    }

    public void ClearDMACache()
    {
        _dmaFeed?.Clear();
    }

    public async Task GetGameBananaFeed(int page, GameBananaFeedType type, GameBananaFeedFilter filter, 
        int? categoryId, int? subcategoryId, int perPage, bool nsfw, string? search)
    {
        HasError = false;
        LastError = null;
        _gamebananaFeed ??= new Dictionary<string, GameBananaModList>();

        // Remove oldest key if more than 15 pages are cached
        if (_gamebananaFeed.Count > 15)
        {
            var oldest = _gamebananaFeed.Aggregate((l, r) => 
                DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r);
            _gamebananaFeed.Remove(oldest.Key);
        }

        var requestUrl = GenerateGameBananaUrl(page, type, filter, categoryId, subcategoryId, perPage, nsfw, search);
        
        if (_gamebananaFeed.ContainsKey(requestUrl) && _gamebananaFeed[requestUrl].IsValid)
        {
            CurrentGameBananaFeed = _gamebananaFeed[requestUrl];
            return;
        }

        CurrentGameBananaFeed = new();
        
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var records = JsonSerializer.Deserialize<ObservableCollection<GameBananaRecord>>(
                await response.Content.ReadAsStringAsync());
            CurrentGameBananaFeed.Records = records;

            // Get record count from header
            if (response.Headers.TryGetValues("X-GbApi-Metadata_nRecordCount", out var values))
            {
                if (double.TryParse(values.FirstOrDefault(), out var numRecords) && numRecords >= 0)
                {
                    var totalPages = Math.Ceiling(numRecords / Convert.ToDouble(perPage));
                    if (totalPages == 0)
                        totalPages = 1;
                    CurrentGameBananaFeed.TotalPages = totalPages;
                }
            }
        }
        catch (Exception e)
        {
            HasError = true;
            LastError = e;
            return;
        }

        if (!_gamebananaFeed.ContainsKey(requestUrl))
            _gamebananaFeed.Add(requestUrl, CurrentGameBananaFeed);
        else
            _gamebananaFeed[requestUrl] = CurrentGameBananaFeed;
    }

    public async Task GetDMAFeed(int page, DMAFeedSort sort, DMAFeedFilter filter, string? search, int limit)
    {
        HasError = false;
        LastError = null;
        _dmaFeed ??= new Dictionary<string, DivaModArchiveModList>();

        // Remove oldest key if more than 15 pages are cached
        if (_dmaFeed.Count > 15)
        {
            var oldest = _dmaFeed.Aggregate((l, r) => 
                DateTime.Compare(l.Value.TimeFetched, r.Value.TimeFetched) < 0 ? l : r);
            _dmaFeed.Remove(oldest.Key);
        }

        var requestUrl = GenerateDMAUrl(page, sort, filter, search, limit);
        
        if (_dmaFeed.ContainsKey(requestUrl) && _dmaFeed[requestUrl].IsValid)
        {
            CurrentDMAFeed = _dmaFeed[requestUrl];
            return;
        }

        CurrentDMAFeed = new();
        
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var posts = JsonSerializer.Deserialize<ObservableCollection<DivaModArchivePost>>(
                await response.Content.ReadAsStringAsync());
            CurrentDMAFeed.Posts = posts;

            response = await _httpClient.GetAsync(
                $"https://divamodarchive.com/api/v1/posts/count?query={search}&limit={limit}");
            var numPostsStr = await response.Content.ReadAsStringAsync();
            if (double.TryParse(numPostsStr, out var numPosts))
            {
                var totalPages = Math.Ceiling(numPosts / limit);
                if (totalPages == 0)
                    totalPages = 1;
                CurrentDMAFeed.TotalPages = totalPages;
            }
        }
        catch (Exception e)
        {
            HasError = true;
            LastError = e;
            return;
        }

        if (!_dmaFeed.ContainsKey(requestUrl))
            _dmaFeed.Add(requestUrl, CurrentDMAFeed);
        else
            _dmaFeed[requestUrl] = CurrentDMAFeed;
    }

    private static string GenerateGameBananaUrl(int page, GameBananaFeedType type, GameBananaFeedFilter filter,
        int? categoryId, int? subcategoryId, int perPage, bool nsfw, string? search)
    {
        var url = "https://gamebanana.com/apiv6/";
        
        url += type switch
        {
            GameBananaFeedType.Mods => "Mod/",
            GameBananaFeedType.Sounds => "Sound/",
            GameBananaFeedType.WiPs => "Wip/",
            _ => "Mod/"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"ByName?_sName=*{search}*&_idGameRow=16522&";
        }
        else if (categoryId != null)
        {
            url += "ByCategory?";
        }
        else
        {
            url += "ByGame?_aGameRowIds[]=16522&";
        }

        url += $"_csvProperties=_sName,_sModelName,_sProfileUrl,_aSubmitter,_tsDateUpdated,_tsDateAdded,_aPreviewMedia,_sText,_sDescription,_aCategory,_aRootCategory,_aGame,_nViewCount," +
            $"_nLikeCount,_nDownloadCount,_aFiles,_aModManagerIntegrations,_bIsNsfw,_aAlternateFileSources&_nPerpage={perPage}";

        if (!nsfw)
            url += "&_aArgs[]=_sbIsNsfw = false";

        url += filter switch
        {
            GameBananaFeedFilter.Recent => "&_sOrderBy=_tsDateUpdated,DESC",
            GameBananaFeedFilter.Featured => "&_aArgs[]=_sbWasFeatured = true& _sOrderBy=_tsDateAdded,DESC",
            GameBananaFeedFilter.Popular => "&_sOrderBy=_nDownloadCount,DESC",
            _ => ""
        };

        if (subcategoryId != null)
            url += $"&_aCategoryRowIds[]={subcategoryId}";
        else if (categoryId != null)
            url += $"&_aCategoryRowIds[]={categoryId}";

        url += $"&_nPage={page}";
        return url;
    }

    private static string GenerateDMAUrl(int page, DMAFeedSort sort, DMAFeedFilter filter, string? search, int limit)
    {
        var url = "https://divamodarchive.com/api/v1/posts?sort=";
        
        url += sort switch
        {
            DMAFeedSort.Latest => "time:desc",
            DMAFeedSort.Downloads => "download_count:desc",
            DMAFeedSort.Likes => "like_count:desc",
            _ => "time:desc"
        };

        url += filter switch
        {
            DMAFeedFilter.Song => "&filter=post_type=Song",
            DMAFeedFilter.Cover => "&filter=post_type=Cover",
            DMAFeedFilter.Module => "&filter=post_type=Module",
            DMAFeedFilter.Ui => "&filter=post_type=UI",
            DMAFeedFilter.Plugin => "&filter=post_type=Plugin",
            DMAFeedFilter.Other => "&filter=post_type=Other",
            _ => ""
        };

        url += $"&query={search ?? ""}";
        var offset = (page - 1) * limit;
        url += $"&offset={offset}&limit={limit}";
        return url;
    }
}







