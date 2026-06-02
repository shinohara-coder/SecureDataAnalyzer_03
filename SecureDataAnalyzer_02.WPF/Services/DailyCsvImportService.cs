using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace SecureDataAnalyzer_02.WPF.Services
{
    /// <summary>
    /// デイリー売上 CSV を読み込み、スキーマ検証後に SQLite の daily_data テーブルへ蓄積するサービス。
    /// 初回読み込み時はヘッダーからテーブルを自動生成し、2回目以降は列構成が一致するCSVのみ追記を許可する。
    /// 「見積番号」列が存在する場合はそれを PRIMARY KEY とし、重複行は最新データで上書きする。
    /// </summary>
    public class DailyCsvImportService
    {
        private readonly DatabaseService _db;

        /// <summary>業務上のプライマリキーとして扱う列名。</summary>
        private const string PrimaryKeyColumn = "見積番号";

        public DailyCsvImportService(DatabaseService db) => _db = db;

        // ─────────────────────────────────────────────────
        // 公開 API
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 指定 CSV を検証し、daily_data テーブルへ蓄積する。
        /// 「見積番号」が PRIMARY KEY でないテーブルが存在する場合は自動再構築する。
        /// </summary>
        /// <returns>(追記・更新件数, テーブル総件数, 初回インポートかどうか)</returns>
        /// <exception cref="InvalidOperationException">
        /// ファイルが空、またはヘッダーが既存テーブルと一致しない場合。
        /// </exception>
        public async Task<(int Added, int Total, bool IsFirst)> ImportAsync(string csvPath)
        {
            var (headers, rows) = ParseCsv(csvPath);

            bool isFirst = !_db.HasDailyTable();

            if (isFirst)
            {
                // 初回：見積番号がある場合は PRIMARY KEY として生成
                string? pkCol = headers.Contains(PrimaryKeyColumn, StringComparer.Ordinal)
                    ? PrimaryKeyColumn : null;
                await _db.CreateDailyTableAsync(headers, pkCol);
            }
            else
            {
                // ── 既存テーブルが見積番号を PK としていない場合は自動再構築 ──
                var currentPk = await _db.GetDailyPrimaryKeyColumnAsync();
                if (!string.Equals(currentPk, PrimaryKeyColumn, StringComparison.Ordinal))
                {
                    var existingCols = await _db.GetDailyTableColumnsAsync();
                    if (existingCols.Contains(PrimaryKeyColumn, StringComparer.Ordinal))
                        await _db.RebuildDailyTableAsync(PrimaryKeyColumn);
                }

                // スキーマ検証
                var existingColumns = await _db.GetDailyTableColumnsAsync();
                ValidateHeaders(headers, existingColumns);
            }

            var (added, total) = await _db.AppendDailyDataAsync(headers, rows);
            return (added, total, isFirst);
        }

        // ─────────────────────────────────────────────────
        // スキーマ検証
        // ─────────────────────────────────────────────────

        /// <summary>
        /// CSVのヘッダーが既存テーブルの列構成と完全一致するか検証する。
        /// 不一致の場合は <see cref="InvalidOperationException"/> をスローする。
        /// </summary>
        private static void ValidateHeaders(List<string> actual, List<string> expected)
        {
            if (actual.Count != expected.Count)
            {
                throw new InvalidOperationException(
                    $"列数が既存テーブルと一致しません。\n" +
                    $"既存テーブル: {expected.Count} 列　このCSV: {actual.Count} 列\n\n" +
                    "最初に読み込んだCSVと同じ列構成のファイルのみ追記できます。");
            }

            var mismatches = new List<string>();
            for (int i = 0; i < expected.Count; i++)
            {
                if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
                    mismatches.Add(
                        $"  列 {i + 1,2}: 既存 「{expected[i]}」  →  このCSV 「{actual[i]}」");
            }

            if (mismatches.Count > 0)
            {
                throw new InvalidOperationException(
                    "CSVのヘッダーが既存テーブルと一致しません。\n\n" +
                    "不一致の列:\n" + string.Join("\n", mismatches) + "\n\n" +
                    "最初に読み込んだCSVと同じ列構成のファイルのみ追記できます。");
            }
        }

        // ─────────────────────────────────────────────────
        // CSV パース
        // ─────────────────────────────────────────────────

        /// <summary>
        /// CSVを読み込んでヘッダーとデータ行に分解する。
        /// </summary>
        private (List<string> Headers, List<string[]> Rows) ParseCsv(string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc = DetectEncoding(path);

            using var parser = new TextFieldParser(path, enc)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = false
            };
            parser.SetDelimiters(",");

            if (parser.EndOfData)
                throw new InvalidOperationException("CSVファイルが空です。");

            var headerFields = parser.ReadFields()
                ?? throw new InvalidOperationException("ヘッダー行を読み込めませんでした。");

            if (headerFields.Length == 0)
                throw new InvalidOperationException("ヘッダー列が存在しません。");

            var headers = new List<string>(headerFields);

            // ── 重複列名チェック ───────────────────────────────────────────
            var duplicates = headers
                .GroupBy(h => h, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => $"「{g.Key}」")
                .ToList();
            if (duplicates.Count > 0)
                throw new InvalidOperationException(
                    $"CSVに重複した列名が含まれています。\n\n重複: {string.Join(", ", duplicates)}\n\n" +
                    "CSVのヘッダーを確認してから再度お試しください。");

            // ── システム予約名との衝突チェック ────────────────────────────
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id", "imported_at" };
            var conflicts = headers.Where(h => reserved.Contains(h)).Select(h => $"「{h}」").ToList();
            if (conflicts.Count > 0)
                throw new InvalidOperationException(
                    $"列名 {string.Join(", ", conflicts)} はシステムの内部列名と競合します。\n\n" +
                    "CSVの該当列名を変更してから再度お試しください。");

            var rows = new List<string[]>();

            while (!parser.EndOfData)
            {
                var f = parser.ReadFields();
                if (f != null) rows.Add(f);
            }

            return (headers, rows);
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

            try { return Encoding.GetEncoding("shift_jis"); }
            catch { return Encoding.UTF8; }
        }
    }
}
