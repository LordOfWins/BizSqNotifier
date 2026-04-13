using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>tb_mail_log CRUD + 중복 방지 + 조회.</summary>
    public sealed class MailLogRepository
    {
        #region INSERT

        private const string InsertSql = @"
INSERT INTO dbo.tb_mail_log
    (mail_type, movein_id, invoice_id, cust_name, email, branch_code,
     send_date, send_time, status, error_msg)
VALUES
    (@mail_type, @movein_id, @invoice_id, @cust_name, @email, @branch_code,
     @send_date, @send_time, @status, @error_msg);";

        public void Insert(MailLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            try
            {
                DbManager.ExecuteNonQuery(InsertSql,
                    new SqlParameter("@mail_type", entry.MailType),
                    new SqlParameter("@movein_id", entry.MoveInId),
                    new SqlParameter("@invoice_id", (object)entry.InvoiceId ?? DBNull.Value),
                    new SqlParameter("@cust_name", entry.CustName ?? string.Empty),
                    new SqlParameter("@email", (object)entry.Email ?? DBNull.Value),
                    new SqlParameter("@branch_code", (object)entry.BranchCode ?? DBNull.Value),
                    new SqlParameter("@send_date", entry.SendDate),
                    new SqlParameter("@send_time", entry.SendTime),
                    new SqlParameter("@status", entry.Status ?? "실패"),
                    new SqlParameter("@error_msg", (object)entry.ErrorMessage ?? DBNull.Value));
                AppLog.Info($"로그 INSERT — {entry.MailType} / {entry.CustName} / {entry.Status}");
            }
            catch (Exception ex)
            {
                AppLog.Error($"로그 INSERT 실패 — {entry.MailType} / {entry.CustName}", ex);
                throw;
            }
        }

        #endregion

        #region 중복 방지

        public bool HasSentByMoveIn(string mailType, int moveInId)
        {
            const string sql = @"
SELECT COUNT(1) FROM dbo.tb_mail_log
WHERE mail_type = @mail_type AND movein_id = @movein_id AND status IN ('성공','SKIP');";
            var r = DbManager.ExecuteScalar(sql,
                new SqlParameter("@mail_type", mailType),
                new SqlParameter("@movein_id", moveInId));
            return Convert.ToInt32(r) > 0;
        }

        public bool HasSentByInvoice(string mailType, int invoiceId)
        {
            const string sql = @"
SELECT COUNT(1) FROM dbo.tb_mail_log
WHERE mail_type = @mail_type AND invoice_id = @invoice_id AND status IN ('성공','SKIP');";
            var r = DbManager.ExecuteScalar(sql,
                new SqlParameter("@mail_type", mailType),
                new SqlParameter("@invoice_id", invoiceId));
            return Convert.ToInt32(r) > 0;
        }

        public bool HasSentManualRenewalToday(int moveInId)
        {
            const string sql = @"
SELECT COUNT(1) FROM dbo.tb_mail_log
WHERE mail_type = @t AND movein_id = @m AND send_date = @d AND status = '성공';";
            var r = DbManager.ExecuteScalar(sql,
                new SqlParameter("@t", MailTypes.RenewalManual),
                new SqlParameter("@m", moveInId),
                new SqlParameter("@d", DateTime.Today));
            return Convert.ToInt32(r) > 0;
        }

        #endregion

        #region 조회

        public Dictionary<string, (int Success, int Fail, int Skip)> GetTodaySummary()
        {
            const string sql = @"
SELECT mail_type,
    SUM(CASE WHEN status='성공' THEN 1 ELSE 0 END) AS cnt_success,
    SUM(CASE WHEN status='실패' THEN 1 ELSE 0 END) AS cnt_fail,
    SUM(CASE WHEN status='SKIP' THEN 1 ELSE 0 END) AS cnt_skip
FROM dbo.tb_mail_log WHERE send_date = @today GROUP BY mail_type;";

            var dict = new Dictionary<string, (int, int, int)>();
            var rows = DbManager.ExecuteReader(sql, reader =>
            {
                var type = DbManager.GetSafeString(reader, "mail_type") ?? "기타";
                return (type, DbManager.GetSafeInt(reader, "cnt_success"),
                    DbManager.GetSafeInt(reader, "cnt_fail"), DbManager.GetSafeInt(reader, "cnt_skip"));
            }, new SqlParameter("@today", DateTime.Today));

            foreach (var r in rows) dict[r.type] = (r.Item2, r.Item3, r.Item4);
            return dict;
        }

        public DataTable GetLogTable(DateTime? dateFrom = null, DateTime? dateTo = null,
            string mailType = null, string custName = null, string status = null,
            string branchCode = null, int maxRows = 500)
        {
            var conds = new List<string>();
            var parms = new List<SqlParameter>();

            if (dateFrom.HasValue) { conds.Add("send_date >= @df"); parms.Add(new SqlParameter("@df", dateFrom.Value.Date)); }
            if (dateTo.HasValue) { conds.Add("send_date <= @dt"); parms.Add(new SqlParameter("@dt", dateTo.Value.Date)); }
            if (!string.IsNullOrWhiteSpace(mailType)) { conds.Add("mail_type = @mt"); parms.Add(new SqlParameter("@mt", mailType)); }
            if (!string.IsNullOrWhiteSpace(custName)) { conds.Add("cust_name LIKE @cn"); parms.Add(new SqlParameter("@cn", "%" + custName.Trim() + "%")); }
            if (!string.IsNullOrWhiteSpace(status)) { conds.Add("status = @st"); parms.Add(new SqlParameter("@st", status)); }
            if (!string.IsNullOrWhiteSpace(branchCode)) { conds.Add("branch_code = @bc"); parms.Add(new SqlParameter("@bc", branchCode)); }

            var where = conds.Count > 0 ? "WHERE " + string.Join(" AND ", conds) : "";
            var sql = $@"
SELECT TOP (@mx) log_id AS [No], mail_type AS [유형], cust_name AS [회사명],
    email AS [이메일], send_date AS [발송일], send_time AS [발송시각],
    status AS [상태], error_msg AS [에러메시지], movein_id AS [MoveInID],
    invoice_id AS [InvoiceID], created_at AS [기록일시]
FROM dbo.tb_mail_log {where} ORDER BY log_id DESC;";
            parms.Add(new SqlParameter("@mx", maxRows));
            return DbManager.ExecuteQuery(sql, parms.ToArray());
        }

        public List<MailLogEntry> Search(DateTime? dateFrom = null, DateTime? dateTo = null,
            string mailType = null, string custName = null, string status = null, int maxRows = 500)
        {
            var conds = new List<string>();
            var parms = new List<SqlParameter>();

            if (dateFrom.HasValue) { conds.Add("send_date >= @df"); parms.Add(new SqlParameter("@df", dateFrom.Value.Date)); }
            if (dateTo.HasValue) { conds.Add("send_date <= @dt"); parms.Add(new SqlParameter("@dt", dateTo.Value.Date)); }
            if (!string.IsNullOrWhiteSpace(mailType)) { conds.Add("mail_type = @mt"); parms.Add(new SqlParameter("@mt", mailType)); }
            if (!string.IsNullOrWhiteSpace(custName)) { conds.Add("cust_name LIKE @cn"); parms.Add(new SqlParameter("@cn", "%" + custName.Trim() + "%")); }
            if (!string.IsNullOrWhiteSpace(status)) { conds.Add("status = @st"); parms.Add(new SqlParameter("@st", status)); }

            var where = conds.Count > 0 ? "WHERE " + string.Join(" AND ", conds) : "";
            var sql = $@"
SELECT TOP (@mx) log_id, mail_type, movein_id, invoice_id, cust_name, email,
    branch_code, send_date, send_time, status, error_msg, created_at
FROM dbo.tb_mail_log {where} ORDER BY log_id DESC;";
            parms.Add(new SqlParameter("@mx", maxRows));

            return DbManager.ExecuteReader(sql, reader => new MailLogEntry
            {
                LogId = DbManager.GetSafeInt(reader, "log_id"),
                MailType = DbManager.GetSafeString(reader, "mail_type"),
                MoveInId = DbManager.GetSafeInt(reader, "movein_id"),
                InvoiceId = DbManager.GetSafeNullableInt(reader, "invoice_id"),
                CustName = DbManager.GetSafeString(reader, "cust_name"),
                Email = DbManager.GetSafeString(reader, "email"),
                BranchCode = DbManager.GetSafeString(reader, "branch_code"),
                SendDate = DbManager.GetSafeDateTimeValue(reader, "send_date", DateTime.MinValue),
                SendTime = reader.IsDBNull(reader.GetOrdinal("send_time"))
                    ? TimeSpan.Zero : (TimeSpan)reader.GetValue(reader.GetOrdinal("send_time")),
                Status = DbManager.GetSafeString(reader, "status"),
                ErrorMessage = DbManager.GetSafeString(reader, "error_msg"),
                CreatedAt = DbManager.GetSafeDateTime(reader, "created_at")
            }, parms.ToArray());
        }

        #endregion
    }
}
