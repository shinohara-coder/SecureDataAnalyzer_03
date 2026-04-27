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

            var mainWindow = Window.GetWindow(this) as MainWindow;
            var dataGrid = FindVisualChild<DataGrid>(mainWindow?.MyPreview);
            DataTable dt = (dataGrid?.ItemsSource as DataView)?.Table ?? dataGrid?.ItemsSource as DataTable;

            if (dt == null || dt.Rows.Count == 0) return;

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
            catch { return; }

            // 各表示領域の高さを 200px に変更
            Border graphBorder = new Border
            {
                Height = 200,
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5)
            };

            Grid innerGrid = new Grid();
            Canvas canvas = new Canvas
            {
                Margin = new Thickness(0, 40, 0, 10), // 左右のマージンを 0 にして中央寄せを優先
                Width = 320,
                HorizontalAlignment = HorizontalAlignment.Center // これで真ん中に表示されます
            };

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

            DrawGraphLogic(canvas, summaryData, type);

            innerGrid.Children.Add(new TextBlock { Text = $"{type}: {xCol} / {yCol}", Margin = new Thickness(10, 5, 0, 0), FontWeight = FontWeights.Bold });
            innerGrid.Children.Add(canvas);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            GraphContainer.Children.Insert(0, graphBorder);
        }

        private void DrawGraphLogic(Canvas canvas, Dictionary<string, double> data, string type)
        {
            if (!data.Any()) return;

            if (type.Contains("円"))
            {
                DrawPieChart(canvas, data);
            }
            else
            {
                var targetData = data.OrderByDescending(d => d.Value).Take(10).ToList();
                double maxVal = targetData.Max(d => d.Value);
                if (maxVal <= 0) maxVal = 1;

                double availableWidth = 300;
                double stepX = availableWidth / Math.Max(targetData.Count, 1);
                double graphHeight = 80;

                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * graphHeight;
                    double xPos = i * stepX;

                    if (type.Contains("棒"))
                    {
                        Rectangle rect = new Rectangle
                        {
                            Width = 14,
                            Height = h,
                            Fill = Brushes.SteelBlue,
                            ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0}"
                        };
                        Canvas.SetLeft(rect, xPos);
                        Canvas.SetTop(rect, graphHeight - h);
                        canvas.Children.Add(rect);
                    }
                    else if (type.Contains("折れ線"))
                    {
                        if (i > 0)
                        {
                            double prevH = (targetData[i - 1].Value / maxVal) * graphHeight;
                            Line connector = new Line
                            {
                                X1 = (i - 1) * stepX + 7,
                                Y1 = graphHeight - prevH,
                                X2 = i * stepX + 7,
                                Y2 = graphHeight - h,
                                Stroke = Brushes.Crimson,
                                StrokeThickness = 2
                            };
                            canvas.Children.Add(connector);
                        }
                        Ellipse dot = new Ellipse
                        {
                            Width = 8,
                            Height = 8,
                            Fill = Brushes.Crimson,
                            Margin = new Thickness(-4),
                            ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0}"
                        };
                        Canvas.SetLeft(dot, xPos + 7); Canvas.SetTop(dot, graphHeight - h);
                        canvas.Children.Add(dot);
                    }

                    AddSlantedLabel(canvas, targetData[i].Key, xPos, graphHeight + 5);
                }
            }
        }

        private void AddSlantedLabel(Canvas c, string txt, double x, double top)
        {
            TextBlock tb = new TextBlock
            {
                Text = txt,
                FontSize = 9,
                Width = 70,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            tb.RenderTransformOrigin = new Point(0, 0);
            tb.LayoutTransform = new RotateTransform(45);

            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, top);
            c.Children.Add(tb);
        }

        private void DrawPieChart(Canvas canvas, Dictionary<string, double> data)
        {
            double totalAll = data.Values.Sum();
            if (totalAll <= 0) totalAll = 1;
            var targetData = data.OrderByDescending(d => d.Value).Take(8).ToList();
            double currentAngle = 0;
            double centerX = 150, centerY = 70, radius = 60;
            Brush[] colors = { Brushes.DodgerBlue, Brushes.Tomato, Brushes.Orange, Brushes.MediumSeaGreen, Brushes.MediumSlateBlue, Brushes.Gold, Brushes.HotPink, Brushes.Orchid };

            for (int i = 0; i < targetData.Count; i++)
            {
                double sweepAngle = (targetData[i].Value / totalAll) * 360;
                if (sweepAngle <= 0) continue;
                if (sweepAngle >= 360) sweepAngle = 359.9;
                Path slice = CreatePieSlice(centerX, centerY, radius, currentAngle, sweepAngle, colors[i % colors.Length]);
                slice.ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0} ({(targetData[i].Value / totalAll):P1})";
                canvas.Children.Add(slice);
                currentAngle += sweepAngle;
            }

            double othersSum = totalAll - targetData.Sum(d => d.Value);
            if (othersSum > 0.1)
            {
                double othersAngle = 360 - currentAngle;
                Path othersSlice = CreatePieSlice(centerX, centerY, radius, currentAngle, othersAngle, Brushes.LightGray);
                othersSlice.ToolTip = $"その他: {othersSum:#,0} ({(othersSum / totalAll):P1})";
                canvas.Children.Add(othersSlice);
            }
        }

        private Path CreatePieSlice(double cx, double cy, double r, double start, double sweep, Brush fill)
        {
            double sRad = Math.PI * (start - 90) / 180.0;
            double eRad = Math.PI * (start + sweep - 90) / 180.0;
            Point pStart = new Point(cx + r * Math.Cos(sRad), cy + r * Math.Sin(sRad));
            Point pEnd = new Point(cx + r * Math.Cos(eRad), cy + r * Math.Sin(eRad));
            var figure = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
            figure.Segments.Add(new LineSegment(pStart, true));
            figure.Segments.Add(new ArcSegment(pEnd, new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));
            return new Path { Fill = fill, Stroke = Brushes.White, StrokeThickness = 1, Data = new PathGeometry(new[] { figure }) };
        }

        public void SetGraphVisibility(int n, bool v) { }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var res = FindVisualChild<T>(child);
                if (res != null) return res;
            }
            return null;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = sender as ScrollViewer;
            if (scv != null) { scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta); e.Handled = true; }
        }
    }
}