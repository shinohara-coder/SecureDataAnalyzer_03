using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using SecureDataAnalyzer_02.WPF.Models;

namespace SecureDataAnalyzer_02.WPF.Services
{
    /// <summary>
    /// 得意先マスター CSV を読み込み SQLite へ格納するサービス。
    /// </summary>
    public class CsvImportService
    {
        private readonly DatabaseService _db;

        public CsvImportService(DatabaseService db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────────────
        // 期待するヘッダー定義（得意先マスター(かな有).csv の列順）
        // ─────────────────────────────────────────────────

        /// <summary>
        /// このシステムが受け付ける「得意先マスター(かな有)」CSV の正規ヘッダー。
        /// 列順・列名が完全に一致していないと読み込みを拒否する。
        /// </summary>
        private static readonly string[] RequiredHeaders =
        {
            "得意先コード",
            "取引先名ひらがな",
            "得意先名１",
            "得意先名２",
            "得意先略称",
            "郵便番号",
            "住所１",
            "住所２",
            "住所３",
            "電話番号",
            "ＦＡＸ番号",           // 全角
            "納品先コード",
            "担当者コード",
            "与信限度額",
            "単価処理区分",
            "単価処理単位",
            "単価処理区分(1kg未満)",
            "単価処理単位(1kg未満)",
            "金額処理区分",
            "単価処理区分(切板以外)",
            "単価処理単位(切板以外)",
            "消費税計算処理区分",
            "消費税計算処理単位",
            "消費税分解処理区分",
            "重量表示",
            "新パスワード適用年月日",
            "新パスワード適用時刻",
        };

        // ─────────────────────────────────────────────────
        // 公開 API
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 指定 CSV ファイルを検証してから DB にインポートする。
        /// ヘッダーが一致しない場合は <see cref="InvalidOperationException"/> をスローする。
        /// </summary>
        /// <returns>インポートした件数</returns>
        public async Task<int> ImportAsync(string csvPath)
        {
            var records = ParseCsv(csvPath);   // 内部で ValidateHeaders を呼ぶ
            await _db.ImportAllAsync(records);
            return records.Count;
        }

        // ─────────────────────────────────────────────────
        // ヘッダー検証
        // ─────────────────────────────────────────────────

        /// <summary>
        /// CSV の第 1 行を Required ヘッダーと照合し、不一致があれば例外をスローする。
        /// </summary>
        private static void ValidateHeaders(string[] actual)
        {
            // ── 列数チェック ──────────────────────────────
            if (actual.Length < RequiredHeaders.Length)
            {
                throw new InvalidOperationException(
                    $"列数が不足しています。\n" +
                    $"必要: {RequiredHeaders.Length} 列　実際: {actual.Length} 列\n\n" +
                    "このCSVは「得意先マスター(かな有)」の形式ではありません。");
            }

            // ── 列名照合 ──────────────────────────────────
            var mismatches = new List<string>();
            for (int i = 0; i < RequiredHeaders.Length; i++)
            {
                var got      = (i < actual.Length ? actual[i] : "").Trim();
                var expected = RequiredHeaders[i];

                if (!string.Equals(got, expected, StringComparison.Ordinal))
                    mismatches.Add($"  列 {i + 1,2}: 期待 「{expected}」  →  実際 「{got}」");
            }

            if (mismatches.Count > 0)
            {
                throw new InvalidOperationException(
                    "CSVのヘッダーが「得意先マスター」の形式と一致しません。\n\n" +
                    "不一致の列:\n" + string.Join("\n", mismatches) + "\n\n" +
                    "読み込み可能なファイルは「得意先マスター(かな有).csv」のみです。");
            }
        }

        // ─────────────────────────────────────────────────
        // CSV パース
        // ─────────────────────────────────────────────────

        private List<TokisakiMaster> ParseCsv(string path)
        {
            var list = new List<TokisakiMaster>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc = DetectEncoding(path);

            using var parser = new TextFieldParser(path, enc)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",");

            // ── ヘッダー行を読み込んで検証 ─────────────────
            if (parser.EndOfData)
                throw new InvalidOperationException("CSVファイルが空です。");

            var headerFields = parser.ReadFields()
                ?? throw new InvalidOperationException("ヘッダー行を読み込めませんでした。");

            ValidateHeaders(headerFields);   // 不一致なら例外

            // ── データ行を読み込む ─────────────────────────
            while (!parser.EndOfData)
            {
                var f = parser.ReadFields();
                if (f == null || f.Length < 11) continue;

                string GetField(int idx) => idx < f.Length ? (f[idx] ?? "").Trim() : "";

                var kana = GetField(1);
                var record = new TokisakiMaster
                {
                    得意先コード          = GetField(0),
                    取引先名ひらがな      = kana,
                    得意先名１            = GetField(2),
                    得意先名２            = GetField(3),
                    得意先略称            = GetField(4),
                    郵便番号              = GetField(5),
                    住所１                = GetField(6),
                    住所２                = GetField(7),
                    住所３                = GetField(8),
                    電話番号              = GetField(9),
                    FAX番号               = GetField(10),
                    納品先コード          = GetField(11),
                    担当者コード          = GetField(12),
                    与信限度額            = GetField(13),
                    単価処理区分          = GetField(14),
                    単価処理単位          = GetField(15),
                    単価処理区分_1kg未満  = GetField(16),
                    単価処理単位_1kg未満  = GetField(17),
                    金額処理区分          = GetField(18),
                    単価処理区分_切板以外 = GetField(19),
                    単価処理単位_切板以外 = GetField(20),
                    消費税計算処理区分    = GetField(21),
                    消費税計算処理単位    = GetField(22),
                    消費税分解処理区分    = GetField(23),
                    重量表示              = GetField(24),
                    新パスワード適用年月日 = GetField(25),
                    新パスワード適用時刻   = GetField(26),
                    SearchKey             = KanaHelper.Normalize(kana)
                };

                if (!string.IsNullOrEmpty(record.得意先コード))
                    list.Add(record);
            }

            return list;
        }

        // ─────────────────────────────────────────────────
        // エンコード自動判定
        // ─────────────────────────────────────────────────

        private static Encoding DetectEncoding(string path)
        {
            try
            {
                var bom = new byte[3];
                using var fs = File.OpenRead(path);
                int read = 0;
                while (read < 3)
                {
                    int n = fs.Read(bom, read, 3 - read);
                    if (n == 0) break;
                    read += n;
                }
                if (read == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            }
            catch { }

            // BOM なし → Shift-JIS（日本語 Windows 業務系 CSV の慣習）
            try { return Encoding.GetEncoding("shift_jis"); }
            catch { return Encoding.UTF8; }
        }
    }
}
