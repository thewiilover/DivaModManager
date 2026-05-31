using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace DivaModManager.Core.Services;

public sealed class ArchiveExtractionService
{
    public void ExtractArchive(string archivePath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException("Archive not found", archivePath);
        }

        if (Path.GetExtension(archivePath).Equals(".7z", StringComparison.InvariantCultureIgnoreCase))
        {
            using var archive = SevenZipArchive.Open(archivePath);
            var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(outputDirectory, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

            return;
        }

        using (Stream stream = File.OpenRead(archivePath))
        using (var reader = ReaderFactory.Open(stream))
        {
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    reader.WriteEntryToDirectory(outputDirectory, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }
    }
}
