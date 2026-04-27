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

        /// <summary>
        /// リボンパネルの「グラフ作成」ボタンから呼ばれるメインメソッド
        /// </summary>
        public void AddNewGraph(string xCol, string yCol, string type)
        {
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。", "上限通知");
                return;
            }

            // 1. データ取得（ビジュアルツリーからDataGridを確実に探す）
            var mainWindow = Window.GetWindow(this) as MainWindow;
            var dataGrid = FindVisualChild<DataGrid>(mainWindow?.MyPreview);

            DataTable dt = (dataGrid?.ItemsSource as DataView)?.Table ?? dataGrid?.ItemsSource as DataTable;

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("グラフ化するデータが見つかりません。CSVを読み込んでください。");
                return;
            }

            // 2. データ集計（X軸でグループ化、Y軸を合計）
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
                MessageBox.Show($"集計中にエラーが発生しました。数値列を選択しているか確認してください。\n詳細: {ex.Message}");
                return;
            }

            // 3. グラフ外枠の構築
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
            Canvas canvas = new Canvas { Margin = new Thickness(25, 45, 25, 35) };

            // 削除ボタン
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

            // 4. グラフ種類による描画分岐
            DrawGraphLogic(canvas, summaryData, type);

            // ラベル・コンテナ追加
            innerGrid.Children.Add(new TextBlock { Text = $"{type}: {xCol} / {yCol}", Margin = new Thickness(10, 5, 0, 0), FontWeight = FontWeights.Bold });
            innerGrid.Children.Add(canvas);
            innerGrid.Children.Add(closeBtn);
            graphBorder.Child = innerGrid;

            // 常に最新を一番上に表示
            GraphContainer.Children.Insert(0, graphBorder);
        }

        private void DrawGraphLogic(Canvas canvas, Dictionary<string, double> data, string type)
        {
            if (!data.Any()) return;

            // 円グラフ用に「全データ」の総計を保持
            double totalAll = data.Values.Sum();
            if (totalAll <= 0) totalAll = 1;

            // 上位8件を抽出（視認性のため）
            var targetData = data.OrderByDescending(d => d.Value).Take(8).ToList();
            double maxVal = targetData.Any() ? targetData.Max(d => d.Value) : 1;

            if (type.Contains("棒"))
            {
                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * 100;
                    Rectangle rect = new Rectangle { Width = 20, Height = h, Fill = Brushes.SteelBlue };
                    Canvas.SetLeft(rect, i * 45 + 10); Canvas.SetBottom(rect, 20);
                    canvas.Children.Add(rect);
                    AddLabel(canvas, targetData[i].Key, i * 45 + 10);
                }
            }
            else if (type.Contains("折れ線"))
            {
                Polyline poly = new Polyline { Stroke = Brushes.Crimson, StrokeThickness = 2 };
                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * 100;
                    Point p = new Point(i * 45 + 20, 100 - h);
                    poly.Points.Add(p);
                    Ellipse dot = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Crimson, Margin = new Thickness(-3) };
                    Canvas.SetLeft(dot, p.X); Canvas.SetTop(dot, p.Y);
                    canvas.Children.Add(dot);
                    AddLabel(canvas, targetData[i].Key, i * 45 + 10);
                }
                canvas.Children.Add(poly);
            }
            else if (type.Contains("円"))
            {
                double currentAngle = 0;
                double centerX = 80, centerY = 60, radius = 55;
                Brush[] colors = { Brushes.DodgerBlue, Brushes.Tomato, Brushes.Orange, Brushes.MediumSeaGreen, Brushes.MediumSlateBlue, Brushes.Gold, Brushes.HotPink, Brushes.Orchid };

                for (int i = 0; i < targetData.Count; i++)
                {
                    double sweepAngle = (targetData[i].Value / totalAll) * 360;
                    if (sweepAngle <= 0) continue;
                    if (sweepAngle >= 360) sweepAngle = 359.99;

                    Path slice = CreatePieSlice(centerX, centerY, radius, currentAngle, sweepAngle, colors[i % colors.Length]);
                    slice.ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0} ({(targetData[i].Value / totalAll):P1})";
                    canvas.Children.Add(slice);
                    currentAngle += sweepAngle;
                }

                // 「その他」の描画（全体から上位8件を引いた残り）
                double othersSum = totalAll - targetData.Sum(d => d.Value);
                if (othersSum > 0.1) // 誤差考慮
                {
                    double othersAngle = (othersSum / totalAll) * 360;
                    if (currentAngle + othersAngle > 360) othersAngle = 360 - currentAngle;

                    Path othersSlice = CreatePieSlice(centerX, centerY, radius, currentAngle, othersAngle, Brushes.LightGray);
                    othersSlice.ToolTip = $"その他: {othersSum:#,0} ({(othersSum / totalAll):P1})";
                    canvas.Children.Add(othersSlice);
                }
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

        private void AddLabel(Canvas c, string txt, double x)
        {
            TextBlock tb = new TextBlock { Text = txt, FontSize = 9, Width = 40, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Canvas.SetLeft(tb, x - 10); Canvas.SetBottom(tb, 0);
            c.Children.Add(tb);
        }

        // ビルドエラー回避用の空メソッド
        public void SetGraphVisibility(int number, bool isVisible) { }

        // ビジュアルツリー探索用ヘルパー（DataGrid特定用）
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

        // マウスホイールでのスクロール制御
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = sender as ScrollViewer;
            if (scv != null)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}