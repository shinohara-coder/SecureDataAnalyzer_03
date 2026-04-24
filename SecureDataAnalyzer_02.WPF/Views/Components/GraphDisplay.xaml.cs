using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        private void HideGraph_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string targetName = btn.Tag.ToString();
            int graphNum = 0;

            if (targetName == "Graph2") { Graph2.Visibility = Visibility.Collapsed; graphNum = 2; }
            else if (targetName == "Graph3") { Graph3.Visibility = Visibility.Collapsed; graphNum = 3; }
            else if (targetName == "Graph4") { Graph4.Visibility = Visibility.Collapsed; graphNum = 4; }
            else if (targetName == "Graph5") { Graph5.Visibility = Visibility.Collapsed; graphNum = 5; }
            else if (targetName == "Graph6") { Graph6.Visibility = Visibility.Collapsed; graphNum = 6; }

            // MainWindow経由でリボンのチェックを外す
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyRibbon != null)
            {
                mainWindow.MyRibbon.UpdateCheckBox(graphNum, false);
            }
        }

        public void SetGraphVisibility(int number, bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            if (number == 2) Graph2.Visibility = visibility;
            else if (number == 3) Graph3.Visibility = visibility;
            else if (number == 4) Graph4.Visibility = visibility;
            else if (number == 5) Graph5.Visibility = visibility;
            else if (number == 6) Graph6.Visibility = visibility;
        }
    }
}