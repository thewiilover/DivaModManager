using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using System.Threading;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using DivaModManager.UI;
using DivaModManager.Core.Services;
using DivaModManager.Core.Models;

namespace DivaModManager
{
    public class ModDownloader
    {
        private string URL_TO_ARCHIVE = string.Empty;
        private string URL = string.Empty;
        private string DL_ID = string.Empty;
        private string MOD_TYPE = string.Empty;
        private string MOD_ID = string.Empty;
        private string fileName = string.Empty;
        private bool cancelled;
        private readonly HttpClient client = new();
        private readonly CancellationTokenSource cancellationToken = new();
        private GameBananaAPIV4 response = new();
        private DivaModArchivePost DMAresponse = new();
        private ProgressBox? progressBox;
        private readonly BrowserDownloadService _downloadService = new();
        private readonly ModInstallerService _installerService = new();

        public async void BrowserDownload(string game, GameBananaRecord record)
        {
            if (!HasValidModsFolder())
            {
                MessageBox.Show($"Please click Setup before installing mods!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Global.logger.WriteLine("Please click Setup before installing mods!", LoggerType.Warning);
                return;
            }

            DownloadWindow downloadWindow = new DownloadWindow(record);
            downloadWindow.ShowDialog();
            if (downloadWindow.YesNo)
            {
                string? downloadUrl = null;
                string? selectedFileName = null;
                if (record.AllFiles?.Count == 1)
                {
                    downloadUrl = record.AllFiles[0].DownloadUrl;
                    selectedFileName = record.AllFiles[0].FileName;
                }
                else if (record.AllFiles?.Count > 1)
                {
                    UpdateFileBox fileBox = new UpdateFileBox(record.AllFiles, record.Title);
                    fileBox.Activate();
                    fileBox.ShowDialog();
                    downloadUrl = fileBox.chosenFileUrl;
                    selectedFileName = fileBox.chosenFileName;
                }

                if (!string.IsNullOrWhiteSpace(downloadUrl) && !string.IsNullOrWhiteSpace(selectedFileName))
                {
                    await DownloadFile(downloadUrl, selectedFileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                    if (!cancelled)
                        await Task.Run(() => ExtractFile(selectedFileName, game, record));
                }
            }
        }

        public async void DMABrowserDownload(string game, DivaModArchivePost post)
        {
            if (!HasValidModsFolder())
            {
                MessageBox.Show($"Please click Setup before installing mods!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Global.logger.WriteLine("Please click Setup before installing mods!", LoggerType.Warning);
                return;
            }

            DownloadWindow downloadWindow = new DownloadWindow(post);
            downloadWindow.ShowDialog();
            if (downloadWindow.YesNo)
            {
                string? downloadUrl = null;
                string? selectedFileName = null;
                if (post.Files?.Count == 1)
                {
                    downloadUrl = post.Files[0].ToString();
                    selectedFileName = post.FileNames?[0];
                }
                else if (post.Files?.Count > 1)
                {
                    UpdateFileBoxDMA fileBox = new UpdateFileBoxDMA(post);
                    fileBox.Activate();
                    fileBox.ShowDialog();
                    downloadUrl = fileBox.chosenFileUrl?.ToString();
                    selectedFileName = fileBox.chosenFileName;
                }

                if (!string.IsNullOrWhiteSpace(downloadUrl) && !string.IsNullOrWhiteSpace(selectedFileName))
                {
                    await DownloadFile(downloadUrl, selectedFileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                    if (!cancelled)
                        await Task.Run(() => ExtractFile(selectedFileName, game, post));
                }
            }
        }

        public async void Download(string line, bool running)
        {
            if (!HasValidModsFolder())
            {
                MessageBox.Show($"Please click Setup before installing mods!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Global.logger.WriteLine("Please click Setup before installing mods!", LoggerType.Warning);
                return;
            }

            if (ParseProtocol(line))
            {
                if (await GetData())
                {
                    if (URL.Contains("gamebanana", StringComparison.CurrentCultureIgnoreCase))
                    {
                        DownloadWindow downloadWindow = new DownloadWindow(response);
                        downloadWindow.ShowDialog();
                        if (downloadWindow.YesNo)
                        {
                            await DownloadFile(URL_TO_ARCHIVE, fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                            if (!cancelled)
                                await Task.Run(() => ExtractFile(fileName, response.Game?.Name ?? Global.config.CurrentGame, response));
                        }
                    }
                    else if (URL.Contains("divamodarchive", StringComparison.CurrentCultureIgnoreCase))
                    {
                        DownloadWindow downloadWindow = new DownloadWindow(DMAresponse);
                        downloadWindow.ShowDialog();
                        if (downloadWindow.YesNo)
                        {
                            await DownloadFile(DMAresponse.Files[0].ToString(), fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                            if (!cancelled)
                                await Task.Run(() => ExtractFile(fileName, "Project DIVA Mega Mix+", DMAresponse));
                        }
                    }
                }
            }

            if (running)
                Environment.Exit(0);
        }

        private async Task<bool> GetData()
        {
            try
            {
                if (URL.Contains("gamebanana", StringComparison.CurrentCultureIgnoreCase))
                {
                    response = await _downloadService.FetchGameBananaDataAsync(URL, DL_ID) ?? new GameBananaAPIV4();
                    var file = response.Files?.FirstOrDefault(x => x.Id.ToString() == DL_ID);
                    if (file == null || string.IsNullOrWhiteSpace(file.FileName))
                    {
                        MessageBox.Show("Could not resolve download file from protocol URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    fileName = file.FileName;
                    return true;
                }
                else if (URL.Contains("divamodarchive", StringComparison.CurrentCultureIgnoreCase))
                {
                    DMAresponse = await _downloadService.FetchDivaModArchiveDataAsync(URL) ?? new DivaModArchivePost();
                    fileName = DMAresponse.FileNames?.FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        MessageBox.Show("Could not resolve DMA file from protocol URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    return true;
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while fetching data {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void ReportUpdateProgress(DownloadProgress progress)
        {
            if (progressBox == null)
            {
                return;
            }

            if (progress.Percentage == 1)
            {
                progressBox.finished = true;
            }

            progressBox.progressBar.Value = progress.Percentage * 100;
            progressBox.taskBarItem.ProgressValue = progress.Percentage;
            progressBox.progressTitle.Text = $"Downloading {progress.FileName}...";
            progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% " +
                $"({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }

        private bool ParseProtocol(string line)
        {
            try
            {
                if (_downloadService.TryParseGameBananaProtocol(line, out var downloadUrl, out var modType, out var modId, out var dlId))
                {
                    URL_TO_ARCHIVE = downloadUrl;
                    DL_ID = dlId;
                    MOD_TYPE = modType;
                    MOD_ID = modId;
                    URL = $"https://gamebanana.com/apiv6/{MOD_TYPE}/{MOD_ID}?_csvProperties=_sName,_aGame,_sProfileUrl,_aPreviewMedia,_sDescription,_aSubmitter,_aCategory,_aSuperCategory,_aFiles,_tsDateUpdated,_aAlternateFileSources,_bHasUpdates,_aLatestUpdates";
                    return true;
                }

                if (_downloadService.TryParseDivaModArchiveProtocol(line, out var postId))
                {
                    MOD_ID = postId;
                    URL = $"https://divamodarchive.com/api/v1/posts/{MOD_ID}";
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while parsing {line}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void ExtractFile(string fileName, string game, GameBananaRecord record)
        {
            try
            {
                string archivePath = $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}";
                if (!File.Exists(archivePath))
                {
                    MessageBox.Show($"Didn't extract {fileName} due to improper format", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var metadata = new Metadata
                {
                    submitter = record.Owner.Name,
                    description = record.Description,
                    preview = record.Image,
                    homepage = record.Link,
                    avi = record.Owner.Avatar,
                    upic = record.Owner.Upic,
                    cat = record.CategoryName,
                    caticon = record.Category.Icon,
                    lastupdate = record.DateUpdated
                };

                _installerService.InstallMod(archivePath, Global.config.Configs[Global.config.CurrentGame].ModsFolder, metadata);
                File.Delete(archivePath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExtractFile(string fileName, string game, GameBananaAPIV4 record)
        {
            try
            {
                string archivePath = $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}";
                if (!File.Exists(archivePath))
                {
                    MessageBox.Show($"Didn't extract {fileName} due to improper format", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var metadata = new Metadata
                {
                    submitter = record.Owner?.Name,
                    description = record.Description,
                    preview = record.Image,
                    homepage = record.Link,
                    avi = record.Owner?.Avatar,
                    upic = record.Upic,
                    cat = record.CategoryName,
                    caticon = record.Category?.Icon,
                    lastupdate = record.DateUpdated == default ? null : record.DateUpdated
                };

                _installerService.InstallMod(archivePath, Global.config.Configs[Global.config.CurrentGame].ModsFolder, metadata);
                File.Delete(archivePath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExtractFile(string fileName, string game, DivaModArchivePost post)
        {
            try
            {
                string archivePath = $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}";
                if (!File.Exists(archivePath))
                {
                    MessageBox.Show($"Didn't extract {fileName} due to improper format", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var metadata = new Metadata
                {
                    id = post.ID,
                    submitter = post.Authors[0].Name,
                    description = post.Text,
                    preview = post.Images[0],
                    homepage = post.Link,
                    avi = post.Authors[0].Avatar,
                    cat = post.PostType,
                    lastupdate = post.Time
                };

                _installerService.InstallMod(archivePath, Global.config.Configs[Global.config.CurrentGame].ModsFolder, metadata);
                File.Delete(archivePath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Couldn't extract {fileName}: {e.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task DownloadFile(string uri, string fileName, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                Directory.CreateDirectory($@"{Global.assemblyLocation}{Global.s}Downloads");
                // Download the file if it doesn't already exist
                if (File.Exists($@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}"))
                {
                    try
                    {
                        File.Delete($@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Couldn't delete the already existing {Global.assemblyLocation}/Downloads/{fileName} ({e.Message})",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                progressBox = new ProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.finished = false;
                progressBox.Title = $"Download Progress";
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                await using (var fs = new FileStream(
                    $@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete($@"{Global.assemblyLocation}{Global.s}Downloads{Global.s}{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                    cancelled = true;
                }
                return;
            }
            catch (Exception e)
            {
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                MessageBox.Show($"Error whilst downloading {fileName}. {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cancelled = true;
            }
        }

        private static bool HasValidModsFolder()
        {
            return !string.IsNullOrEmpty(Global.config.Configs[Global.config.CurrentGame].ModsFolder)
                   && Directory.Exists(Global.config.Configs[Global.config.CurrentGame].ModsFolder);
        }

    }
}
