using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // リボン格納
        private void ToggleRibbon_Click(object sender, RoutedEventArgs e)
        {
            if (MyRibbon.Visibility == Visibility.Visible)
            {
                MyRibbon.Visibility = Visibility.Collapsed;
                ToggleRibbonBtn.Content = "▼";
            }
            else
            {
                MyRibbon.Visibility = Visibility.Visible;
                ToggleRibbonBtn.Content = "▲";
            }
        }

        // グラフエリアを閉じる（×ボタン）
        private void CloseGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphArea.Visibility = Visibility.Collapsed;
            MySplitter.Visibility = Visibility.Collapsed;
            // 右側の列を完全に消す
            GraphColumn.Width = new GridLength(0);

            // 再表示ボタンを出す
            OpenGraphBtn.Visibility = Visibility.Visible;
        }

        // グラフエリアを再表示する
        private void OpenGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphArea.Visibility = Visibility.Visible;
            MySplitter.Visibility = Visibility.Visible;
            // 右側の列を元の比率に戻す
            GraphColumn.Width = new GridLength(1, GridUnitType.Star);

            // 自分（再表示ボタン）を隠す
            OpenGraphBtn.Visibility = Visibility.Collapsed;
        }
    }
}