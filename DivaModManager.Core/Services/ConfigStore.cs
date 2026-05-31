using System.Text.Json;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class ConfigStore
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public Config Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return new Config();
        }

        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }

    public void Save(string configPath, Config config)
    {
        var json = JsonSerializer.Serialize(config, _serializerOptions);
        File.WriteAllText(configPath, json);
    }
}

