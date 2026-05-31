using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DivaModManager.Core.Models;

namespace DivaModManager.UI
{
    /// <summary>
    /// Interaction logic for UpdateFileBoxDMA.xaml
    /// </summary>
    public partial class UpdateFileBoxDMA : Window
    {
        public Uri chosenFileUrl;
        public string chosenFileName;

        class DMAFileDownload
        {
            public String FileName { get; set; }
            public Uri FileUrl { get; set; }
        }

        public UpdateFileBoxDMA(DivaModArchivePost post)
        {
            InitializeComponent();
            List<DMAFileDownload> files = new List<DMAFileDownload>();
            if (post?.Files != null && post.FileNames != null)
            {
                for (int i = 0; i < post.Files.Count; i++)
                {
                    files.Add(new DMAFileDownload { FileName = post.FileNames[i], FileUrl = post.Files[i] });
                }
            }
            FileList.ItemsSource = files;
            TitleBox.Text = post?.Name;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as DMAFileDownload;
            chosenFileUrl = item?.FileUrl;
            chosenFileName = item?.FileName;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
