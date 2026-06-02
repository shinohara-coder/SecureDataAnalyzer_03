using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using SecureDataAnalyzer_02.WPF.Models;

namespace SecureDataAnalyzer_02.WPF.Services
{
    /// <summary>
    /// SQLiteデータベースへのアクセスをすべて担うサービス。
    /// Dapper を利用して高速なデータ取得を実現する。
    /// </summary>
    public class DatabaseService
    {
        private static readonly string _dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SecureDataAnalyzer");

        private static readonly string _dbPath = Path.Combine(_dbFolder, "tokisaki_master.db");

        private string ConnectionString => $"Data Source={_dbPath}";

        // ─────────────────────────────────────────────────
        // 初期化
        // ─────────────────────────────────────────────────

        /// <summary>
        /// データベースファイルと必要なテーブル・インデックスを作成する。
        /// アプリ起動時に呼ぶこと。
        /// </summary>
        public void Initialize()
        {
            Directory.CreateDirectory(_dbFolder);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS TokisakiMaster (
                    tokisaki_code       TEXT PRIMARY KEY,
                    kana_name           TEXT,
                    name1               TEXT,
                    name2               TEXT,
                    ryakusho            TEXT,
                    postal_code         TEXT,
                    address1            TEXT,
                    address2            TEXT,
                    address3            TEXT,
                    tel                 TEXT,
                    fax                 TEXT,
                    nohinsaki_code      TEXT,
                    tantosha_code       TEXT,
                    sinyo_limit         TEXT,
                    tanka_kubun1        TEXT,
                    tanka_unit1         TEXT,
                    tanka_kubun2        TEXT,
                    tanka_unit2         TEXT,
                    kingaku_kubun       TEXT,
                    tanka_kubun3        TEXT,
                    tanka_unit3         TEXT,
                    shouhizei_kubun     TEXT,
                    shouhizei_unit      TEXT,
                    shouhizei_bunkai    TEXT,
                    weight_display      TEXT,
                    pw_date             TEXT,
                    pw_time             TEXT,
                    search_key          TEXT
                )");

            // 検索高速化インデックス
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_search_key ON TokisakiMaster(search_key)");
            conn.Execute("CREATE INDEX IF NOT EXISTS idx_name1 ON TokisakiMaster(name1)");
        }

        // ─────────────────────────────────────────────────
        // インポート
        // ─────────────────────────────────────────────────

        /// <summary>
        /// 得意先マスターを一括登録する。既存データは全削除してから挿入（上書き）。
        /// </summary>
        public async Task ImportAllAsync(IEnumerable<TokisakiMaster> records)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM TokisakiMaster", transaction: tx);

                const string sql = @"
                    INSERT INTO TokisakiMaster (
                        tokisaki_code, kana_name, name1, name2, ryakusho,
                        postal_code, address1, address2, address3, tel, fax,
                        nohinsaki_code, tantosha_code, sinyo_limit,
                        tanka_kubun1, tanka_unit1, tanka_kubun2, tanka_unit2,
                        kingaku_kubun, tanka_kubun3, tanka_unit3,
                        shouhizei_kubun, shouhizei_unit, shouhizei_bunkai,
                        weight_display, pw_date, pw_time, search_key
                    ) VALUES (
                        @得意先コード, @取引先名ひらがな, @得意先名１, @得意先名２, @得意先略称,
                        @郵便番号, @住所１, @住所２, @住所３, @電話番号, @FAX番号,
                        @納品先コード, @担当者コード, @与信限度額,
                        @単価処理区分, @単価処理単位, @単価処理区分_1kg未満, @単価処理単位_1kg未満,
                        @金額処理区分, @単価処理区分_切板以外, @単価処理単位_切板以外,
                        @消費税計算処理区分, @消費税計算処理単位, @消費税分解処理区分,
                        @重量表示, @新パスワード適用年月日, @新パスワード適用時刻, @SearchKey
                    )";

                await conn.ExecuteAsync(sql, records, transaction: tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ─────────────────────────────────────────────────
        // 検索
        // ─────────────────────────────────────────────────

        /// <summary>
        /// インクリメンタルサーチ：得意先名１・ひらがな（正規化）・略称で部分一致検索。
        /// 最大 <paramref name="maxResults"/> 件を返す。
        /// </summary>
        public async Task<IEnumerable<CustomerSearchResult>> SearchAsync(string query, int maxResults = 15)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<CustomerSearchResult>();

            var norm = KanaHelper.Normalize(query);
            var like = "%" + norm + "%";
            var likeOrig = "%" + query + "%";

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            return await conn.QueryAsync<CustomerSearchResult>(@"
                SELECT tokisaki_code AS Code,
                       name1        AS Name1,
                       kana_name    AS KanaName
                FROM   TokisakiMaster
                WHERE  search_key LIKE @like
                    OR name1      LIKE @likeOrig
                    OR ryakusho   LIKE @likeOrig
                LIMIT  @max",
                new { like, likeOrig, max = maxResults });
        }

        /// <summary>
        /// 全件を候補リスト形式で取得する（全件表示ボタン：初回表示用）。
        /// </summary>
        public async Task<IEnumerable<CustomerSearchResult>> GetAllAsync(int maxResults = 15)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            return await conn.QueryAsync<CustomerSearchResult>(@"
                SELECT tokisaki_code AS Code,
                       name1        AS Name1,
                       kana_name    AS KanaName
                FROM   TokisakiMaster
                ORDER BY tokisaki_code
                LIMIT  @max",
                new { max = maxResults });
        }

        /// <summary>
        /// ページング取得：指定オフセットから limit 件と総件数を同時に返す。
        /// </summary>
        /// <param name="offset">取得開始位置（0始まり）</param>
        /// <param name="limit">取得件数</param>
        public async Task<(IEnumerable<CustomerSearchResult> Items, int TotalCount)> GetPagedAsync(
            int offset, int limit)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            var items = await conn.QueryAsync<CustomerSearchResult>(@"
                SELECT tokisaki_code AS Code,
                       name1        AS Name1,
                       kana_name    AS KanaName
                FROM   TokisakiMaster
                ORDER BY tokisaki_code
                LIMIT  @limit OFFSET @offset",
                new { limit, offset });

            var total = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM TokisakiMaster");

            return (items, total);
        }

        /// <summary>
        /// 得意先コードで 1件の詳細データを取得する。
        /// </summary>
        public async Task<TokisakiMaster?> GetByCodeAsync(string code)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            // ── SQLite の列名を英語のままとり、コード側で手動マッピングする ──
            // （Dapper が日本語プロパティ名への自動マップに失敗するケースを回避）
            var raw = await conn.QuerySingleOrDefaultAsync(@"
                SELECT tokisaki_code,
                       kana_name,
                       name1,
                       name2,
                       ryakusho,
                       postal_code,
                       address1,
                       address2,
                       address3,
                       tel,
                       fax,
                       nohinsaki_code,
                       tantosha_code,
                       sinyo_limit,
                       tanka_kubun1,
                       tanka_unit1,
                       tanka_kubun2,
                       tanka_unit2,
                       kingaku_kubun,
                       tanka_kubun3,
                       tanka_unit3,
                       shouhizei_kubun,
                       shouhizei_unit,
                       shouhizei_bunkai,
                       weight_display,
                       pw_date,
                       pw_time
                FROM   TokisakiMaster
                WHERE  tokisaki_code = @code",
                new { code }) as IDictionary<string, object>;

            if (raw == null) return null;

            string S(string key) => raw.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

            return new TokisakiMaster
            {
                得意先コード          = S("tokisaki_code"),
                取引先名ひらがな      = S("kana_name"),
                得意先名１            = S("name1"),
                得意先名２            = S("name2"),
                得意先略称            = S("ryakusho"),
                郵便番号              = S("postal_code"),
                住所１                = S("address1"),
                住所２                = S("address2"),
                住所３                = S("address3"),
                電話番号              = S("tel"),
                FAX番号               = S("fax"),
                納品先コード          = S("nohinsaki_code"),
                担当者コード          = S("tantosha_code"),
                与信限度額            = S("sinyo_limit"),
                単価処理区分          = S("tanka_kubun1"),
                単価処理単位          = S("tanka_unit1"),
                単価処理区分_1kg未満  = S("tanka_kubun2"),
                単価処理単位_1kg未満  = S("tanka_unit2"),
                金額処理区分          = S("kingaku_kubun"),
                単価処理区分_切板以外 = S("tanka_kubun3"),
                単価処理単位_切板以外 = S("tanka_unit3"),
                消費税計算処理区分    = S("shouhizei_kubun"),
                消費税計算処理単位    = S("shouhizei_unit"),
                消費税分解処理区分    = S("shouhizei_bunkai"),
                重量表示              = S("weight_display"),
                新パスワード適用年月日 = S("pw_date"),
                新パスワード適用時刻   = S("pw_time"),
            };
        }

        /// <summary>
        /// DBにデータが 1件以上存在するかを返す。
        /// </summary>
        public bool HasData()
        {
            try
            {
                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM TokisakiMaster");
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────
        // デイリーデータ（daily_data テーブル）
        // ─────────────────────────────────────────────────

        private const string DailyTableName = "daily_data";

        /// <summary>
        /// デイリーデータテーブルが存在するか確認する。
        /// </summary>
        public bool HasDailyTable()
        {
            try
            {
                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                var count = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name",
                    new { name = DailyTableName });
                return count > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// デイリーテーブルのデータ列名リストを返す（id・imported_at を除く）。
        /// </summary>
        public async Task<List<string>> GetDailyTableColumnsAsync()
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync($"PRAGMA table_info({DailyTableName})");
            var columns = new List<string>();
            foreach (IDictionary<string, object> row in rows)
            {
                var name = row["name"]?.ToString() ?? "";
                if (name != "id" && name != "imported_at")
                    columns.Add(name);
            }
            return columns;
        }

        /// <summary>
        /// デイリーテーブルの PRIMARY KEY 列名と型を返す（id を除く）。
        /// 存在しない場合は (null, null)。
        /// </summary>
        public async Task<(string? Column, string? Type)> GetDailyPrimaryKeyColumnAsync()
        {
            if (!HasDailyTable()) return (null, null);

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync($"PRAGMA table_info({DailyTableName})");
            foreach (IDictionary<string, object> row in rows)
            {
                int pk = row.TryGetValue("pk", out var pkObj) ? Convert.ToInt32(pkObj) : 0;
                var name = row["name"]?.ToString() ?? "";
                var type = row["type"]?.ToString() ?? "";
                if (pk != 0 && name != "id")
                    return (name, type.ToUpperInvariant());
            }
            return (null, null);
        }

        /// <summary>
        /// 指定した列名でデイリーデータテーブルを新規作成する。
        /// <paramref name="primaryKeyColumn"/> を指定するとその列を PRIMARY KEY とする（id 列なし）。
        /// <paramref name="primaryKeyIsInteger"/> が true の場合は INTEGER 型として定義する。
        /// </summary>
        public async Task CreateDailyTableAsync(
            IEnumerable<string> columns,
            string? primaryKeyColumn   = null,
            bool   primaryKeyIsInteger = false)
        {
            var colList = columns.ToList();
            string colDefs;
            string leadingCols;

            if (primaryKeyColumn != null
                && colList.Contains(primaryKeyColumn, StringComparer.Ordinal))
            {
                // 業務キーを PRIMARY KEY にする（AUTOINCREMENT id なし）
                // primaryKeyIsInteger=true の場合は INTEGER 型（数値ソート対応）
                string pkType = primaryKeyIsInteger ? "INTEGER" : "TEXT";
                colDefs = string.Join(",\n    ", colList.Select(c =>
                    string.Equals(c, primaryKeyColumn, StringComparison.Ordinal)
                        ? $"\"{c}\" {pkType} PRIMARY KEY"
                        : $"\"{c}\" TEXT"));
                leadingCols = "imported_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),";
            }
            else
            {
                // フォールバック：AUTOINCREMENT id
                colDefs = string.Join(",\n    ", colList.Select(c => $"\"{c}\" TEXT"));
                leadingCols = "id INTEGER PRIMARY KEY AUTOINCREMENT,\n    " +
                              "imported_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),";
            }

            var sql = $@"
                CREATE TABLE IF NOT EXISTS {DailyTableName} (
                    {leadingCols}
                    {colDefs}
                )";

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync(sql);
        }

        /// <summary>
        /// デイリーデータを追記する。PRIMARY KEY が重複する行は最新データで上書きする。
        /// </summary>
        /// <returns>(追記・更新件数, テーブル総件数) のタプル</returns>
        public async Task<(int Added, int Total)> AppendDailyDataAsync(
            List<string> columns, List<string[]> rows)
        {
            var colNames   = string.Join(", ", columns.Select(c => $"\"{c}\""));
            var paramNames = string.Join(", ", columns.Select((_, i) => $"@p{i}"));

            // INSERT OR REPLACE: PRIMARY KEY 重複時は既存行を削除して新行を挿入（上書き）
            var sql = $"INSERT OR REPLACE INTO {DailyTableName} ({colNames}) VALUES ({paramNames})";

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var row in rows)
                {
                    var param = new DynamicParameters();
                    for (int i = 0; i < columns.Count; i++)
                        param.Add($"p{i}", i < row.Length ? row[i] : "");
                    await conn.ExecuteAsync(sql, param, tx);
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM {DailyTableName}");
            return (rows.Count, total);
        }

        /// <summary>
        /// 指定した得意先コードに紐づく daily_data レコードを見積番号の降順で取得する。
        /// テーブル未作成・「得意先コード」列なしの場合は <see cref="InvalidOperationException"/> をスローする。
        /// </summary>
        /// <returns>(表示列名リスト, 行リスト) のタプル</returns>
        public async Task<(List<string> Columns, List<IDictionary<string, object>> Rows)>
            GetDailyDataByCodeAsync(string tokisakiCode)
        {
            if (!HasDailyTable())
                throw new InvalidOperationException(
                    "デイリーデータがまだ読み込まれていません。\n" +
                    "先に「デイリーCSV読込」ボタンでデータを読み込んでください。");

            const string linkColumn  = "得意先コード";
            const string orderColumn = "見積番号";

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            // id を除く全列を PRAGMA で取得（imported_at は表示する）
            var columns = new List<string>();
            var pragmaRows = await conn.QueryAsync($"PRAGMA table_info({DailyTableName})");
            foreach (IDictionary<string, object> pr in pragmaRows)
            {
                var name = pr["name"]?.ToString() ?? "";
                if (name != "id") columns.Add(name);
            }

            if (!columns.Contains(linkColumn, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    $"daily_data テーブルに「{linkColumn}」列がありません。\n" +
                    "得意先コードを含むCSVを読み込んでから再度お試しください。");

            bool hasOrderCol = columns.Contains(orderColumn, StringComparer.Ordinal);
            // CAST でテキスト型列でも数値降順ソートを保証する
            string orderBy   = hasOrderCol
                ? $"ORDER BY CAST(\"{orderColumn}\" AS INTEGER) DESC"
                : "";

            var rows = (await conn.QueryAsync(
                    $"SELECT * FROM {DailyTableName} " +
                    $"WHERE \"{linkColumn}\" = @code {orderBy}",
                    new { code = tokisakiCode }))
                .Cast<IDictionary<string, object>>()
                .ToList();

            return (columns, rows);
        }

        /// <summary>
        /// 既存の daily_data テーブルを <paramref name="primaryKeyColumn"/> を
        /// PRIMARY KEY として再構築する。重複行は imported_at が最新のものを残す。
        /// </summary>
        /// <param name="primaryKeyIsInteger">true の場合 PRIMARY KEY を INTEGER 型で定義する</param>
        /// <returns>再構築後のテーブル内総件数</returns>
        public async Task<int> RebuildDailyTableAsync(
            string primaryKeyColumn,
            bool   primaryKeyIsInteger = false)
        {
            // 現在のデータ列を取得
            var currentColumns = await GetDailyTableColumnsAsync();
            if (!currentColumns.Contains(primaryKeyColumn, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    $"列 「{primaryKeyColumn}」 が daily_data テーブルに存在しないため再構築できません。");

            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            // 全行読み込み（imported_at 昇順 → GroupBy.Last() で最新が残る）
            var allRows = (await conn.QueryAsync(
                    $"SELECT * FROM {DailyTableName} ORDER BY imported_at ASC"))
                .Cast<IDictionary<string, object>>()
                .ToList();

            // 業務キーで重複排除：同一キーは最新（最後）の行を採用
            var deduped = allRows
                .GroupBy(r => r.TryGetValue(primaryKeyColumn, out var v)
                              ? v?.ToString() ?? ""
                              : "")
                .Select(g => g.Last())
                .ToList();

            // テーブルを削除して作り直し（primaryKeyColumn を PRIMARY KEY に設定）
            await conn.ExecuteAsync($"DROP TABLE IF EXISTS {DailyTableName}");

            string pkType = primaryKeyIsInteger ? "INTEGER" : "TEXT";
            var colDefs = string.Join(",\n    ", currentColumns.Select(c =>
                string.Equals(c, primaryKeyColumn, StringComparison.Ordinal)
                    ? $"\"{c}\" {pkType} PRIMARY KEY"
                    : $"\"{c}\" TEXT"));

            await conn.ExecuteAsync($@"
                CREATE TABLE {DailyTableName} (
                    imported_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                    {colDefs}
                )");

            if (deduped.Count == 0) return 0;

            // 再挿入（imported_at も明示的に指定して元の値を保持）
            var allCols   = new[] { "imported_at" }.Concat(currentColumns).ToList();
            var colNames  = string.Join(", ", allCols.Select(c => $"\"{c}\""));
            var paramNms  = string.Join(", ", allCols.Select((_, i) => $"@p{i}"));
            var insertSql = $"INSERT OR REPLACE INTO {DailyTableName} ({colNames}) VALUES ({paramNms})";

            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var row in deduped)
                {
                    var param = new DynamicParameters();
                    for (int i = 0; i < allCols.Count; i++)
                    {
                        row.TryGetValue(allCols[i], out var val);
                        param.Add($"p{i}", val?.ToString() ?? "");
                    }
                    await conn.ExecuteAsync(insertSql, param, tx);
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            return deduped.Count;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ひらがな / カタカナ 正規化ヘルパー
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 検索キー生成・入力正規化のユーティリティ
    /// </summary>
    public static class KanaHelper
    {
        /// <summary>
        /// カタカナ→ひらがな変換、全角英数→半角、小文字化を行い検索キーを生成する。
        /// </summary>
        public static string Normalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                // 全角カタカナ (ァ=0x30A1 ～ ン=0x30F3) → ひらがな
                if (c >= '\u30A1' && c <= '\u30F3')
                {
                    sb.Append((char)(c - 0x60));
                    continue;
                }
                // 全角英大文字 (Ａ=0xFF21 ～ Ｚ=0xFF3A) → 半角小文字
                if (c >= '\uFF21' && c <= '\uFF3A')
                {
                    sb.Append((char)(c - 0xFEE0 + 32));
                    continue;
                }
                // 全角英小文字 (ａ=0xFF41 ～ ｚ=0xFF5A) → 半角小文字
                if (c >= '\uFF41' && c <= '\uFF5A')
                {
                    sb.Append((char)(c - 0xFEE0));
                    continue;
                }
                // 全角数字 (０=0xFF10 ～ ９=0xFF19) → 半角
                if (c >= '\uFF10' && c <= '\uFF19')
                {
                    sb.Append((char)(c - 0xFEE0));
                    continue;
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
