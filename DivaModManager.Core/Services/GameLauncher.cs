using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

/// <summary>
/// Service for launching the game with configured mods
/// </summary>
public static class GameLauncher
{
    /// <summary>
    /// Launches the game via executable or Steam
    /// </summary>
    public static async Task<bool> LaunchGameAsync(GameConfig gameConfig, LaunchMethod method)
    {
        if (gameConfig == null)
        {
            Console.WriteLine("Game configuration is null");
            return false;
        }

        try
        {
            if (method == LaunchMethod.Executable)
            {
                return await LaunchViaExecutableAsync(gameConfig);
            }
            else if (method == LaunchMethod.Steam)
            {
                return await LaunchViaSteamAsync(gameConfig);
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching game: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Launches the game directly via executable
    /// </summary>
    private static async Task<bool> LaunchViaExecutableAsync(GameConfig gameConfig)
    {
        if (string.IsNullOrWhiteSpace(gameConfig.GamePath))
        {
            Console.WriteLine("Game path is not configured");
            return false;
        }

        if (!File.Exists(gameConfig.GamePath))
        {
            Console.WriteLine($"Game executable not found at: {gameConfig.GamePath}");
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = gameConfig.GamePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(gameConfig.GamePath)
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                Console.WriteLine($"Game launched successfully (PID: {process.Id})");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching via executable: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Launches the game via Steam
    /// </summary>
    private static async Task<bool> LaunchViaSteamAsync(GameConfig gameConfig)
    {
        // Project DIVA Mega Mix+ on Steam has App ID: 1761390
        const string steamAppId = "1761390";
        const string steamProtocol = "steam://run/1761390";

        try
        {
            // Try to use Steam protocol first
            var startInfo = new ProcessStartInfo
            {
                FileName = steamProtocol,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process != null)
            {
                Console.WriteLine($"Game launched via Steam successfully (App ID: {steamAppId})");
                return true;
            }

            // Fallback: Try to find Steam executable
            string? steamPath = FindSteamPath();
            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = steamPath,
                    Arguments = $"-applaunch {steamAppId}",
                    UseShellExecute = true
                };

                process = Process.Start(startInfo);
                if (process != null)
                {
                    Console.WriteLine($"Game launched via Steam.exe successfully (App ID: {steamAppId})");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error launching via Steam: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to find the Steam executable on the system
    /// </summary>
    private static string? FindSteamPath()
    {
        // Common Steam installation paths
        var possiblePaths = new[]
        {
            "C:\\Program Files (x86)\\Steam\\steam.exe",
            "C:\\Program Files\\Steam\\steam.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steam.exe"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that a game path is a valid Project DIVA executable
    /// </summary>
    public static bool IsValidGamePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(path).ToLower();
        // Check for common Project DIVA executable names
        return fileName.Contains("diva") || fileName.Contains("megamix") || fileName.EndsWith(".exe");
    }
}

public enum LaunchMethod
{
    Executable,
    Steam
}

