using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Data;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        public void AddNewGraph(string xCol, string yCol, string type)
        {
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。", "上限通知");
                return;
            }

            // 1. データ取得ロジックの強化
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow?.MyPreview == null) return;

            // ContentPresenter等を経由している場合があるため、名前で探す
            DataGrid dataGrid = mainWindow.MyPreview.FindName("DataGrid") as DataGrid;

            // それでも見つからない場合、ビジュアルツリーから探す予備手段（念のため）
            if (dataGrid == null)
            {
                dataGrid = FindVisualChild<DataGrid>(mainWindow.MyPreview);
            }

            if (dataGrid == null || dataGrid.ItemsSource == null)
            {
                MessageBox.Show("グラフ化するデータが見つかりません。CSVを読み込んでください。", "データ未検出");
                return;
            }

            // DataTableの取得をより柔軟に
            DataTable dt = null;
            if (dataGrid.ItemsSource is DataView dv) dt = dv.Table;
            else if (dataGrid.ItemsSource is DataTable table) dt = table;

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("有効なデータテーブルが見つかりませんでした。", "エラー");
                return;
            }

            // 2. データ集計（パターンB）
            var summaryData = new Dictionary<string, double>();
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    string key = row[xCol]?.ToString() ?? "不明";
                    double val = 0;
                    if (row[yCol] != DBNull.Value) double.TryParse(row[yCol].ToString(), out val);

                    if (summaryData.ContainsKey(key)) summaryData[key] += val;
                    else summaryData.Add(key, val);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"集計エラー: {ex.Message}");
                return;
            }

            // 3. UI構築
            Border graphBorder = new Border
            {
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)), // 安全な書き方に修正
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
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => GraphContainer.Children.Remove(graphBorder);

            Canvas canvas = new Canvas { Margin = new Thickness(20, 40, 20, 30) };

            if (summaryData.Any())
            {
                double maxVal = summaryData.Values.Max();
                if (maxVal <= 0) maxVal = 1;
                double canvasHeight = 100; // 描画可能高さ
                double barWidth = 25;
                double xPos = 10;

                foreach (var item in summaryData.Take(8))
                {
                    double h = (item.Value / maxVal) * canvasHeight;
                    Rectangle rect = new Rectangle { Width = barWidth, Height = h, Fill = Brushes.SteelBlue };
                    Canvas.SetLeft(rect, xPos);
                    Canvas.SetBottom(rect, 20);

                    TextBlock txt = new TextBlock
                    {
                        Text = item.Key,
                        FontSize = 9,
                        Width = 45,
                        TextAlignment = TextAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    Canvas.SetLeft(txt, xPos - 10);
                    Canvas.SetBottom(txt, 0);

                    canvas.Children.Add(rect);
                    canvas.Children.Add(txt);
                    xPos += 50;
                }
            }

            innerGrid.Children.Add(new TextBlock { Text = $"{type}: {xCol} / {yCol}", Margin = new Thickness(10, 5, 0, 0), FontWeight = FontWeights.Bold });
            innerGrid.Children.Add(canvas);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            GraphContainer.Children.Insert(0, graphBorder);
        }

        // ビジュアルツリーから子要素を探すヘルパー
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t) return t;
                T childItem = FindVisualChild<T>(child);
                if (childItem != null) return childItem;
            }
            return null;
        }

        public void SetGraphVisibility(int number, bool isVisible) { }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = sender as ScrollViewer;
            if (scv != null)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}