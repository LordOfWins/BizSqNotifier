using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>
    /// tb_branch 조회 — 지점별 SMTP 설정 및 지점 정보 로드.
    /// [ESTIMATED] SMTP 컬럼명은 추정값. 최초 실행 시 자동 감지합니다.
    /// </summary>
    public sealed class BranchRepository
    {
        private static bool _columnsChecked = false;
        private static HashSet<string> _existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        #region 컬럼 자동 감지

        private static void EnsureColumnsChecked()
        {
            if (_columnsChecked) return;
            try
            {
                const string sql = @"
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tb_branch';";

                var rows = DbManager.ExecuteReader(sql, reader =>
                    DbManager.GetSafeString(reader, "COLUMN_NAME"));
                _existingColumns = new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
                _columnsChecked = true;
                AppLog.Info($"[BranchRepo] tb_branch 컬럼: {string.Join(", ", _existingColumns)}");
            }
            catch (Exception ex)
            {
                AppLog.Error("[BranchRepo] 컬럼 감지 실패", ex);
                _columnsChecked = true;
            }
        }

        private static string Col(string columnName, string alias = null)
        {
            alias = alias ?? columnName;
            if (_existingColumns.Count == 0 || _existingColumns.Contains(columnName))
                return $"{columnName} AS [{alias}]";
            return $"NULL AS [{alias}]";
        }

        #endregion

        #region 조회

        public BranchSmtpInfo GetByBranchId(int branchId)
        {
            EnsureColumnsChecked();
            var sql = BuildSelectSql("WHERE br_id = @br_id");
            var list = DbManager.ExecuteReader(sql, MapRow, new SqlParameter("@br_id", branchId));
            return list.Count > 0 ? list[0] : null;
        }

        public List<BranchSmtpInfo> GetAll()
        {
            EnsureColumnsChecked();
            var sql = BuildSelectSql("ORDER BY br_name");
            return DbManager.ExecuteReader(sql, MapRow);
        }

        #endregion

        #region 동적 SQL

        private static string BuildSelectSql(string whereOrOrder)
        {
            return $@"
SELECT
    br_id,
    {Col("br_name")},
    {Col("smtp_host")},
    {Col("smtp_port")},
    {Col("smtp_ssl")},
    {Col("smtp_user")},
    {Col("smtp_pwd")},
    {Col("smtp_from")},
    {Col("smtp_name")},
    {Col("bank_accnt")},
    {Col("bank_holder")}
FROM dbo.tb_branch
{whereOrOrder};";
        }

        #endregion

        #region Row Mapper

        private static BranchSmtpInfo MapRow(SqlDataReader reader)
        {
            return new BranchSmtpInfo
            {
                BranchId        = SafeGetInt(reader, "br_id"),
                BranchName      = SafeGetString(reader, "br_name"),
                SmtpHost        = SafeGetString(reader, "smtp_host"),
                SmtpPort        = SafeGetInt(reader, "smtp_port", 587),
                EnableSsl       = SafeGetInt(reader, "smtp_ssl", 1) == 1,
                SmtpUser        = SafeGetString(reader, "smtp_user"),
                SmtpPassword    = SafeGetString(reader, "smtp_pwd"),
                FromAddress     = SafeGetString(reader, "smtp_from"),
                FromDisplayName = SafeGetString(reader, "smtp_name"),
                BankAccount     = SafeGetString(reader, "bank_accnt"),
                BankHolder      = SafeGetString(reader, "bank_holder")
            };
        }

        private static string SafeGetString(SqlDataReader reader, string column)
        {
            try { return DbManager.GetSafeString(reader, column); }
            catch { return null; }
        }

        private static int SafeGetInt(SqlDataReader reader, string column, int defaultValue = 0)
        {
            try { return DbManager.GetSafeInt(reader, column, defaultValue); }
            catch { return defaultValue; }
        }

        #endregion
    }
}
