namespace SecureDataAnalyzer_02.WPF.Models
{
    /// <summary>
    /// 得意先マスター 1件分のデータを保持するモデル
    /// </summary>
    public class TokisakiMaster
    {
        public string 得意先コード { get; set; } = "";
        public string 取引先名ひらがな { get; set; } = "";
        public string 得意先名１ { get; set; } = "";
        public string 得意先名２ { get; set; } = "";
        public string 得意先略称 { get; set; } = "";
        public string 郵便番号 { get; set; } = "";
        public string 住所１ { get; set; } = "";
        public string 住所２ { get; set; } = "";
        public string 住所３ { get; set; } = "";
        public string 電話番号 { get; set; } = "";
        public string FAX番号 { get; set; } = "";
        public string 納品先コード { get; set; } = "";
        public string 担当者コード { get; set; } = "";
        public string 与信限度額 { get; set; } = "";
        public string 単価処理区分 { get; set; } = "";
        public string 単価処理単位 { get; set; } = "";
        public string 単価処理区分_1kg未満 { get; set; } = "";
        public string 単価処理単位_1kg未満 { get; set; } = "";
        public string 金額処理区分 { get; set; } = "";
        public string 単価処理区分_切板以外 { get; set; } = "";
        public string 単価処理単位_切板以外 { get; set; } = "";
        public string 消費税計算処理区分 { get; set; } = "";
        public string 消費税計算処理単位 { get; set; } = "";
        public string 消費税分解処理区分 { get; set; } = "";
        public string 重量表示 { get; set; } = "";
        public string 新パスワード適用年月日 { get; set; } = "";
        public string 新パスワード適用時刻 { get; set; } = "";

        /// <summary>
        /// 検索高速化のための正規化済みキー（カタカナ→ひらがな変換済み）
        /// </summary>
        public string SearchKey { get; set; } = "";
    }

    /// <summary>
    /// 検索候補ポップアップ表示用の軽量モデル
    /// </summary>
    public class CustomerSearchResult
    {
        public string Code { get; set; } = "";
        public string Name1 { get; set; } = "";
        public string KanaName { get; set; } = "";

        public override string ToString() => $"{Code}  {Name1}  {KanaName}";
    }
}
