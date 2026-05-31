using System;
using System.Threading.Tasks;
using DivaModManager.Core.Services;

namespace DivaModManager
{
    /// <summary>
    /// Wrapper around BrowserFeedService for DivaModArchive feed generation
    /// </summary>
    public static class DMAFeedGenerator
    {
        private static readonly BrowserFeedService _feedService = new();

        public static bool error => _feedService.HasError;
        public static Exception? exception => _feedService.LastError;
        public static DivaModArchiveModList? CurrentFeed => _feedService.CurrentDMAFeed;

        public static void ClearCache()
        {
            _feedService.ClearDMACache();
        }

        public static async Task GetFeed(int page, DMAFeedSort sort, DMAFeedFilter filter, string? search, int limit)
        {
            // Convert to core service enums
            var coreSort = sort switch
            {
                DMAFeedSort.Latest => Services.DMAFeedSort.Latest,
                DMAFeedSort.Downloads => Services.DMAFeedSort.Downloads,
                DMAFeedSort.Likes => Services.DMAFeedSort.Likes,
                _ => Services.DMAFeedSort.Latest
            };

            var coreFilter = filter switch
            {
                DMAFeedFilter.Song => Services.DMAFeedFilter.Song,
                DMAFeedFilter.Cover => Services.DMAFeedFilter.Cover,
                DMAFeedFilter.Module => Services.DMAFeedFilter.Module,
                DMAFeedFilter.Ui => Services.DMAFeedFilter.Ui,
                DMAFeedFilter.Plugin => Services.DMAFeedFilter.Plugin,
                DMAFeedFilter.Other => Services.DMAFeedFilter.Other,
                _ => Services.DMAFeedFilter.None
            };

            await _feedService.GetDMAFeed(page, coreSort, coreFilter, search, limit);
        }
    }
}
