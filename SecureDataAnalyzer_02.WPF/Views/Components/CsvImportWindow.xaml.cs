using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// CSVファイルを読み込むためのダイアログウィンドウ
    /// </summary>
    public partial class CsvImportWindow : Window
    {
        // 読み込まれたファイルのパスを保持するプロパティ（外部から参照可能）
        public string SelectedFilePath { get; private set; } = string.Empty;

        public CsvImportWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 「エクスプローラーで参照」ボタンクリック時：標準のファイルダイアログを開く
        /// </summary>
        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "CSVファイル (*.csv)|*.csv"; // 選択できる拡張子を制限
            if (dialog.ShowDialog() == true)
            {
                CompleteImport(dialog.FileName);
            }
        }

        /// <summary>
        /// ドラッグ中のファイルがエリアに入った時：アイコンの変化と背景色の変更
        /// </summary>
        private void DropArea_DragOver(object sender, DragEventArgs e)
        {
            // ドラッグされているものがファイル形式か確認
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy; // マウスカーソルをコピー状態にする
                DropArea.Background = Brushes.AliceBlue; // 視覚的なフィードバック
            }
            e.Handled = true;
        }

        /// <summary>
        /// ドラッグ中のファイルがエリアから外れた時：背景色を元に戻す
        /// </summary>
        private void DropArea_DragLeave(object sender, DragEventArgs e)
        {
            DropArea.Background = Brushes.White;
        }

        /// <summary>
        /// ファイルがドロップされた時：パスを取得し、CSVなら取り込む
        /// </summary>
        private void DropArea_Drop(object sender, DragEventArgs e)
        {
            DropArea.Background = Brushes.White;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // ドロップされた複数のファイルパスを取得
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 最初の1つのファイルのみ対象
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

        /// <summary>
        /// パスを確定させてウィンドウを閉じる共通処理
        /// </summary>
        private void CompleteImport(string path)
        {
            SelectedFilePath = path;
            this.DialogResult = true; // 呼び出し元(RibbonPanel)に成功を通知
            this.Close();
        }

        /// <summary>
        /// キャンセルボタン：処理を中止して閉じる
        /// </summary>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}