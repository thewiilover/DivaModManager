using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DivaModManager.Core.Services;
using DivaModManager.Core.Models;

namespace DivaModManager.UI
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class FetchWindow : Window
    {
        public bool success;
        public Mod _mod;
        private readonly MetadataFetchingService _metadataService = new();

        public FetchWindow(Mod mod)
        {
            InitializeComponent();
            _mod = mod;
            Title = $"Fetch Metadata for {_mod.name}";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Fetch()
        {
            try
            {
                // Try GameBanana first
                var metadata = await _metadataService.FetchFromGameBananaUrlAsync(UrlBox.Text);
                if (metadata != null)
                {
                    SaveMetadata(metadata);
                    success = true;
                    Close();
                    return;
                }

                // Try DivaModArchive
                metadata = await _metadataService.FetchFromDivaModArchiveUrlAsync(UrlBox.Text);
                if (metadata != null)
                {
                    SaveMetadata(metadata);
                    success = true;
                    Close();
                    return;
                }

                Global.logger.WriteLine($"{UrlBox.Text} is invalid. The url should have the following format: https://gamebanana.com/<Mod Category>/<Mod ID> or https://divamodarchive.com/post/<Post ID>", LoggerType.Error);
            }
            catch (Exception ex)
            {
                Global.logger.WriteLine(ex.Message, LoggerType.Error);
            }
        }

        private void SaveMetadata(Metadata metadata)
        {
            var modPath = Path.Combine(Global.config.Configs[Global.config.CurrentGame].ModsFolder, _mod.name);
            var installer = new ModInstallerService();
            installer.SaveMetadata(modPath, metadata);
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Fetch();
        }

        private void UrlBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Fetch();
        }
    }
}
