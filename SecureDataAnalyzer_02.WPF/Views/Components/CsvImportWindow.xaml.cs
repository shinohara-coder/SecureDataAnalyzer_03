using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class CsvImportWindow : Window
    {
        public string SelectedFilePath { get; private set; } = string.Empty;

        public CsvImportWindow()
        {
            InitializeComponent();
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "CSVファイル (*.csv)|*.csv";
            if (dialog.ShowDialog() == true)
            {
                CompleteImport(dialog.FileName);
            }
        }

        private void DropArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                DropArea.Background = Brushes.AliceBlue;
            }
            e.Handled = true;
        }

        private void DropArea_DragLeave(object sender, DragEventArgs e)
        {
            DropArea.Background = Brushes.White;
        }

        private void DropArea_Drop(object sender, DragEventArgs e)
        {
            DropArea.Background = Brushes.White;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    if (Path.GetExtension(filePath).ToLower() == ".csv")
                    {
                        CompleteImport(filePath);
                    }
                    else
                    {
                        MessageBox.Show("CSVファイルのみ対応しています。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void CompleteImport(string path)
        {
            SelectedFilePath = path;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}