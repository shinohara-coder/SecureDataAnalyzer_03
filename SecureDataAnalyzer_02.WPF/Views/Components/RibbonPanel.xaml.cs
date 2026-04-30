using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// アプリ上部のリボンメニュー操作を管理するコントロール
    /// </summary>
    public partial class RibbonPanel : UserControl
    {
        // 読込済みのCSV列名を保持するリスト（グラフ設定用）
        private List<string> _csvColumns = new List<string>();

        public RibbonPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 「CSV読込」ボタン：インポートダイアログを開き、結果を取得する
        /// </summary>
        private void CsvImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var importWin = new CsvImportWindow();
            importWin.Owner = Window.GetWindow(this); // 親ウィンドウの中央に出す設定
            if (importWin.ShowDialog() == true)
            {
                LoadCsv(importWin.SelectedFilePath);
            }
        }

        /// <summary>
        /// 指定されたパスのCSVファイルを解析し、DataTable化してプレビューへ送る
        /// </summary>
        private void LoadCsv(string filePath)
        {
            try
            {
                DataTable dt = new DataTable();
                int totalRows = 0;
                _csvColumns.Clear(); // 列名リストをリセット

                // 汎用的なCSV解析ライブラリTextFieldParserを使用
                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath))
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(",");

                    bool isFirstRow = true;
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (isFirstRow)
                        {
                            // 1行目は列名（ヘッダー）として処理
                            foreach (string field in fields)
                            {
                                dt.Columns.Add(field);
                                _csvColumns.Add(field);
                            }
                            isFirstRow = false;
                        }
                        else
                        {
                            // 2行目以降はデータ。プレビューの負荷を考え1,000件でカット
                            if (totalRows < 1000) dt.Rows.Add(fields);
                            totalRows++;
                        }
                    }
                }

                // UI（DataPreviewコントロール）へのデータの受け渡し
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    var previewControl = mainWindow.FindName("MyPreview") as DataPreview;
                    if (previewControl != null) previewControl.DisplayData(dt);
                    MessageBox.Show($"読込完了: {Path.GetFileName(filePath)} ({totalRows:N0}件)", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"エラー: {ex.Message}"); }
        }

        /// <summary>
        /// 「グラフ作成」ボタン：設定ウィンドウを開き、結果をGraphDisplayへ送る
        /// </summary>
        private void CreateGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            // 未ロード時のガード
            if (_csvColumns.Count == 0)
            {
                MessageBox.Show("先にCSVファイルを読み込んでください。", "通知");
                return;
            }

            var settingsWin = new GraphSettingsWindow(_csvColumns);
            settingsWin.Owner = Window.GetWindow(this);

            if (settingsWin.ShowDialog() == true)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow?.MyGraphContent != null)
                {
                    // GraphDisplayのAddNewGraphメソッドを実行してグラフ追加
                    mainWindow.MyGraphContent.AddNewGraph(settingsWin.SelectedX, settingsWin.SelectedY, settingsWin.SelectedType);

                    // グラフエリアが折りたたまれている場合は強制的に開く
                    if (mainWindow.GraphArea.Visibility != Visibility.Visible)
                    {
                        ToggleGraphAreaBtn_Click(null, null);
                    }
                }
            }
        }

        /// <summary>
        /// 「グラフ表示/非表示」の切り替えとレイアウトの動的変更
        /// </summary>
        private void ToggleGraphAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (mainWindow.GraphArea.Visibility == Visibility.Visible)
            {
                // 非表示にする処理
                mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                mainWindow.GraphColumn.Width = new GridLength(0); // 列の幅をゼロにする
                ToggleGraphAreaBtn.Content = "グラフ表示";
            }
            else
            {
                // 表示する処理
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star); // 幅を再割り当て
                ToggleGraphAreaBtn.Content = "全グラフ非表示";

                // チェックボックスの状態に合わせて個別のグラフ表示を更新（拡張用）
                if (mainWindow.MyGraphContent != null)
                {
                    mainWindow.MyGraphContent.SetGraphVisibility(2, Chk2.IsChecked ?? false);
                    mainWindow.MyGraphContent.SetGraphVisibility(3, Chk3.IsChecked ?? false);
                    mainWindow.MyGraphContent.SetGraphVisibility(4, Chk4.IsChecked ?? false);
                    mainWindow.MyGraphContent.SetGraphVisibility(5, Chk5.IsChecked ?? false);
                    mainWindow.MyGraphContent.SetGraphVisibility(6, Chk6.IsChecked ?? false);
                }
            }
        }

        /// <summary>
        /// 個別の表示チェックボックスがクリックされた時の処理（拡張用）
        /// </summary>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;
            int graphNum = int.Parse(chk.Tag.ToString());
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, chk.IsChecked ?? true);
        }

        /// <summary>
        /// 外部（GraphDisplay等）からチェックボックスの状態を変更するメソッド
        /// </summary>
        public void UpdateCheckBox(int graphNum, bool isChecked)
        {
            if (graphNum == 2) Chk2.IsChecked = isChecked;
            else if (graphNum == 3) Chk3.IsChecked = isChecked;
            else if (graphNum == 4) Chk4.IsChecked = isChecked;
            else if (graphNum == 5) Chk5.IsChecked = isChecked;
            else if (graphNum == 6) Chk6.IsChecked = isChecked;
        }
    }
}