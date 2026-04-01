using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier.Data
{
    /// <summary>
    /// tb_mail_log CRUD + 중복 발송 방지 체크 + 조회.
    /// 모든 쿼리는 parameterized query를 사용합니다.
    /// </summary>
    public sealed class MailLogRepository
    {
        #region INSERT — 로그 기록

        private const string InsertSql = @"
INSERT INTO dbo.tb_mail_log
    (mail_type, movein_id, invoice_id, cust_name, email, branch_id,
     send_date, send_time, status, error_msg)
VALUES
    (@mail_type, @movein_id, @invoice_id, @cust_name, @email, @branch_id,
     @send_date, @send_time, @status, @error_msg);";

        /// <summary>
        /// 발송 로그를 INSERT합니다.
        /// </summary>
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
                    new SqlParameter("@branch_id", (object)entry.BranchId ?? DBNull.Value),
                    new SqlParameter("@send_date", entry.SendDate),
                    new SqlParameter("@send_time", entry.SendTime),
                    new SqlParameter("@status", entry.Status ?? "실패"),
                    new SqlParameter("@error_msg", (object)entry.ErrorMessage ?? DBNull.Value)
                );

                AppLog.Info($"로그 INSERT — {entry.MailType} / {entry.CustName} / {entry.Status}");
            }
            catch (Exception ex)
            {
                AppLog.Error($"로그 INSERT 실패 — {entry.MailType} / {entry.CustName}", ex);
                throw;
            }
        }

        #endregion

        #region 중복 발송 방지 — 입주 / 퇴실 / 갱신자동

        /// <summary>
        /// 특정 mail_type + movein_id 조합으로 성공(또는 SKIP) 로그가 이미 존재하는지 확인합니다.
        /// 입주/퇴실/갱신자동처럼 movein_id 기반 1회성 발송에 사용합니다.
        /// </summary>
        public bool HasSentByMoveIn(string mailType, int moveInId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.tb_mail_log
WHERE mail_type = @mail_type
  AND movein_id = @movein_id
  AND status IN ('성공', 'SKIP');";

            var result = DbManager.ExecuteScalar(sql,
                new SqlParameter("@mail_type", mailType),
                new SqlParameter("@movein_id", moveInId));

            return Convert.ToInt32(result) > 0;
        }

        #endregion

        #region 중복 발송 방지 — 미납 (invoice_id 기반)

        /// <summary>
        /// 특정 mail_type + invoice_id 조합으로 성공(또는 SKIP) 로그가 이미 존재하는지 확인합니다.
        /// 미납 1차/2차/최종처럼 청구서 단위 발송에 사용합니다.
        /// </summary>
        public bool HasSentByInvoice(string mailType, int invoiceId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.tb_mail_log
WHERE mail_type = @mail_type
  AND invoice_id = @invoice_id
  AND status IN ('성공', 'SKIP');";

            var result = DbManager.ExecuteScalar(sql,
                new SqlParameter("@mail_type", mailType),
                new SqlParameter("@invoice_id", invoiceId));

            return Convert.ToInt32(result) > 0;
        }

        #endregion

        #region 중복 발송 방지 — 갱신수동 (movein_id + 날짜 범위)

        /// <summary>
        /// 갱신수동은 동일 계약에 대해 같은 날 중복 발송만 방지합니다.
        /// (담당자가 조건 변경 후 재발송할 수 있으므로 날짜 단위 체크)
        /// </summary>
        public bool HasSentManualRenewalToday(int moveInId)
        {
            const string sql = @"
SELECT COUNT(1)
FROM dbo.tb_mail_log
WHERE mail_type = @mail_type
  AND movein_id = @movein_id
  AND send_date = @today
  AND status = '성공';";

            var result = DbManager.ExecuteScalar(sql,
                new SqlParameter("@mail_type", MailTypes.RenewalManual),
                new SqlParameter("@movein_id", moveInId),
                new SqlParameter("@today", DateTime.Today));

            return Convert.ToInt32(result) > 0;
        }

        #endregion

        #region 조회 — 기간별/유형별/고객별 검색 (LogViewForm용)

        /// <summary>
        /// 발송 로그를 검색합니다. 모든 파라미터는 선택적(null 시 필터 미적용).
        /// </summary>
        /// <param name="dateFrom">시작일 (null → 제한없음)</param>
        /// <param name="dateTo">종료일 (null → 제한없음)</param>
        /// <param name="mailType">메일 유형 (null → 전체)</param>
        /// <param name="custName">회사명 검색어 (null → 전체, LIKE 검색)</param>
        /// <param name="status">상태 필터 (null → 전체)</param>
        /// <param name="maxRows">최대 반환 행 수 (기본 500)</param>
        public List<MailLogEntry> Search(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string mailType = null,
            string custName = null,
            string status = null,
            int maxRows = 500)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (dateFrom.HasValue)
            {
                conditions.Add("send_date >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", dateFrom.Value.Date));
            }
            if (dateTo.HasValue)
            {
                conditions.Add("send_date <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", dateTo.Value.Date));
            }
            if (!string.IsNullOrWhiteSpace(mailType))
            {
                conditions.Add("mail_type = @mailType");
                parameters.Add(new SqlParameter("@mailType", mailType));
            }
            if (!string.IsNullOrWhiteSpace(custName))
            {
                conditions.Add("cust_name LIKE @custName");
                parameters.Add(new SqlParameter("@custName", "%" + custName.Trim() + "%"));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                conditions.Add("status = @status");
                parameters.Add(new SqlParameter("@status", status));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            var sql = $@"
SELECT TOP (@maxRows)
    log_id, mail_type, movein_id, invoice_id, cust_name, email,
    branch_id, send_date, send_time, status, error_msg, created_at
FROM dbo.tb_mail_log
{whereClause}
ORDER BY log_id DESC;";

            parameters.Add(new SqlParameter("@maxRows", maxRows));

            return DbManager.ExecuteReader(sql, MapRow, parameters.ToArray());
        }

        #endregion

        #region 조회 — 오늘 발송 현황 요약 (대시보드용)

        /// <summary>
        /// 오늘 날짜 기준 mail_type별 발송 건수 요약을 반환합니다.
        /// Key = mail_type, Value = (성공건수, 실패건수, SKIP건수)
        /// </summary>
        public Dictionary<string, (int Success, int Fail, int Skip)> GetTodaySummary()
        {
            const string sql = @"
SELECT
    mail_type,
    SUM(CASE WHEN status = '성공' THEN 1 ELSE 0 END) AS cnt_success,
    SUM(CASE WHEN status = '실패' THEN 1 ELSE 0 END) AS cnt_fail,
    SUM(CASE WHEN status = 'SKIP' THEN 1 ELSE 0 END) AS cnt_skip
FROM dbo.tb_mail_log
WHERE send_date = @today
GROUP BY mail_type;";

            var dict = new Dictionary<string, (int, int, int)>();

            var rows = DbManager.ExecuteReader(sql, reader =>
            {
                var type = DbManager.GetSafeString(reader, "mail_type") ?? "기타";
                var success = DbManager.GetSafeInt(reader, "cnt_success");
                var fail = DbManager.GetSafeInt(reader, "cnt_fail");
                var skip = DbManager.GetSafeInt(reader, "cnt_skip");
                return (type, success, fail, skip);
            },
            new SqlParameter("@today", DateTime.Today));

            foreach (var r in rows)
            {
                dict[r.type] = (r.success, r.fail, r.skip);
            }

            return dict;
        }

        #endregion

        #region 조회 — 특정 날짜 범위 로그 (DataTable 반환 — DataGridView 바인딩용)

        /// <summary>
        /// DataGridView에 직접 바인딩할 수 있도록 DataTable로 반환합니다.
        /// </summary>
        public DataTable GetLogTable(
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string mailType = null,
            string custName = null,
            string status = null,
            int maxRows = 500)
        {
            var conditions = new List<string>();
            var parameters = new List<SqlParameter>();

            if (dateFrom.HasValue)
            {
                conditions.Add("send_date >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", dateFrom.Value.Date));
            }
            if (dateTo.HasValue)
            {
                conditions.Add("send_date <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", dateTo.Value.Date));
            }
            if (!string.IsNullOrWhiteSpace(mailType))
            {
                conditions.Add("mail_type = @mailType");
                parameters.Add(new SqlParameter("@mailType", mailType));
            }
            if (!string.IsNullOrWhiteSpace(custName))
            {
                conditions.Add("cust_name LIKE @custName");
                parameters.Add(new SqlParameter("@custName", "%" + custName.Trim() + "%"));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                conditions.Add("status = @status");
                parameters.Add(new SqlParameter("@status", status));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            var sql = $@"
SELECT TOP (@maxRows)
    log_id      AS [No],
    mail_type   AS [유형],
    cust_name   AS [회사명],
    email       AS [이메일],
    send_date   AS [발송일],
    send_time   AS [발송시각],
    status      AS [상태],
    error_msg   AS [에러메시지],
    movein_id   AS [MoveInID],
    invoice_id  AS [InvoiceID],
    created_at  AS [기록일시]
FROM dbo.tb_mail_log
{whereClause}
ORDER BY log_id DESC;";

            parameters.Add(new SqlParameter("@maxRows", maxRows));

            return DbManager.ExecuteQuery(sql, parameters.ToArray());
        }

        #endregion

        #region Row Mapper

        /// <summary>SqlDataReader -> MailLogEntry 매핑.</summary>
        private static MailLogEntry MapRow(SqlDataReader reader)
        {
            return new MailLogEntry
            {
                LogId = DbManager.GetSafeInt(reader, "log_id"),
                MailType = DbManager.GetSafeString(reader, "mail_type"),
                MoveInId = DbManager.GetSafeInt(reader, "movein_id"),
                InvoiceId = DbManager.GetSafeNullableInt(reader, "invoice_id"),
                CustName = DbManager.GetSafeString(reader, "cust_name"),
                Email = DbManager.GetSafeString(reader, "email"),
                BranchId = DbManager.GetSafeNullableInt(reader, "branch_id"),
                SendDate = DbManager.GetSafeDateTimeValue(reader, "send_date", DateTime.MinValue),
                SendTime = reader.IsDBNull(reader.GetOrdinal("send_time"))
                    ? TimeSpan.Zero
                    : (TimeSpan)reader.GetValue(reader.GetOrdinal("send_time")),
                Status = DbManager.GetSafeString(reader, "status"),
                ErrorMessage = DbManager.GetSafeString(reader, "error_msg"),
                CreatedAt = DbManager.GetSafeDateTime(reader, "created_at")
            };
        }

        #endregion
    }
}
