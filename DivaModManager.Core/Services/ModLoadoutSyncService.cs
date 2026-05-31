using System.Collections.ObjectModel;
using System.Text.Json;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class ModLoadoutSyncService
{
    public void SyncCurrentLoadout(Config config, string currentGame, string currentLoadout, ObservableCollection<Mod> modList)
    {
        if (!config.Configs.TryGetValue(currentGame, out var gameConfig))
        {
            gameConfig = new GameConfig();
            config.Configs[currentGame] = gameConfig;
        }

        gameConfig.CurrentLoadout = currentLoadout;
        gameConfig.Loadouts ??= new Dictionary<string, ObservableCollection<Mod>>();
        gameConfig.Loadouts[currentLoadout] = modList;
        config.CurrentGame = currentGame;
    }

    public string Save(Config config)
        => JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
}
