using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

/// <summary>
/// Cross-platform mod loader that builds mod configurations for the game.
/// This version is independent of WPF-specific code and can be used by both WPF and Avalonia.
/// </summary>
public static class ModLoader
{
    /// <summary>
    /// Builds the mod configuration by reading enabled mods and writing them to config.toml
    /// </summary>
    /// <param name="modsFolder">Path to the mods folder containing config.toml</param>
    /// <param name="enabledMods">List of enabled mods to prioritize</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool Build(string modsFolder, List<Mod> mods, string? selectedGame = null, string? selectedLoadout = null, string? selectedLaunchMethod = null, List<string>? loadouts = null)
    {
        var normalizedModsFolder = modsFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var configPath = Path.Combine(
            Path.GetDirectoryName(normalizedModsFolder) ?? normalizedModsFolder,
            "config.toml"
        );

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Unable to find {configPath}");
            return false;
        }

        // Read config file with retry logic for file locking
        var configString = ReadConfigWithRetry(configPath);
        if (string.IsNullOrEmpty(configString))
        {
            Console.WriteLine($"Failed to read {configPath}");
            return false;
        }

        // Parse TOML config
        if (!Toml.TryToModel(configString, out TomlTable? config, out var diagnostics))
        {
            // Create a new config if it failed to parse
            config = new();
            config.Add("enabled", true);
            config.Add("console", false);
            config.Add("mods", "mods");
        }

        if (config == null)
        {
            Console.WriteLine("Failed to create or parse config");
            return false;
        }

        // Build priority list from enabled mods
        var priorityList = mods
            .Where(x => x.enabled)
            .Select(x => x.name)
            .ToList();

        var disabledList = mods
            .Where(x => !x.enabled)
            .Select(x => x.name)
            .ToList();

        config["priority"] = priorityList.ToArray();
        config["disabled"] = disabledList.ToArray();

        // Add extra settings if provided
        if (selectedGame != null) config["selected_game"] = selectedGame;
        if (selectedLoadout != null) config["selected_loadout"] = selectedLoadout;
        if (selectedLaunchMethod != null) config["selected_launch_method"] = selectedLaunchMethod;
        if (loadouts != null) config["loadouts"] = loadouts.ToArray();

        // Write config file with retry logic for file locking
        return WriteConfigWithRetry(configPath, config);
    }

    /// <summary>
    /// Reads the config file with retry logic for handling file locks
    /// </summary>
    private static string ReadConfigWithRetry(string configPath, int maxRetries = 5)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return File.ReadAllText(configPath);
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                // Wait a bit and retry if it's a file lock issue
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading config: {e.Message}");
                return string.Empty;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Writes the config file with retry logic for handling file locks
    /// </summary>
    private static bool WriteConfigWithRetry(string configPath, TomlTable config, int maxRetries = 5)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                File.WriteAllText(configPath, Toml.FromModel(config));
                return true;
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                // Wait a bit and retry if it's a file lock issue
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error writing config: {e.Message}");
                return false;
            }
        }
        return false;
    }
}

