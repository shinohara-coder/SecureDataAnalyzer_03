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
        public GraphDisplay() => InitializeComponent();

        public void AddNewGraph(string xCol, string yCol, string type)
        {
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。", "上限通知");
                return;
            }

            // 1. データ取得
            var mainWindow = Window.GetWindow(this) as MainWindow;
            DataGrid dataGrid = mainWindow?.MyPreview?.FindName("DataGrid") as DataGrid ?? FindVisualChild<DataGrid>(mainWindow?.MyPreview);
            DataTable dt = (dataGrid?.ItemsSource as DataView)?.Table ?? dataGrid?.ItemsSource as DataTable;

            if (dt == null)
            {
                MessageBox.Show("データが見つかりません。");
                return;
            }

            // 2. 集計 (パターンB: X軸でグループ化)
            var summaryData = new Dictionary<string, double>();
            foreach (DataRow row in dt.Rows)
            {
                string key = row[xCol]?.ToString() ?? "不明";
                double val = 0;
                if (row[yCol] != DBNull.Value) double.TryParse(row[yCol].ToString(), out val);
                if (summaryData.ContainsKey(key)) summaryData[key] += val;
                else summaryData.Add(key, val);
            }

            // 3. UI構築
            Border graphBorder = new Border
            {
                Height = 220,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5)
            };
            Grid innerGrid = new Grid();
            Canvas canvas = new Canvas { Margin = new Thickness(25, 45, 25, 30) };

            // 削除ボタン
            Button closeBtn = new Button { Content = "×", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Width = 25, Height = 25, Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            closeBtn.Click += (s, e) => GraphContainer.Children.Remove(graphBorder);

            // 4. グラフ種類による分岐描画
            DrawGraph(canvas, summaryData, type);

            innerGrid.Children.Add(new TextBlock { Text = $"{type}: {xCol} / {yCol}", Margin = new Thickness(10, 5, 0, 0), FontWeight = FontWeights.Bold });
            innerGrid.Children.Add(canvas);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            GraphContainer.Children.Insert(0, graphBorder);
        }

        private void DrawGraph(Canvas canvas, Dictionary<string, double> data, string type)
        {
            if (!data.Any()) return;
            double maxVal = data.Values.Max();
            if (maxVal <= 0) maxVal = 1;
            var targetData = data.Take(8).ToList(); // 表示は最大8件に制限

            if (type.Contains("棒")) // 棒グラフ
            {
                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * 100;
                    Rectangle rect = new Rectangle { Width = 20, Height = h, Fill = Brushes.SteelBlue };
                    Canvas.SetLeft(rect, i * 40 + 10);
                    Canvas.SetBottom(rect, 20);
                    canvas.Children.Add(rect);
                    AddLabel(canvas, targetData[i].Key, i * 40 + 10);
                }
            }
            else if (type.Contains("折れ線")) // 折れ線グラフ
            {
                Polyline line = new Polyline { Stroke = Brushes.Crimson, StrokeThickness = 2 };
                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * 100;
                    Point p = new Point(i * 40 + 20, 100 - h);
                    line.Points.Add(p);

                    Ellipse dot = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Crimson, Margin = new Thickness(-3) };
                    Canvas.SetLeft(dot, p.X); Canvas.SetTop(dot, p.Y);
                    canvas.Children.Add(dot);
                    AddLabel(canvas, targetData[i].Key, i * 40 + 10);
                }
                canvas.Children.Add(line);
            }
            else if (type.Contains("円")) // 円グラフ
            {
                double total = data.Values.Sum();
                double currentAngle = 0;
                double centerX = 100, centerY = 50, radius = 45;
                var colors = new Brush[] { Brushes.Gold, Brushes.LightCoral, Brushes.LightSkyBlue, Brushes.LightGreen, Brushes.MediumPurple, Brushes.Orange };

                for (int i = 0; i < targetData.Count; i++)
                {
                    double sweepAngle = (targetData[i].Value / total) * 360;
                    Path path = CreatePieSlice(centerX, centerY, radius, currentAngle, sweepAngle, colors[i % colors.Length]);
                    canvas.Children.Add(path);
                    currentAngle += sweepAngle;
                }
            }
        }

        private Path CreatePieSlice(double centerX, double centerY, double radius, double startAngle, double sweepAngle, Brush fill)
        {
            // 円弧を描画するための計算
            double startRad = Math.PI * (startAngle - 90) / 180.0;
            double endRad = Math.PI * (startAngle + sweepAngle - 90) / 180.0;

            Point startPoint = new Point(centerX + radius * Math.Cos(startRad), centerY + radius * Math.Sin(startRad));
            Point endPoint = new Point(centerX + radius * Math.Cos(endRad), centerY + radius * Math.Sin(endRad));

            PathFigure figure = new PathFigure { StartPoint = new Point(centerX, centerY), IsClosed = true };
            figure.Segments.Add(new LineSegment(startPoint, true));
            figure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Clockwise, true));

            return new Path { Fill = fill, Stroke = Brushes.White, StrokeThickness = 1, Data = new PathGeometry(new[] { figure }) };
        }

        private void AddLabel(Canvas canvas, string text, double x)
        {
            TextBlock txt = new TextBlock { Text = text, FontSize = 9, Width = 40, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Canvas.SetLeft(txt, x - 10); Canvas.SetBottom(txt, 0);
            canvas.Children.Add(txt);
        }

        // --- 以前のヘルパーメソッド ---
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject { /* 以前と同じ */ for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) { DependencyObject child = VisualTreeHelper.GetChild(obj, i); if (child is T t) return t; T childItem = FindVisualChild<T>(child); if (childItem != null) return childItem; } return null; }
        public void SetGraphVisibility(int n, bool v) { }
        private void ScrollViewer_PreviewMouseWheel(object s, MouseWheelEventArgs e) { var scv = s as ScrollViewer; if (scv != null) { scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta); e.Handled = true; } }
    }
}