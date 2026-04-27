using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SecureDataAnalyzer_02.WPF.Views.Components;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class RibbonPanel : UserControl
    {
        public RibbonPanel()
        {
            InitializeComponent();
        }

        // ==========================================
        // 1. CSV読込・プレビューロジック
        // ==========================================

        /// <summary>
        /// CSV読込ボタン押下時：読込用ポップアップウィンドウを開く
        /// </summary>
        private void CsvImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var importWin = new CsvImportWindow();
            // 親ウィンドウ（MainWindow）をセットして、その中央に表示されるようにする
            importWin.Owner = Window.GetWindow(this);

            // ウィンドウを「対話モード」で開き、OKボタン(DialogResult=true)で閉じられたか確認
            if (importWin.ShowDialog() == true)
            {
                // ウィンドウから取得したファイルパスを使って解析を開始
                LoadCsv(importWin.SelectedFilePath);
            }
        }

        /// <summary>
        /// 指定されたパスのCSVファイルを読み込み、最大1000行をDataPreviewに渡す
        /// </summary>
        private void LoadCsv(string filePath)
        {
            try
            {
                DataTable dt = new DataTable();

                // Microsoft.VisualBasic.FileIO.TextFieldParser はカンマ区切り内の
                // ダブルクォーテーション等も適切に処理してくれるため採用
                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath))
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(","); // カンマ区切りを指定

                    bool isFirstRow = true;
                    int rowCount = 0;

                    // ファイルの終端まで、または最大1000行に達するまでループ
                    while (!parser.EndOfData && rowCount < 1000)
                    {
                        string[] fields = parser.ReadFields(); // 1行分のデータを配列で取得

                        if (isFirstRow)
                        {
                            // 最初の1行目は「列名（ヘッダー）」としてDataTableの列を作成
                            foreach (string field in fields) dt.Columns.Add(field);
                            isFirstRow = false;
                        }
                        else
                        {
                            // 2行目以降は「データ」としてDataTableに行を追加
                            dt.Rows.Add(fields);
                            rowCount++;
                        }
                    }
                }

                // 表示先の DataPreview コントロールを MainWindow から探し出す
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // MainWindow.xaml で x:Name="MyPreview" と命名したインスタンスを取得
                    var previewControl = mainWindow.FindName("MyPreview") as DataPreview;
                    if (previewControl != null)
                    {
                        // 取得したDataTableを渡して画面を更新
                        previewControl.DisplayData(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                // ファイルが開けない、形式が違う等のエラー時にユーザーに通知
                MessageBox.Show($"CSVの読み込み中にエラーが発生しました:\n{ex.Message}",
                                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // 2. グラフ表示・非表示制御ロジック（既存）
        // ==========================================

        /// <summary>
        /// 右側グラフエリア全体の表示/非表示を一括で切り替える
        /// </summary>
        private void ToggleGraphAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (mainWindow.GraphArea.Visibility == Visibility.Visible)
            {
                // エリアを閉じ、ボタン名を「表示」に変更
                mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                mainWindow.GraphColumn.Width = new GridLength(0);
                ToggleGraphAreaBtn.Content = "グラフ表示";
            }
            else
            {
                // エリアを開き、ボタン名を「非表示」に変更
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);
                ToggleGraphAreaBtn.Content = "全グラフ非表示";

                // 再表示時は、リボンのチェックボックスの「現在の状態」をグラフに反映させる
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
        /// 個別のグラフ表示チェックボックスがクリックされた時の処理
        /// </summary>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;

            // XAMLのTagプロパティからグラフ番号(2〜6)を取得
            int graphNum = int.Parse(chk.Tag.ToString());

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
            {
                // チェック状態に合わせて個別のグラフ枠の表示を切り替え
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, chk.IsChecked ?? true);
            }
        }

        /// <summary>
        /// グラフ側の「×」ボタンで消された際に、リボンのチェックを外すための公開メソッド
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