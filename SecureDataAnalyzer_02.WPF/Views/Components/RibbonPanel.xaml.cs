using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class RibbonPanel : UserControl
    {
        public RibbonPanel()
        {
            InitializeComponent();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            int graphNum = int.Parse(chk.Tag.ToString());
            bool isVisible = chk.IsChecked ?? true;

            // MainWindowを通じてGraphDisplayを操作する
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // GraphDisplayの中身を直接操作します
                // MainWindow.xamlでGraphDisplayに x:Name="MyGraphContent" と付ける必要があります（後述）
                mainWindow.MyGraphContent.SetGraphVisibility(graphNum, isVisible);
            }
        }

        // 以前のメソッド
        public void SetShowButtonVisibility(Visibility vis) { /* 以前と同じ */ }
        private void ShowGraphBtn_Click(object sender, RoutedEventArgs e) { /* 以前と同じ */ }
    }
}