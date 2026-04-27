using System.Windows;

namespace SecureDataAnalyzer_02.WPF.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// リボン格納ボタンクリック時の処理
        /// </summary>
        private void ToggleRibbon_Click(object sender, RoutedEventArgs e)
        {
            // リボンの表示状態を反転
            if (MyRibbon.Visibility == Visibility.Visible)
            {
                MyRibbon.Visibility = Visibility.Collapsed;
                ToggleRibbonBtn.Content = "▼";
                // ボタンを上端に張り付かせる（リボンがない時の見た目調整）
                ToggleRibbonBtn.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                MyRibbon.Visibility = Visibility.Visible;
                ToggleRibbonBtn.Content = "▲";
                // ボタンをリボンの左下位置に戻す
                ToggleRibbonBtn.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }
    }
}