using System.Collections.Generic;
using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphSettingsWindow : Window
    {
        // メイン画面へ渡すための選択結果プロパティ
        public string SelectedX { get; private set; }
        public string SelectedY { get; private set; }
        public string SelectedType { get; private set; }

        public GraphSettingsWindow(List<string> columns)
        {
            InitializeComponent();
            // CSVから取得した列名をコンボボックスにセット
            ComboX.ItemsSource = columns;
            ComboY.ItemsSource = columns;
        }

        private void ShowGraph_Click(object sender, RoutedEventArgs e)
        {
            SelectedX = ComboX.Text;
            SelectedY = ComboY.Text;
            SelectedType = ComboType.Text;

            // バリデーション
            if (string.IsNullOrEmpty(SelectedX) || string.IsNullOrEmpty(SelectedY))
            {
                MessageBox.Show("X軸とY軸の両方を選択してください。", "入力確認");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}