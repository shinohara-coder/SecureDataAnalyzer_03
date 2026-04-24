using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class RibbonPanel : UserControl
    {
        public RibbonPanel()
        {
            InitializeComponent();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;

            int graphNum = int.Parse(chk.Tag.ToString());
            bool isVisible = chk.IsChecked ?? true;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
            {
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, isVisible);
            }
        }

        public void UpdateCheckBox(int graphNum, bool isChecked)
        {
            if (graphNum == 2) Chk2.IsChecked = isChecked;
            else if (graphNum == 3) Chk3.IsChecked = isChecked;
            else if (graphNum == 4) Chk4.IsChecked = isChecked;
            else if (graphNum == 5) Chk5.IsChecked = isChecked;
            else if (graphNum == 6) Chk6.IsChecked = isChecked;
        }

        private void ShowGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);
                mainWindow.OpenGraphBtn.Visibility = Visibility.Collapsed;
                this.ShowGraphBtn.Visibility = Visibility.Collapsed;
            }
        }
    }
}