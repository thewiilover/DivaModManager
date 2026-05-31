using Tomlyn;
using Tomlyn.Model;

namespace DivaModManager.Core.Services;

public sealed class ModLoaderConfigWriter
{
    public string BuildUpdatedConfig(string existingToml, IEnumerable<string> enabledPriorityMods)
    {
        TomlTable? config;
        if (!Toml.TryToModel(existingToml, out config, out _) || config is null)
        {
            config = new TomlTable
            {
                ["enabled"] = true,
                ["console"] = false,
                ["mods"] = "mods"
            };
        }

        config["priority"] = enabledPriorityMods.ToArray();
        return Toml.FromModel(config);
    }
}