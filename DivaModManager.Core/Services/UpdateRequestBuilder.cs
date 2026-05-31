using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class UpdateRequestBuilder
{
    public Dictionary<string, List<string>> BuildGameBananaRequests(IEnumerable<InstalledModUpdateSource> sources)
    {
        var requestUrls = new Dictionary<string, List<string>>();
        var urlCounts = new Dictionary<string, int>();

        foreach (var source in sources.Where(x => x.Kind == InstalledModSourceKind.GameBanana && !string.IsNullOrWhiteSpace(x.GameBananaType) && !string.IsNullOrWhiteSpace(x.GameBananaId)))
        {
            var type = source.GameBananaType!;
            var id = source.GameBananaId!;

            if (!urlCounts.ContainsKey(type))
            {
                urlCounts[type] = 0;
            }

            var index = urlCounts[type];
            if (!requestUrls.ContainsKey(type))
            {
                requestUrls[type] = new List<string>
                {
                    $"https://gamebanana.com/apiv6/{type}/Multi?_csvProperties=_sName,_aSubmitter,_aCategory,_aSuperCategory,_sProfileUrl,_sDescription,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources,_tsDateUpdated&_csvRowIds="
                };
            }
            else if (requestUrls[type].Count == index)
            {
                requestUrls[type].Add($"https://gamebanana.com/apiv6/{type}/Multi?_csvProperties=_sName,_aSubmitter,_aCategory,_aSuperCategory,_sProfileUrl,_sDescription,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources,_tsDateUpdated&_csvRowIds=");
            }

            requestUrls[type][index] += $"{id},";
            if (requestUrls[type][index].Length > 1990)
            {
                urlCounts[type]++;
            }
        }

        foreach (var type in requestUrls.Keys.ToList())
        {
            for (var i = 0; i < requestUrls[type].Count; i++)
            {
                if (requestUrls[type][i].EndsWith(','))
                {
                    requestUrls[type][i] = requestUrls[type][i][..^1];
                }
            }
        }

        return requestUrls;
    }

    public string BuildDivaModArchiveRequest(IEnumerable<InstalledModUpdateSource> sources)
    {
        var requestUrl = "https://divamodarchive.com/api/v1/posts/posts?";
        foreach (var source in sources.Where(x => x.Kind == InstalledModSourceKind.DivaModArchive && x.DivaModArchivePostId != null))
        {
            requestUrl += $"post_id={source.DivaModArchivePostId}&";
        }

        return requestUrl;
    }
}
