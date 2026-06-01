using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SecureDataAnalyzer_02.WPF.Models;
using SecureDataAnalyzer_02.WPF.Services;

namespace SecureDataAnalyzer_02.WPF.Views.Components
{
    /// <summary>
    /// CSVデータのプレビュー表示と得意先インクリメンタルサーチを統括するコントロール
    /// </summary>
    public partial class DataPreview : UserControl
    {
        // ─────────────────────────────────────────────────
        // フィールド
        // ─────────────────────────────────────────────────

        private readonly DatabaseService _db = new DatabaseService();

        /// <summary>デバウンス用キャンセルトークン</summary>
        private CancellationTokenSource? _searchCts;

        /// <summary>デバウンス遅延（ミリ秒）</summary>
        private const int DebounceMs = 300;

        // ─────────────────────────────────────────────────
        // 初期化
        // ─────────────────────────────────────────────────

        public DataPreview()
        {
            InitializeComponent();
            _db.Initialize();
        }

        // ─────────────────────────────────────────────────
        // 外部公開 API
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 外部（RibbonPanel）から受け取った DataTable を DataGrid に表示する。
        /// </summary>
        public void DisplayData(DataTable dt)
        {
            PreviewGrid.ItemsSource = dt.DefaultView;
            EmptyMessage.Visibility = (dt != null && dt.Rows.Count > 0)
                                      ? Visibility.Collapsed
                                      : Visibility.Visible;
        }

        /// <summary>
        /// 外部から DatabaseService を取得する（RibbonPanel の DB インポートで使用）。
        /// </summary>
        public DatabaseService DbService => _db;

        // ─────────────────────────────────────────────────
        // 検索バー イベント
        // ─────────────────────────────────────────────────

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
                SearchPlaceholder.Visibility = Visibility.Visible;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;

            // デバウンス：前の検索リクエストをキャンセルして再発行
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var query = SearchBox.Text;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(DebounceMs, token);
                    if (token.IsCancellationRequested) return;
                    await ExecuteSearchAsync(query, token);
                }
                catch (OperationCanceledException) { }
            }, token);
        }

        /// <summary>
        /// キーボード操作：↓で候補リストにフォーカス移動、Esc でポップアップを閉じる
        /// </summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && SuggestPopup.IsOpen && SuggestList.Items.Count > 0)
            {
                SuggestList.Focus();
                SuggestList.SelectedIndex = 0;
                var item = SuggestList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                item?.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CloseSuggestPopup();
            }
        }

        /// <summary>
        /// 「全件表示」ボタン：DB から先頭 15 件を取得して候補を表示する
        /// </summary>
        private async void ShowAllBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            SearchPlaceholder.Visibility = Visibility.Visible;
            var results = await _db.GetAllAsync(15);
            UpdateSuggestList(results);
        }

        // ─────────────────────────────────────────────────
        // 候補リスト イベント
        // ─────────────────────────────────────────────────

        private void SuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // マウス移動だけでは確定しない。Enter またはダブルクリックで確定。
        }

        private void SuggestList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        private void SuggestList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CloseSuggestPopup();
                SearchBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Up && SuggestList.SelectedIndex == 0)
            {
                // リスト先頭から SearchBox に戻る
                SearchBox.Focus();
                e.Handled = true;
            }
        }

        private void CloseDetailBtn_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────
        // 内部ロジック
        // ─────────────────────────────────────────────────

        private async Task ExecuteSearchAsync(string query, CancellationToken token)
        {
            System.Collections.Generic.IEnumerable<CustomerSearchResult> results;

            if (string.IsNullOrWhiteSpace(query))
            {
                results = Array.Empty<CustomerSearchResult>();
            }
            else
            {
                results = await _db.SearchAsync(query);
            }

            if (token.IsCancellationRequested) return;

            Dispatcher.Invoke(() => UpdateSuggestList(results));
        }

        private void UpdateSuggestList(System.Collections.Generic.IEnumerable<CustomerSearchResult> results)
        {
            SuggestList.ItemsSource = results;

            var hasItems = SuggestList.Items.Count > 0;
            SuggestPopup.IsOpen = hasItems;
        }

        private void CloseSuggestPopup()
        {
            SuggestPopup.IsOpen = false;
        }

        private void ConfirmSelection()
        {
            if (SuggestList.SelectedItem is CustomerSearchResult selected)
            {
                CloseSuggestPopup();
                LoadDetailAsync(selected.Code);
            }
        }

        private async void LoadDetailAsync(string code)
        {
            var customer = await _db.GetByCodeAsync(code);
            if (customer == null) return;

            Dispatcher.Invoke(() => ShowDetail(customer));
        }

        private void ShowDetail(TokisakiMaster c)
        {
            DetailCode.Text      = c.得意先コード;
            DetailName1.Text     = c.得意先名１;
            DetailRyakusho.Text  = c.得意先略称;
            DetailKana.Text      = c.取引先名ひらがな;
            DetailPostal.Text    = c.郵便番号;
            DetailTel.Text       = c.電話番号;
            DetailFax.Text       = c.FAX番号;

            // 住所を結合して表示
            var addr = string.Join(" ", new[] { c.住所１, c.住所２, c.住所３ }
                                            .Where(s => !string.IsNullOrWhiteSpace(s)));
            DetailAddress.Text = addr;

            DetailPanel.Visibility = Visibility.Visible;
        }
    }
}
