using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SecureDataAnalyzer_02.WPF.Services;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// アプリ上部のリボンメニュー操作を管理するコントロール
    /// </summary>
    public partial class RibbonPanel : UserControl
    {
        private List<string> _csvColumns = new List<string>();

        public RibbonPanel()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────
        // CSV 読込
        // ─────────────────────────────────────────────────

        private void CsvImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var importWin = new CsvImportWindow();
            importWin.Owner = Window.GetWindow(this);
            if (importWin.ShowDialog() == true)
            {
                _ = LoadCsvAsync(importWin.SelectedFilePath);
            }
        }

        /// <summary>
        /// CSV を検証してから SQLite DB + DataGrid プレビューへ読み込む。
        /// 検証失敗時は DataGrid への表示も行わない。
        /// </summary>
        private async System.Threading.Tasks.Task LoadCsvAsync(string filePath)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                var previewControl = mainWindow?.FindName("MyPreview") as DataPreview;
                if (previewControl == null) return;

                // ── (1) スキーマ検証 + SQLite DB へインポート ─────────────────
                //   ImportAsync 内でヘッダー検証を行い、不一致なら例外をスロー。
                //   例外が発生した場合は DataGrid への表示も行わない。
                var csvSvc = new CsvImportService(previewControl.DbService);
                int imported = await csvSvc.ImportAsync(filePath);

                // ── (2) 検証通過後のみ DataGrid プレビュー用 DataTable を構築 ─
                DataTable dt = new DataTable();
                int totalRows = 0;
                _csvColumns.Clear();

                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath))
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(",");

                    bool isFirstRow = true;
                    while (!parser.EndOfData)
                    {
                        string[]? fields = parser.ReadFields();
                        if (fields == null) continue;

                        if (isFirstRow)
                        {
                            foreach (string field in fields)
                            {
                                dt.Columns.Add(field);
                                _csvColumns.Add(field);
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

                previewControl.DisplayData(dt);

                MessageBox.Show(
                    $"読込完了: {Path.GetFileName(filePath)}\n" +
                    $"表示件数: {Math.Min(totalRows, 1000):N0} 件（プレビュー上限 1,000 件）\n" +
                    $"DB 登録数: {imported:N0} 件",
                    "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                // ヘッダー不一致など、仕様外CSVの場合は警告ダイアログ（DataGrid は更新しない）
                MessageBox.Show(ex.Message, "読み込み不可",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"CSV 読込エラー: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────
        // デイリーCSV 読込
        // ─────────────────────────────────────────────────

        private void DailyCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            var importWin = new CsvImportWindow();
            importWin.Owner = Window.GetWindow(this);
            if (importWin.ShowDialog() == true)
            {
                _ = LoadDailyCsvAsync(importWin.SelectedFilePath);
            }
        }

        /// <summary>
        /// デイリー CSV をスキーマ検証・DB 追記してから DataGrid に表示する。
        /// 検証失敗時は DataGrid を更新しない。
        /// </summary>
        private async System.Threading.Tasks.Task LoadDailyCsvAsync(string filePath)
        {
            try
            {
                var mainWindow    = Window.GetWindow(this) as MainWindow;
                var previewControl = mainWindow?.FindName("MyPreview") as DataPreview;
                if (previewControl == null) return;

                // ── (1) スキーマ検証 + DB 追記（失敗なら例外） ───────────────
                var svc = new DailyCsvImportService(previewControl.DbService);
                var (added, total, isFirst) = await svc.ImportAsync(filePath);

                // ── (2) 検証通過後のみ DataGrid に表示 ───────────────────────
                DataTable dt = new DataTable();
                int totalRows = 0;
                _csvColumns.Clear();

                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath))
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(",");

                    bool isFirstRow = true;
                    while (!parser.EndOfData)
                    {
                        string[]? fields = parser.ReadFields();
                        if (fields == null) continue;

                        if (isFirstRow)
                        {
                            foreach (string field in fields)
                            {
                                dt.Columns.Add(field);
                                _csvColumns.Add(field);
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

                previewControl.DisplayData(dt);

                var label = isFirst ? "新規テーブル作成＆読込完了" : "追記完了";
                MessageBox.Show(
                    $"デイリーCSV {label}: {Path.GetFileName(filePath)}\n" +
                    $"今回追記: {added:N0} 件\n" +
                    $"テーブル累計: {total:N0} 件",
                    "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (InvalidOperationException ex)
            {
                // スキーマ不一致など仕様外CSVの場合は警告（DataGrid は更新しない）
                MessageBox.Show(ex.Message, "読み込み不可",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"デイリーCSV 読込エラー: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─────────────────────────────────────────────────
        // グラフ作成
        // ─────────────────────────────────────────────────

        private void CreateGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_csvColumns.Count == 0)
            {
                MessageBox.Show("先に CSV ファイルを読み込んでください。", "通知");
                return;
            }

            var settingsWin = new GraphSettingsWindow(_csvColumns);
            settingsWin.Owner = Window.GetWindow(this);

            if (settingsWin.ShowDialog() == true)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow?.MyGraphContent != null)
                {
                    mainWindow.MyGraphContent.AddNewGraph(
                        settingsWin.SelectedX, settingsWin.SelectedY, settingsWin.SelectedType);

                    if (mainWindow.GraphArea.Visibility != Visibility.Visible)
                        ToggleGraphAreaBtn_Click(null, null);
                }
            }
        }

        // ─────────────────────────────────────────────────
        // グラフエリア 表示 / 非表示
        // ─────────────────────────────────────────────────

        private void ToggleGraphAreaBtn_Click(object? sender, RoutedEventArgs? e)
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
            if (chk?.Tag == null) return;
            int graphNum = int.Parse(chk.Tag.ToString()!);
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow?.MyGraphContent != null)
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
