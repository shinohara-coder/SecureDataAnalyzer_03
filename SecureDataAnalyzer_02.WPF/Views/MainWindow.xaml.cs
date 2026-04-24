using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

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

        private void CloseGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphArea.Visibility = Visibility.Collapsed;
            GraphColumn.Width = new GridLength(0);

            // 【追加】境界線（スプリッター）も隠す
            MySplitter.Visibility = Visibility.Collapsed;

            MyRibbon.SetShowButtonVisibility(Visibility.Visible);
        }
    }
}