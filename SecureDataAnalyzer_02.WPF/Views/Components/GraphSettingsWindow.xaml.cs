using System.Collections.Generic;
using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// グラフの軸と形式をユーザーに選択させる設定ウィンドウ
    /// </summary>
    public partial class GraphSettingsWindow : Window
    {
        // メイン画面へ渡すための選択結果プロパティ
        public string SelectedX { get; private set; }
        public string SelectedY { get; private set; }
        public string SelectedType { get; private set; }

        /// <summary>
        /// コンストラクタ：CSVのヘッダー一覧を受け取ってコンボボックスにセット
        /// </summary>
        public GraphSettingsWindow(List<string> columns)
        {
            InitializeComponent();
            // CSVから取得した列名をコンボボックスの選択肢として設定
            ComboX.ItemsSource = columns;
            ComboY.ItemsSource = columns;
        }

        /// <summary>
        /// 「グラフを表示」ボタンクリック時：入力を確定させて閉じる
        /// </summary>
        private void ShowGraph_Click(object sender, RoutedEventArgs e)
        {
            SelectedX = ComboX.Text;
            SelectedY = ComboY.Text;
            SelectedType = ComboType.Text;

            // 必須入力チェック
            if (string.IsNullOrEmpty(SelectedX) || string.IsNullOrEmpty(SelectedY))
            {
                MessageBox.Show("X軸とY軸の両方を選択してください。", "入力確認");
                return;
            }

            this.DialogResult = true; // OKの結果を返す
            this.Close();
        }
    }
}