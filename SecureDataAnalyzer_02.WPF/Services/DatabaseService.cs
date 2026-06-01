using System;
using System.Collections.Generic;
using System.IO;
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
        /// 全件を候補リスト形式で取得する（全件表示ボタン用）。
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
        /// 得意先コードで 1件の詳細データを取得する。
        /// </summary>
        public async Task<TokisakiMaster?> GetByCodeAsync(string code)
        {
            using var conn = new SqliteConnection(ConnectionString);
            await conn.OpenAsync();

            var row = await conn.QuerySingleOrDefaultAsync(@"
                SELECT tokisaki_code   AS 得意先コード,
                       kana_name       AS 取引先名ひらがな,
                       name1           AS 得意先名１,
                       name2           AS 得意先名２,
                       ryakusho        AS 得意先略称,
                       postal_code     AS 郵便番号,
                       address1        AS 住所１,
                       address2        AS 住所２,
                       address3        AS 住所３,
                       tel             AS 電話番号,
                       fax             AS FAX番号,
                       nohinsaki_code  AS 納品先コード,
                       tantosha_code   AS 担当者コード,
                       sinyo_limit     AS 与信限度額,
                       tanka_kubun1    AS 単価処理区分,
                       tanka_unit1     AS 単価処理単位,
                       tanka_kubun2    AS 単価処理区分_1kg未満,
                       tanka_unit2     AS 単価処理単位_1kg未満,
                       kingaku_kubun   AS 金額処理区分,
                       tanka_kubun3    AS 単価処理区分_切板以外,
                       tanka_unit3     AS 単価処理単位_切板以外,
                       shouhizei_kubun AS 消費税計算処理区分,
                       shouhizei_unit  AS 消費税計算処理単位,
                       shouhizei_bunkai AS 消費税分解処理区分,
                       weight_display  AS 重量表示,
                       pw_date         AS 新パスワード適用年月日,
                       pw_time         AS 新パスワード適用時刻
                FROM   TokisakiMaster
                WHERE  tokisaki_code = @code",
                new { code });

            return row;
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
