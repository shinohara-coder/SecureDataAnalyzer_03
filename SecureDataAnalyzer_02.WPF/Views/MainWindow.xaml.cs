using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // リボンパネルの表示/非表示を切り替える
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

        // 【注意】以前ここにあった CloseGraph_Click と OpenGraph_Click は、
        // リボン側のボタンに統合されたため、MainWindow.xaml から呼び出しがなければ削除可能です。
    }
}