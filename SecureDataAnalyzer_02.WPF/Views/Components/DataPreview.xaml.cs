using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
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
            // DataGridのデータソースに設定（これだけで自動的に表が生成される）
            PreviewGrid.ItemsSource = dt.DefaultView;

            // データが1行以上あれば、中央の「CSVプレビュー」というガイド文字を非表示にする
            EmptyMessage.Visibility = (dt != null && dt.Rows.Count > 0)
                                      ? Visibility.Collapsed
                                      : Visibility.Visible;
        }
    }
}