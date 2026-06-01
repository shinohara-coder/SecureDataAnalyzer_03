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
    /// CSV データのプレビューと得意先インクリメンタルサーチ・詳細レコード表示を統括するコントロール
    /// </summary>
    public partial class DataPreview : UserControl
    {
        // ─────────────────────────────────────────────────
        // フィールド
        // ─────────────────────────────────────────────────

        private readonly DatabaseService _db = new DatabaseService();

        /// <summary>デバウンス用キャンセルトークン</summary>
        private CancellationTokenSource? _searchCts;

        /// <summary>デバウンス遅延（ms）：要件は 300ms 以内</summary>
        private const int DebounceMs = 250;

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

            // デバウンス：前のリクエストをキャンセルして再発行
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

        /// <summary>「全件表示」ボタン：DB から先頭 15 件を取得して候補を表示する</summary>
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

        /// <summary>
        /// アイテム上のシングルクリックで確定する。
        /// PreviewMouseLeftButtonDown は Popup 内でも確実に発火する。
        /// </summary>
        private void SuggestList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // クリック位置に対応する ListBoxItem を特定する
            var item = ItemsControl.ContainerFromElement(
                SuggestList,
                e.OriginalSource as DependencyObject) as ListBoxItem;

            if (item?.DataContext is CustomerSearchResult result)
            {
                SuggestList.SelectedItem = result;
                ConfirmSelection();
                e.Handled = true;   // 二重発火を防ぐ
            }
        }

        private void SuggestList_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
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
        // 内部ロジック
        // ─────────────────────────────────────────────────

        private async Task ExecuteSearchAsync(string query, CancellationToken token)
        {
            IEnumerable<CustomerSearchResult> results;

            if (string.IsNullOrWhiteSpace(query))
                results = Array.Empty<CustomerSearchResult>();
            else
                results = await _db.SearchAsync(query);

            if (token.IsCancellationRequested) return;

            Dispatcher.Invoke(() => UpdateSuggestList(results));
        }

        private void UpdateSuggestList(IEnumerable<CustomerSearchResult> results)
        {
            SuggestList.ItemsSource = results;
            SuggestPopup.IsOpen = SuggestList.Items.Count > 0;
        }

        private void CloseSuggestPopup()
        {
            SuggestPopup.IsOpen = false;
        }

        private void ConfirmSelection()
        {
            if (SuggestList.SelectedItem is CustomerSearchResult selected)
            {
                var code = selected.Code;   // クロージャで保持
                CloseSuggestPopup();

                // UI スレッドをブロックしない非同期呼び出し
                Task.Run(() => LoadAndShowDetailAsync(code));
            }
        }

        // ─────────────────────────────────────────────────
        // 詳細レコード表示
        // ─────────────────────────────────────────────────

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

        /// <summary>
        /// 取得した TokisakiMaster の全フィールドを詳細パネルの各 TextBlock に反映する。
        /// </summary>
        private void BindDetailFields(TokisakiMaster c)
        {
            // ── ヘッダー ──
            DetailCode.Text  = c.得意先コード;
            DetailName1.Text = c.得意先名１;

            // ── 基本情報 ──
            DetailRyakusho.Text = c.得意先略称;
            DetailKana.Text     = c.取引先名ひらがな;
            DetailName2.Text    = c.得意先名２;

            // ── 住所・連絡先 ──
            DetailPostal.Text  = FormatPostal(c.郵便番号);
            DetailTel.Text     = c.電話番号;
            DetailFax.Text     = c.FAX番号;
            DetailAddr1.Text   = c.住所１;
            DetailAddr2.Text   = c.住所２;
            DetailAddr3.Text   = c.住所３;

            // ── 管理情報 ──
            DetailNohinsakiCode.Text  = c.納品先コード;
            DetailTantoshaCode.Text   = c.担当者コード;
            DetailSinyoLimit.Text     = string.IsNullOrWhiteSpace(c.与信限度額) ? "―" : c.与信限度額;
            DetailWeightDisplay.Text  = c.重量表示;

            // ── 単価・金額処理区分 ──
            DetailTankaKubun1.Text = c.単価処理区分;
            DetailTankaUnit1.Text  = c.単価処理単位;
            DetailTankaKubun2.Text = c.単価処理区分_1kg未満;
            DetailTankaUnit2.Text  = c.単価処理単位_1kg未満;
            DetailKingakuKubun.Text = c.金額処理区分;
            DetailTankaKubun3.Text = c.単価処理区分_切板以外;
            DetailTankaUnit3.Text  = c.単価処理単位_切板以外;

            // ── 消費税処理 ──
            DetailShouhizeiKubun.Text   = c.消費税計算処理区分;
            DetailShouhizeiUnit.Text    = c.消費税計算処理単位;
            DetailShouhizeiBunkai.Text  = c.消費税分解処理区分;

            // ── パスワード管理 ──
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
