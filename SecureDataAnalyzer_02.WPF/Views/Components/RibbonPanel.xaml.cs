using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class RibbonPanel : UserControl
    {
        // 読込済みのCSV列名を保持するリスト
        private List<string> _csvColumns = new List<string>();

        public RibbonPanel()
        {
            InitializeComponent();
        }

        private void CsvImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var importWin = new CsvImportWindow();
            importWin.Owner = Window.GetWindow(this);
            if (importWin.ShowDialog() == true)
            {
                LoadCsv(importWin.SelectedFilePath);
            }
        }

        private void LoadCsv(string filePath)
        {
            try
            {
                DataTable dt = new DataTable();
                int totalRows = 0;
                _csvColumns.Clear(); // 列名リストをリセット

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
                            foreach (string field in fields)
                            {
                                dt.Columns.Add(field);
                                _csvColumns.Add(field); // 列名を保存
                            }
                            isFirstRow = false;
                        }
                        else
                        {
                            if (totalRows < 1000) dt.Rows.Add(fields);
                            totalRows++;
                        }
                    }
                }

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
        /// [グラフ作成]ボタン：設定ウィンドウを開き、結果をGraphDisplayへ送る
        /// </summary>
        private void CreateGraphBtn_Click(object sender, RoutedEventArgs e)
        {
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
                    // グラフ作成処理を実行
                    mainWindow.MyGraphContent.AddNewGraph(settingsWin.SelectedX, settingsWin.SelectedY, settingsWin.SelectedType);

                    // もしグラフエリアが非表示なら自動的に表示させる
                    if (mainWindow.GraphArea.Visibility != Visibility.Visible)
                    {
                        ToggleGraphAreaBtn_Click(null, null);
                    }
                }
            }
        }

        // 既存の ToggleGraphAreaBtn_Click, CheckBox_Click, UpdateCheckBox は変更なしで維持
        private void ToggleGraphAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;
            if (mainWindow.GraphArea.Visibility == Visibility.Visible)
            {
                mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                mainWindow.GraphColumn.Width = new GridLength(0);
                ToggleGraphAreaBtn.Content = "グラフ表示";
            }
            else
            {
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);
                ToggleGraphAreaBtn.Content = "全グラフ非表示";
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
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;
            int graphNum = int.Parse(chk.Tag.ToString());
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, chk.IsChecked ?? true);
        }
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