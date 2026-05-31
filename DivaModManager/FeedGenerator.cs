using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DivaModManager.Core.Services;

namespace DivaModManager
{
    public enum GameFilter
    {
        MMP
    }
    
    public enum FeedFilter
    {
        Featured,
        Recent,
        Popular,
        None
    }
    
    public enum TypeFilter
    {
        Mods,
        WiPs,
        Sounds
    }

    public static class FeedGenerator
    {
        private static readonly BrowserFeedService _feedService = new();

        public static bool error => _feedService.HasError;
        public static Exception? exception => _feedService.LastError;
        public static GameBananaModList? CurrentFeed => _feedService.CurrentGameBananaFeed;

        public static void ClearCache()
        {
            _feedService.ClearGameBananaCache();
        }

        public static async Task GetFeed(int page, GameFilter game, TypeFilter type, FeedFilter filter, 
            GameBananaCategory? category, GameBananaCategory? subcategory, int perPage, bool nsfw, string? search)
        {
            // Convert enum values
            var feedType = type switch
            {
                TypeFilter.Mods => GameBananaFeedType.Mods,
                TypeFilter.Sounds => GameBananaFeedType.Sounds,
                TypeFilter.WiPs => GameBananaFeedType.WiPs,
                _ => GameBananaFeedType.Mods
            };

            var feedFilter = filter switch
            {
                FeedFilter.Featured => GameBananaFeedFilter.Featured,
                FeedFilter.Recent => GameBananaFeedFilter.Recent,
                FeedFilter.Popular => GameBananaFeedFilter.Popular,
                _ => GameBananaFeedFilter.None
            };

            await _feedService.GetGameBananaFeed(page, feedType, feedFilter, category?.ID, subcategory?.ID, perPage, nsfw, search);
        }
    }
}
