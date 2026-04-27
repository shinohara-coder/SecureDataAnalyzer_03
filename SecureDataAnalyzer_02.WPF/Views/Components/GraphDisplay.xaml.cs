using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 新しいグラフを作成し、最上部に追加します。上限は5個です。
        /// </summary>
        public void AddNewGraph(string x, string y, string type)
        {
            // 1. 上限チェック（現在表示されているグラフの数を数える）
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。\n新しいグラフを作成するには、既存のグラフを削除してください。",
                                "上限に達しました", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. グラフ枠（Border）を動的に生成
            Border graphBorder = new Border
            {
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 250)),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5)
            };

            // 3. 内部のレイアウト（Grid）
            Grid innerGrid = new Grid();

            // 削除ボタン（×）
            Button closeBtn = new Button
            {
                Content = "×",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 25,
                Height = 25,
                Margin = new Thickness(0, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += (s, e) => RemoveGraph(graphBorder); // 削除イベント

            // 情報テキスト
            TextBlock infoText = new TextBlock
            {
                Text = $"【最新グラフ】\n形式：{type}\nX軸：{x}\nY軸：{y}",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.Black
            };

            innerGrid.Children.Add(infoText);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            // 4. ★最重要：最上部（インデックス0）に挿入
            GraphContainer.Children.Insert(0, graphBorder);

            // 5. リボン側のチェックボックスとの同期（オプション）
            // ※動的生成の場合、固定のChk2〜6との紐付けが難しいため、
            // 　必要に応じてリボン側のUIも動的にするか、現状はメッセージ表示を優先します。
        }

        /// <summary>
        /// 特定のグラフを削除する
        /// </summary>
        private void RemoveGraph(Border target)
        {
            GraphContainer.Children.Remove(target);

            // 全て消えたらエリアを閉じる処理
            if (GraphContainer.Children.Count == 0)
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
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
        /// 既存のチェックボックス連動用メソッド（互換性のために残す）
        /// </summary>
        public void SetGraphVisibility(int number, bool isVisible)
        {
            // 動的生成に変更したため、インデックスに基づいた制御が必要な場合はここで実装
            // 今回は「新しい順に上」という挙動を優先しています
        }
    }
}