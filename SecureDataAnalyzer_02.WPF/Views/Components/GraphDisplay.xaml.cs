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
    /// <summary>
    /// グラフの描画・管理（生成・削除・スタック）を行うコントロール
    /// </summary>
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 新しいグラフカードを生成して画面に追加する
        /// </summary>
        /// <param name="xCol">X軸に使用する列名</param>
        /// <param name="yCol">Y軸に使用する数値列名</param>
        /// <param name="type">グラフ形式（棒、折れ線、円）</param>
        public void AddNewGraph(string xCol, string yCol, string type)
        {
            // グラフの最大表示数を制限（リソース管理）
            if (GraphContainer.Children.Count >= 5)
            {
                MessageBox.Show("表示できるグラフの数が上限（5個）に達しました。", "上限通知");
                return;
            }

            // メインウィンドウを経由してプレビュー用のDataGridから生データを取得
            var mainWindow = Window.GetWindow(this) as MainWindow;
            var dataGrid = FindVisualChild<DataGrid>(mainWindow?.MyPreview);
            DataTable dt = (dataGrid?.ItemsSource as DataView)?.Table ?? dataGrid?.ItemsSource as DataTable;

            if (dt == null || dt.Rows.Count == 0) return;

            // データの集計ロジック（X軸でグループ化、Y軸を合計）
            var summaryData = new Dictionary<string, double>();
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    string key = row[xCol]?.ToString() ?? "不明";     //X軸を取り出す
                    double val = 0;
                    if (row[yCol] != DBNull.Value) double.TryParse(row[yCol].ToString(), out val);  //Y軸を取り出す

                    if (summaryData.ContainsKey(key)) summaryData[key] += val;      //key毎に値を合計する
                    else summaryData.Add(key, val);                                 //初めて出てきた項目なら、新しいkey、valのペアを作る
                }
            }
            catch { return; }

            // グラフカードの外枠（デザイン用Border）の設定
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

            // 描画用のCanvas設定（横幅320px、中央寄せ）
            Canvas canvas = new Canvas
            {
                Margin = new Thickness(0, 40, 0, 10),
                Width = 320,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // グラフ削除用の「×」ボタン生成
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
            // 削除ボタンクリック時に自身のBorderをコンテナから削除
            // s (object sender):/ e (RoutedEventArgs e):
            // closeBtn.Clickにイベントハンドラを登録
            closeBtn.Click += (s, e) => GraphContainer.Children.Remove(graphBorder);

            // グラフ描画実行
            DrawGraphLogic(canvas, summaryData, type);

            // カード内レイアウトの組み立て（タイトル、描画エリア、ボタン）
            innerGrid.Children.Add(new TextBlock { Text = $"{type}: {xCol} / {yCol}", Margin = new Thickness(10, 5, 0, 0), FontWeight = FontWeights.Bold });    // 1.タイトルを載せる
            innerGrid.Children.Add(canvas);                             // 2.グラフを載せる
            innerGrid.Children.Add(closeBtn);                           // 3.ボタンを載せる
            graphBorder.Child = innerGrid;                              // まとめて枠(graphBorder)の中へ！

            // StackPanelの先頭（一番上）に追加
            GraphContainer.Children.Insert(0, graphBorder);
        }

        /// <summary>
        /// グラフ形式に応じた具体的な描画ロジックの分岐
        /// </summary>
        private void DrawGraphLogic(Canvas canvas, Dictionary<string, double> data, string type)
        {
            if (!data.Any()) return;

            if (type.Contains("円"))
            {
                DrawPieChart(canvas, data);
            }
            else
            {
                // 棒グラフ・折れ線グラフ：上位10件を表示対象とする
                var targetData = data.OrderByDescending(d => d.Value).Take(10).ToList();
                double maxVal = targetData.Max(d => d.Value);   //グラフの高さを算出するために必要
                if (maxVal <= 0) maxVal = 1;

                double availableWidth = 300; // 有効描画幅
                double stepX = availableWidth / Math.Max(targetData.Count, 1); // 項目間のピッチ(Math.Maxメソッドを使って0で除算しないようにする)
                double graphHeight = 80; // グラフ自体の基本高さ

                for (int i = 0; i < targetData.Count; i++)
                {
                    double h = (targetData[i].Value / maxVal) * graphHeight; // 比率に基づいた高さ算出
                    double xPos = i * stepX;

                    if (type.Contains("棒"))
                    {
                        Rectangle rect = new Rectangle
                        {
                            Width = 14,
                            Height = h,
                            Fill = Brushes.SteelBlue,
                            ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0}" // ツールチップ表示
                        };
                        Canvas.SetLeft(rect, xPos);
                        Canvas.SetTop(rect, graphHeight - h); // Canvas上端からの座標
                        canvas.Children.Add(rect);
                    }
                    else if (type.Contains("折れ線"))
                    {
                        // 前の点と現在の点を繋ぐ線を描画
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
                        // 頂点ドットの描画
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

                    // X軸の項目名ラベル（斜め）を追加
                    AddSlantedLabel(canvas, targetData[i].Key, xPos, graphHeight + 5);
                }
            }
        }

        /// <summary>
        /// X軸のラベルを斜め45度に回転させて配置する補助メソッド
        /// </summary>
        private void AddSlantedLabel(Canvas c, string txt, double x, double top)
        {
            TextBlock tb = new TextBlock
            {
                Text = txt,
                FontSize = 9,
                Width = 70,
                TextTrimming = TextTrimming.CharacterEllipsis // 長い文字列を省略表示(...)
            };
            tb.RenderTransformOrigin = new Point(0, 0); // 左上を支点に回転
            tb.LayoutTransform = new RotateTransform(45); // 45度傾ける

            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, top);
            c.Children.Add(tb);
        }

        /// <summary>
        /// 円グラフの描画処理
        /// </summary>
        private void DrawPieChart(Canvas canvas, Dictionary<string, double> data)
        {
            double totalAll = data.Values.Sum(); // 総計の算出（比率用）
            if (totalAll <= 0) totalAll = 1;

            // 上位8件のみを個別表示し、残りは「その他」として集約
            var targetData = data.OrderByDescending(d => d.Value).Take(10).ToList();
            double currentAngle = 0;
            double centerX = 150, centerY = 70, radius = 60;
            Brush[] colors = { Brushes.DodgerBlue, Brushes.Tomato, Brushes.Orange, Brushes.MediumSeaGreen, Brushes.MediumSlateBlue, Brushes.Gold, Brushes.HotPink, Brushes.Orchid };

            for (int i = 0; i < targetData.Count; i++)
            {
                double sweepAngle = (targetData[i].Value / totalAll) * 360; // 扇形の角度算出
                if (sweepAngle <= 0) continue;
                if (sweepAngle >= 360) sweepAngle = 359.9; // 360度ちょうどの描画エラー回避

                Path slice = CreatePieSlice(centerX, centerY, radius, currentAngle, sweepAngle, colors[i % colors.Length]);
                slice.ToolTip = $"{targetData[i].Key}: {targetData[i].Value:#,0} ({(targetData[i].Value / totalAll):P1})";
                canvas.Children.Add(slice);
                currentAngle += sweepAngle;
            }

            // 「その他」項目の集計と描画
            double othersSum = totalAll - targetData.Sum(d => d.Value);
            if (othersSum > 0.1)
            {
                double othersAngle = 360 - currentAngle;
                Path othersSlice = CreatePieSlice(centerX, centerY, radius, currentAngle, othersAngle, Brushes.LightGray);
                othersSlice.ToolTip = $"その他: {othersSum:#,0} ({(othersSum / totalAll):P1})";
                canvas.Children.Add(othersSlice);
            }
        }

        /// <summary>
        /// 円グラフの1ピース（扇形）をGeometryで生成する補助メソッド
        /// </summary>
        private Path CreatePieSlice(double cx, double cy, double r, double start, double sweep, Brush fill)
        {
            // 開始角・終了角をラジアンに変換
            double sRad = Math.PI * (start - 90) / 180.0;
            double eRad = Math.PI * (start + sweep - 90) / 180.0;
            Point pStart = new Point(cx + r * Math.Cos(sRad), cy + r * Math.Sin(sRad));
            Point pEnd = new Point(cx + r * Math.Cos(eRad), cy + r * Math.Sin(eRad));

            var figure = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
            figure.Segments.Add(new LineSegment(pStart, true));
            figure.Segments.Add(new ArcSegment(pEnd, new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));

            return new Path { Fill = fill, Stroke = Brushes.White, StrokeThickness = 1, Data = new PathGeometry(new[] { figure }) };
        }

        // RibbonPanelのチェックボックス連動用（現状は空実装）
        public void SetGraphVisibility(int n, bool v) { }

        /// <summary>
        /// WPFのビジュアルツリーを探索して、指定した型のコントロール（DataGrid等）を見つけるヘルパー
        /// </summary>
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

        /// <summary>
        /// スクロールビューワー内でマウスホイールを使用した際、横スクロールではなく縦スクロールさせる処理
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = sender as ScrollViewer;
            if (scv != null) { scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta); e.Handled = true; }
        }
    }
}