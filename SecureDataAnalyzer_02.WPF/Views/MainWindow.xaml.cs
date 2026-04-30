using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    /// <summary>
    /// メイン画面のルートクラス。レイアウト全体の統括を行う。
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// リボンパネルの展開・折りたたみボタンクリック時の処理
        /// </summary>
        private void ToggleRibbon_Click(object sender, RoutedEventArgs e)
        {
            // リボンの表示状態を反転
            if (MyRibbon.Visibility == Visibility.Visible)
            {
                // 折りたたみ
                MyRibbon.Visibility = Visibility.Collapsed;
                ToggleRibbonBtn.Content = "▼";
                // ボタンをウィンドウ上端に配置し直す（見た目の調整）
                ToggleRibbonBtn.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                // 展開
                MyRibbon.Visibility = Visibility.Visible;
                ToggleRibbonBtn.Content = "▲";
                // ボタンをリボン下端の位置に戻す
                ToggleRibbonBtn.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }
    }
}