using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SecureDataAnalyzer_02.WPF.Views.Components;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// 画面上部のリボンパネルを制御するクラス
    /// CSV読込、グラフの表示・非表示、チェックボックスの連動を管理します
    /// </summary>
    public partial class RibbonPanel : UserControl
    {
        public RibbonPanel()
        {
            InitializeComponent();
        }

        // ==========================================
        // 1. CSV読込・プレビュー表示機能
        // ==========================================

        /// <summary>
        /// [CSV読込]ボタンクリック：ファイル選択ウィンドウを起動
        /// </summary>
        private void CsvImportBtn_Click(object sender, RoutedEventArgs e)
        {
            // カスタムウィンドウ（CsvImportWindow）のインスタンス生成
            var importWin = new CsvImportWindow();
            // メインウィンドウの中央に表示されるよう設定
            importWin.Owner = Window.GetWindow(this);

            // ウィンドウをモーダル表示（閉じるまで操作不可）し、OK(DialogResult=true)なら実行
            if (importWin.ShowDialog() == true)
            {
                // ウィンドウが保持しているファイルパスを解析メソッドへ渡す
                LoadCsv(importWin.SelectedFilePath);
            }
        }

        /// <summary>
        /// CSVの解析、プレビュー生成、およびユーザーへの完了通知を行います
        /// </summary>
        /// <param name="filePath">読み込むCSVのフルパス</param>
        private void LoadCsv(string filePath)
        {
            try
            {
                DataTable dt = new DataTable(); // DataGrid表示用のデータ格納庫
                int totalRows = 0;              // 全データの行数カウント用

                // --- 【解析フェーズ】 ---
                // TextFieldParserを使用して、CSVの特殊ルール（カンマ入りデータ等）を考慮して読み込む
                using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath))
                {
                    parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                    parser.SetDelimiters(","); // カンマ区切り

                    bool isFirstRow = true;

                    // ファイルの終端まで1行ずつループ処理
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields(); // 1行分の列データを配列で取得
                        if (fields == null) continue;

                        if (isFirstRow)
                        {
                            // 【列定義】1行目はヘッダーとしてDataTableの列名に設定
                            foreach (string field in fields) dt.Columns.Add(field);
                            isFirstRow = false;
                        }
                        else
                        {
                            // 【データ取得】プレビュー用に最初の1000行だけDataTableに追加
                            if (totalRows < 1000)
                            {
                                dt.Rows.Add(fields);
                            }
                            // プレビューの制限に関わらず、実際の総データ数はカウントし続ける
                            totalRows++;
                        }
                    }
                }

                // --- 【表示フェーズ】 ---
                // メインウィンドウを経由して、DataPreviewコントロールへデータを届ける
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // MainWindow.xamlで命名した「MyPreview」を探し出す
                    var previewControl = mainWindow.FindName("MyPreview") as DataPreview;
                    if (previewControl != null)
                    {
                        // DataPreview内のDataGridにDataTableをバインド
                        previewControl.DisplayData(dt);
                    }

                    // --- 【通知フェーズ】 ---
                    // ファイル名のみを抽出し、総件数とともにメッセージボックスで通知
                    string fileName = Path.GetFileName(filePath);
                    MessageBox.Show(
                        $"CSVファイルの読み込みが正常に完了しました。\n\n" +
                        $"ファイル名: {fileName}\n" +
                        $"総データ数: {totalRows:N0} 件", // :N0 は3桁カンマ区切りフォーマット
                        "読み込み完了",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // ファイル占有中や不正なフォーマットなどのエラーをキャッチ
                MessageBox.Show($"CSVの読み込み中にエラーが発生しました:\n{ex.Message}",
                                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // 2. グラフエリア表示・非表示制御（既存機能）
        // ==========================================

        /// <summary>
        /// [全グラフ非表示/表示]ボタンクリック：右側エリア全体の開閉
        /// </summary>
        private void ToggleGraphAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            // 現在表示されているなら隠す
            if (mainWindow.GraphArea.Visibility == Visibility.Visible)
            {
                mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                mainWindow.GraphColumn.Width = new GridLength(0); // 列幅をゼロにして隠す
                ToggleGraphAreaBtn.Content = "グラフ表示";
            }
            else
            {
                // 隠れているなら表示する
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);
                ToggleGraphAreaBtn.Content = "全グラフ非表示";

                // 【状態復元】再表示時は、チェックボックスの今の状態をグラフ側に同期させる
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
        /// 個別グラフのチェックボックスクリック：特定のグラフのみ表示・非表示
        /// </summary>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;

            // XAML側のTagに設定した番号(2〜6)を取得
            int graphNum = int.Parse(chk.Tag.ToString());

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
            {
                // チェック状態(True/False)をGraphDisplayへ通知
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, chk.IsChecked ?? true);
            }
        }

        /// <summary>
        /// グラフ側の「×」ボタン押下時に、リボン側のチェックを同期させるためのメソッド
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