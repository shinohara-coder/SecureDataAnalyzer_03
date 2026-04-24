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
            MySplitter.Visibility = Visibility.Collapsed;
            GraphColumn.Width = new GridLength(0);
            OpenGraphBtn.Visibility = Visibility.Visible;
        }

        private void OpenGraph_Click(object sender, RoutedEventArgs e)
        {
            GraphArea.Visibility = Visibility.Visible;
            MySplitter.Visibility = Visibility.Visible;
            GraphColumn.Width = new GridLength(1, GridUnitType.Star);
            OpenGraphBtn.Visibility = Visibility.Collapsed;
        }
    }
}