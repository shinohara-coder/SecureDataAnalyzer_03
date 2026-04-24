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

        // グラフエリアの表示/非表示を切り替えるメインボタン
        private void ToggleGraphAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (mainWindow.GraphArea.Visibility == Visibility.Visible)
            {
                // --- 非表示にする処理（変更なし） ---
                mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                mainWindow.GraphColumn.Width = new GridLength(0);
                ToggleGraphAreaBtn.Content = "グラフ表示";
            }
            else
            {
                // --- 表示する処理（ここを修正） ---
                mainWindow.GraphArea.Visibility = Visibility.Visible;
                mainWindow.MySplitter.Visibility = Visibility.Visible;
                mainWindow.GraphColumn.Width = new GridLength(1, GridUnitType.Star);
                ToggleGraphAreaBtn.Content = "全グラフ非表示";

                // 【重要】中身のグラフ（GraphDisplay）も全て表示状態に戻す
                if (mainWindow.MyGraphContent != null)
                {
                    // 全てのグラフ（2〜6）を表示に戻し、チェックボックスも同期させる
                    for (int i = 2; i <= 6; i++)
                    {
                        mainWindow.MyGraphContent.SetGraphVisibility(i, true);
                        UpdateCheckBox(i, true);
                    }
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null || chk.Tag == null) return;
            int graphNum = int.Parse(chk.Tag.ToString());

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null && mainWindow.MyGraphContent != null)
            {
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, chk.IsChecked ?? true);
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
    }
}