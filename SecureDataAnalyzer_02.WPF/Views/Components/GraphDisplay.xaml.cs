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

        /// <summary>
        /// 各グラフ内の「×」ボタン押下時の処理
        /// </summary>
        private void HideGraph_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            string targetName = btn.Tag.ToString();
            int graphNum = 0;

            // 1. ターゲットのグラフを隠す
            if (targetName == "Graph2") { Graph2.Visibility = Visibility.Collapsed; graphNum = 2; }
            else if (targetName == "Graph3") { Graph3.Visibility = Visibility.Collapsed; graphNum = 3; }
            else if (targetName == "Graph4") { Graph4.Visibility = Visibility.Collapsed; graphNum = 4; }
            else if (targetName == "Graph5") { Graph5.Visibility = Visibility.Collapsed; graphNum = 5; }
            else if (targetName == "Graph6") { Graph6.Visibility = Visibility.Collapsed; graphNum = 6; }

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // 2. リボンのチェックボックスをOFFにする（状態の保存）
                mainWindow.MyRibbon?.UpdateCheckBox(graphNum, false);

                // 3. すべてのグラフが消えたかチェック
                if (Graph2.Visibility == Visibility.Collapsed && Graph3.Visibility == Visibility.Collapsed &&
                    Graph4.Visibility == Visibility.Collapsed && Graph5.Visibility == Visibility.Collapsed &&
                    Graph6.Visibility == Visibility.Collapsed)
                {
                    // エリア全体を閉じ、ボタンの文言を切り替える
                    mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                    mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                    mainWindow.GraphColumn.Width = new GridLength(0);

                    if (mainWindow.MyRibbon != null)
                    {
                        mainWindow.MyRibbon.ToggleGraphAreaBtn.Content = "グラフ表示";
                    }
                }
            }
        }

        /// <summary>
        /// 外部（リボン）から表示状態を切り替えるためのメソッド
        /// </summary>
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