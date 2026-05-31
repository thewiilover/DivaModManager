using System.Text.Json;
using DivaModManager.Core.Models;

namespace DivaModManager.Core.Services;

public sealed class ModInstallerService
{
    private readonly ArchiveExtractionService _extractionService = new();

    public void InstallMod(string archivePath, string modsFolder, Metadata? metadata)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "diva_mod_manager", Guid.NewGuid().ToString());
        try
        {
            _extractionService.ExtractArchive(archivePath, tempDir);
            
            // Find all mod folders (those with config.toml)
            var modFolders = Directory.GetDirectories(tempDir, "*", SearchOption.AllDirectories)
                .Where(x => File.Exists(Path.Combine(x, "config.toml")))
                .ToList();

            if (modFolders.Count == 0)
            {
                throw new InvalidOperationException("No valid mod structure found in archive");
            }

            foreach (var sourceFolder in modFolders)
            {
                var targetPath = GetAvailableModPath(modsFolder, Path.GetFileName(sourceFolder));
                MoveDirectory(sourceFolder, targetPath);
                
                // Write metadata if provided
                if (metadata != null && !File.Exists(Path.Combine(targetPath, "mod.json")))
                {
                    var metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(targetPath, "mod.json"), metadataString);
                }
            }
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    public void SaveMetadata(string modPath, Metadata metadata)
    {
        var metadataPath = Path.Combine(modPath, "mod.json");
        var metadataString = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metadataPath, metadataString);
    }

    private static string GetAvailableModPath(string modsFolder, string modName)
    {
        var path = Path.Combine(modsFolder, modName);
        if (!Directory.Exists(path))
        {
            return path;
        }

        var index = 2;
        while (Directory.Exists(path))
        {
            path = Path.Combine(modsFolder, $"{modName} ({index})");
            index++;
        }

        return path;
    }

    private static void MoveDirectory(string sourcePath, string targetPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? targetPath);
        Directory.CreateDirectory(targetPath);

        foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var targetFile = Path.Combine(targetPath, relativePath);
            var targetFileDir = Path.GetDirectoryName(targetFile);
            if (targetFileDir != null)
            {
                Directory.CreateDirectory(targetFileDir);
            }
            File.Copy(file, targetFile, true);
        }
    }
}

