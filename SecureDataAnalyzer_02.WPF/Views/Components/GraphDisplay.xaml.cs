using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        public void AddNewGraph(string x, string y, string type)
        {
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。", "上限通知");
                return;
            }

            // 【修正ポイント】Borderの初期化を正しい文法に修正
            Border graphBorder = new Border
            {
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5)
            };

            Grid innerGrid = new Grid();
            Button closeBtn = new Button
            {
                Content = "×",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 25,
                Height = 25,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            closeBtn.Click += (s, e) => RemoveGraph(graphBorder);

            TextBlock infoText = new TextBlock
            {
                Text = $"【最新グラフ】\n形式：{type}\nX軸：{x}\nY軸：{y}",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                IsHitTestVisible = false // テキスト上でホイールしても邪魔しないように
            };

            innerGrid.Children.Add(infoText);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            // リストの先頭に挿入
            GraphContainer.Children.Insert(0, graphBorder);
        }

        private void RemoveGraph(Border target)
        {
            GraphContainer.Children.Remove(target);
            if (GraphContainer.Children.Count == 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.GraphArea.Visibility = Visibility.Collapsed;
                    mainWindow.MySplitter.Visibility = Visibility.Collapsed;
                    mainWindow.GraphColumn.Width = new GridLength(0);
                }
            }
        }

        /// <summary>
        /// マウスホイールスクロールの強制制御
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = sender as ScrollViewer;
            if (scv == null) return;

            // スクロール位置を計算（e.Deltaが負なら下へスクロール）
            double nextOffset = scv.VerticalOffset - e.Delta;

            if (nextOffset < 0) nextOffset = 0;
            else if (nextOffset > scv.ScrollableHeight) nextOffset = scv.ScrollableHeight;

            scv.ScrollToVerticalOffset(nextOffset);
            e.Handled = true; // イベントをここで完了させる
        }

        public void SetGraphVisibility(int number, bool isVisible) { }
    }
}