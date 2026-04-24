using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // リボンパネルの表示・非表示（格納）を切り替える
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
            GraphColumn.Width = new GridLength(0);

            // リボン内の「再表示ボタン」を出すように命令する
            MyRibbon.SetShowButtonVisibility(Visibility.Visible);
        }
    }
}