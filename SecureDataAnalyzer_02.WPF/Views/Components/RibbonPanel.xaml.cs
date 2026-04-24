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

        // MainWindowからボタンの表示状態を切り替えるための窓口
        public void SetShowButtonVisibility(Visibility vis)
        {
            ShowGraphBtn.Visibility = vis;
        }

        // 「グラフエリアを表示」ボタンが押された時の処理
        private void ShowGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            // 親ウィンドウ(MainWindow)を取得して操作する
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // グラフを表示状態に戻す
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);

                // このボタン自体は隠す
                this.ShowGraphBtn.Visibility = Visibility.Collapsed;
            }
        }
    }
}