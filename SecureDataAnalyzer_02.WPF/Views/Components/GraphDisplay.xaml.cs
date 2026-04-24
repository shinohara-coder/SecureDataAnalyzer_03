using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    public partial class GraphDisplay : UserControl
    {
        public GraphDisplay()
        {
            InitializeComponent();
        }

        private void HideGraph_Click(object sender, RoutedEventArgs e)
        {
            // ボタンの「Tag」に書いてある名前を頼りに、どのグラフを隠すか判断します
            var btn = sender as Button;
            string targetName = btn.Tag.ToString();

            if (targetName == "Graph2") Graph2.Visibility = Visibility.Collapsed;
            if (targetName == "Graph3") Graph3.Visibility = Visibility.Collapsed;
            if (targetName == "Graph4") Graph4.Visibility = Visibility.Collapsed;
            if (targetName == "Graph5") Graph5.Visibility = Visibility.Collapsed;
            if (targetName == "Graph6") Graph6.Visibility = Visibility.Collapsed;
        }

        // 外側（リボンなど）から表示状態を変えるためのメソッド
        public void SetGraphVisibility(int number, bool isVisible)
        {
            var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            switch (number)
            {
                case 2: Graph2.Visibility = visibility; break;
                case 3: Graph3.Visibility = visibility; break;
                case 4: Graph4.Visibility = visibility; break;
                case 5: Graph5.Visibility = visibility; break;
                case 6: Graph6.Visibility = visibility; break;
            }
        }
    }
}