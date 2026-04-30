using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// CSVデータのプレビュー表示（DataGrid）を管理するコントロール
    /// </summary>
    public partial class DataPreview : UserControl
    {
        public DataPreview()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 外部から受け取ったDataTableを画面のDataGridにバインドします
        /// </summary>
        /// <param name="dt">表示対象のデータテーブル</param>
        public void DisplayData(DataTable dt)
        {
            // DataGridのデータソースに設定（AutoGenerateColumns="True"により自動描画）
            PreviewGrid.ItemsSource = dt.DefaultView;

            // データが空なら「CSVプレビュー」というガイド文字を表示、あれば隠す
            EmptyMessage.Visibility = (dt != null && dt.Rows.Count > 0)
                                      ? Visibility.Collapsed
                                      : Visibility.Visible;
        }
    }
}