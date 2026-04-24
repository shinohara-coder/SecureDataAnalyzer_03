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

        public void SetShowButtonVisibility(Visibility vis)
        {
            ShowGraphBtn.Visibility = vis;
        }

        private void ShowGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);

                // 【追加】境界線（スプリッター）を再表示する
                mainWindow.MySplitter.Visibility = Visibility.Visible;

                this.ShowGraphBtn.Visibility = Visibility.Collapsed;
            }
        }
    }
}