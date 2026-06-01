using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    // ══════════════════════════════════════════════════════════════
    // ページング用センチネルアイテム
    // ══════════════════════════════════════════════════════════════

    /// <summary>候補リストの末尾に置く「次の N 件を表示」ボタン用モデル</summary>
    public class LoadMoreItem
    {
        public int RemainingCount { get; set; }
        public string Label => RemainingCount > 0
            ? $"▼  次の50件を表示  （残り {RemainingCount:N0} 件）"
            : "▼  次の50件を表示";
    }

    // ══════════════════════════════════════════════════════════════
    // DataTemplate セレクター
    // ══════════════════════════════════════════════════════════════

    /// <summary>通常の候補行と「次の N 件を表示」ボタン行を切り替えるセレクター</summary>
    public class SuggestItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? CustomerTemplate { get; set; }
        public DataTemplate? LoadMoreTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
            => item is LoadMoreItem ? LoadMoreTemplate : CustomerTemplate;
    }

    // ══════════════════════════════════════════════════════════════
    // DataPreview UserControl
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// CSV データのプレビュー、得意先インクリメンタルサーチ、詳細レコード表示、
    /// 全件表示のページネーション機能を統括するコントロール
    /// </summary>
    public partial class DataPreview : UserControl
    {
        // ─────────────────────────────────────────────────
        // フィールド
        // ─────────────────────────────────────────────────

        private readonly DatabaseService _db = new DatabaseService();

        /// <summary>デバウンス用キャンセルトークン</summary>
        private CancellationTokenSource? _searchCts;

        /// <summary>デバウンス遅延（ms）</summary>
        private const int DebounceMs = 250;

        // ── ページネーション状態 ──
        /// <summary>候補リストのデータソース（検索結果・全件表示両用）</summary>
        private readonly ObservableCollection<object> _suggestItems = new();

        /// <summary>全件表示モードで次にロードする開始位置</summary>
        private int _allDisplayOffset = 0;

        /// <summary>現在全件表示モードかどうか（将来の検索 vs 全件切り替え判定用）</summary>
        private bool _isShowingAll;

        private const int InitialPageSize = 15;
        private const int LoadMorePageSize = 50;

        // ─────────────────────────────────────────────────
        // 初期化
        // ─────────────────────────────────────────────────

        public DataPreview()
        {
            InitializeComponent();
            _db.Initialize();

            // ListBox のデータソースを固定（以後 _suggestItems を操作するだけ）
            SuggestList.ItemsSource = _suggestItems;
        }

        // ─────────────────────────────────────────────────
        // 外部公開 API
        // ─────────────────────────────────────────────────

        /// <summary>外部（RibbonPanel）から受け取った DataTable を DataGrid に表示する。</summary>
        public void DisplayData(DataTable dt)
        {
            PreviewGrid.ItemsSource = dt.DefaultView;
            EmptyMessage.Visibility = (dt != null && dt.Rows.Count > 0)
                                      ? Visibility.Collapsed
                                      : Visibility.Visible;
        }

        /// <summary>外部から DatabaseService を取得する（RibbonPanel の DB インポートで使用）。</summary>
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

            _isShowingAll = false;  // 文字入力したら全件表示モード解除

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

        /// <summary>↓ で候補リストへ移動、Esc でポップアップを閉じる</summary>
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && SuggestPopup.IsOpen && SuggestList.Items.Count > 0)
            {
                SuggestList.Focus();
                SuggestList.SelectedIndex = 0;
                (SuggestList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CloseSuggestPopup();
            }
        }

        // ─────────────────────────────────────────────────
        // 全件表示 ボタン
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 「全件表示」ボタン：最初の 15 件を表示し、残りがあれば
        /// 末尾に「次の50件を表示」ボタンを追加する。
        /// </summary>
        private async void ShowAllBtn_Click(object sender, RoutedEventArgs e)
        {
            // 検索ボックスをリセット（再検索デバウンスが走らないようイベントを外す）
            SearchBox.TextChanged -= SearchBox_TextChanged;
            SearchBox.Text = "";
            SearchPlaceholder.Visibility = Visibility.Visible;
            SearchBox.TextChanged += SearchBox_TextChanged;

            _isShowingAll = true;
            _allDisplayOffset = 0;
            _suggestItems.Clear();

            await AppendNextPageAsync(InitialPageSize);
        }

        // ─────────────────────────────────────────────────
        // 候補リスト イベント
        // ─────────────────────────────────────────────────

        /// <summary>
        /// シングルクリックで確定 or「次の50件を表示」ボタン押下を処理する。
        /// </summary>
        private void SuggestList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var container = ItemsControl.ContainerFromElement(
                SuggestList,
                e.OriginalSource as DependencyObject) as ListBoxItem;

            if (container == null) return;

            if (container.DataContext is CustomerSearchResult result)
            {
                SuggestList.SelectedItem = result;
                ConfirmSelection();
                e.Handled = true;
            }
            else if (container.DataContext is LoadMoreItem)
            {
                // 「次の50件を表示」ボタンをクリック
                Task.Run(() => Dispatcher.Invoke(() => _ = AppendNextPageAsync(LoadMorePageSize)));
                e.Handled = true;
            }
        }

        private void SuggestList_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (SuggestList.SelectedItem is LoadMoreItem)
                        Task.Run(() => Dispatcher.Invoke(() => _ = AppendNextPageAsync(LoadMorePageSize)));
                    else
                        ConfirmSelection();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    CloseSuggestPopup();
                    SearchBox.Focus();
                    e.Handled = true;
                    break;

                case Key.Up when SuggestList.SelectedIndex == 0:
                    SearchBox.Focus();
                    e.Handled = true;
                    break;
            }
        }

        private void CloseDetailBtn_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────────
        // 内部ロジック：検索
        // ─────────────────────────────────────────────────

        private async Task ExecuteSearchAsync(string query, CancellationToken token)
        {
            IEnumerable<CustomerSearchResult> results;

            if (string.IsNullOrWhiteSpace(query))
                results = Array.Empty<CustomerSearchResult>();
            else
                results = await _db.SearchAsync(query);

            if (token.IsCancellationRequested) return;

            Dispatcher.Invoke(() =>
            {
                _suggestItems.Clear();
                foreach (var r in results) _suggestItems.Add(r);
                SuggestPopup.IsOpen = _suggestItems.Count > 0;
            });
        }

        private void UpdateSuggestList(IEnumerable<CustomerSearchResult> results)
        {
            _suggestItems.Clear();
            foreach (var r in results) _suggestItems.Add(r);
            SuggestPopup.IsOpen = _suggestItems.Count > 0;
        }

        private void CloseSuggestPopup()
        {
            SuggestPopup.IsOpen = false;
        }

        // ─────────────────────────────────────────────────
        // 内部ロジック：ページネーション
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 次のページを <paramref name="pageSize"/> 件分取得してリストへ追加する。
        /// 既存の LoadMoreItem センチネルは差し替える。
        /// </summary>
        private async Task AppendNextPageAsync(int pageSize)
        {
            var (items, total) = await _db.GetPagedAsync(_allDisplayOffset, pageSize);
            var list = items.ToList();

            // 既存の LoadMoreItem を削除
            var existing = _suggestItems.OfType<LoadMoreItem>().FirstOrDefault();
            if (existing != null) _suggestItems.Remove(existing);

            foreach (var item in list)
                _suggestItems.Add(item);

            _allDisplayOffset += list.Count;

            // 残りがあれば末尾にセンチネルを追加
            int remaining = total - _allDisplayOffset;
            if (remaining > 0)
                _suggestItems.Add(new LoadMoreItem { RemainingCount = remaining });

            SuggestPopup.IsOpen = _suggestItems.Count > 0;

            // 新しく追加されたアイテムの先頭が見えるようスクロール
            if (list.Count > 0)
            {
                SuggestList.ScrollIntoView(list[0]);
            }
        }

        // ─────────────────────────────────────────────────
        // 内部ロジック：確定・詳細表示
        // ─────────────────────────────────────────────────

        private void ConfirmSelection()
        {
            if (SuggestList.SelectedItem is CustomerSearchResult selected)
            {
                var code = selected.Code;

                CloseSuggestPopup();

                // 検索窓に選択した企業名を表示し続ける
                SearchBox.TextChanged -= SearchBox_TextChanged;
                SearchBox.Text = selected.Name1;
                SearchPlaceholder.Visibility = Visibility.Collapsed;
                SearchBox.TextChanged += SearchBox_TextChanged;
                SearchBox.CaretIndex = SearchBox.Text.Length;

                Task.Run(() => LoadAndShowDetailAsync(code));
            }
        }

        private async Task LoadAndShowDetailAsync(string code)
        {
            try
            {
                var customer = await _db.GetByCodeAsync(code);
                if (customer == null)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"得意先コード [{code}] のデータが見つかりませんでした。",
                            "データなし", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }
                Dispatcher.Invoke(() => BindDetailFields(customer));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"データ取得エラー: {ex.Message}",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        // ─────────────────────────────────────────────────
        // 詳細レコード表示
        // ─────────────────────────────────────────────────

        private void BindDetailFields(TokisakiMaster c)
        {
            DetailCode.Text  = c.得意先コード;
            DetailName1.Text = c.得意先名１;

            DetailRyakusho.Text = c.得意先略称;
            DetailKana.Text     = c.取引先名ひらがな;
            DetailName2.Text    = c.得意先名２;

            DetailPostal.Text  = FormatPostal(c.郵便番号);
            DetailTel.Text     = c.電話番号;
            DetailFax.Text     = c.FAX番号;
            DetailAddr1.Text   = c.住所１;
            DetailAddr2.Text   = c.住所２;
            DetailAddr3.Text   = c.住所３;

            DetailNohinsakiCode.Text  = c.納品先コード;
            DetailTantoshaCode.Text   = c.担当者コード;
            DetailSinyoLimit.Text     = string.IsNullOrWhiteSpace(c.与信限度額) ? "―" : c.与信限度額;
            DetailWeightDisplay.Text  = c.重量表示;

            DetailTankaKubun1.Text = c.単価処理区分;
            DetailTankaUnit1.Text  = c.単価処理単位;
            DetailTankaKubun2.Text = c.単価処理区分_1kg未満;
            DetailTankaUnit2.Text  = c.単価処理単位_1kg未満;
            DetailKingakuKubun.Text = c.金額処理区分;
            DetailTankaKubun3.Text = c.単価処理区分_切板以外;
            DetailTankaUnit3.Text  = c.単価処理単位_切板以外;

            DetailShouhizeiKubun.Text   = c.消費税計算処理区分;
            DetailShouhizeiUnit.Text    = c.消費税計算処理単位;
            DetailShouhizeiBunkai.Text  = c.消費税分解処理区分;

            DetailPwDate.Text = string.IsNullOrWhiteSpace(c.新パスワード適用年月日) ? "―" : c.新パスワード適用年月日;
            DetailPwTime.Text = string.IsNullOrWhiteSpace(c.新パスワード適用時刻)   ? "―" : c.新パスワード適用時刻;

            DetailPanel.Visibility = Visibility.Visible;
        }

        /// <summary>郵便番号を 〒XXX-XXXX 形式に整形する（7桁の場合のみ）</summary>
        private static string FormatPostal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 7)
                return $"〒{digits[..3]}-{digits[3..]}";
            return raw;
        }
    }
}
