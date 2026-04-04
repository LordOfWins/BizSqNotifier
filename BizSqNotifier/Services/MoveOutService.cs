using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>퇴실 안내 메일 자동 발송 서비스.</summary>
    public sealed class MoveOutService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;
        public const int DefaultDaysBefore = 1;

        public MoveOutService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region SQL

        private const string SelectMoveOutTargetsSql = @"
SELECT
    m.movein_id, m.cust AS cust_name, c.email AS email,
    m.br_id AS branch_id, b.br_name AS branch_name,
    m.prd_prd AS product_name, m.off_num AS office_num,
    ISNULL(m.deposit, 0) AS deposit, m.date_to, m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) AS days_until_moveout
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE m.date_out IS NOT NULL
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) = @daysBefore
ORDER BY m.date_out ASC, m.cust;";

        private const string SelectMoveOutRangeSql = @"
SELECT
    m.movein_id, m.cust AS cust_name, c.email AS email,
    m.br_id AS branch_id, b.br_name AS branch_name,
    m.prd_prd AS product_name, m.off_num AS office_num,
    ISNULL(m.deposit, 0) AS deposit, m.date_to, m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) AS days_until_moveout
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE m.date_out IS NOT NULL
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) BETWEEN @daysFrom AND @daysTo
ORDER BY m.date_out ASC, m.cust;";

        #endregion

        #region 조회

        public List<MoveOutInfo> GetTargets(int daysBefore = DefaultDaysBefore)
        {
            try
            {
                return DbManager.ExecuteReader(SelectMoveOutTargetsSql, MapRow,
                    new SqlParameter("@daysBefore", daysBefore));
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 대상 조회 실패 (D-{daysBefore})", ex);
                return new List<MoveOutInfo>();
            }
        }

        /// <summary>대시보드용 범위 조회 (0~7일 1회 쿼리)</summary>
        public List<MoveOutInfo> GetTargetsInRange(int daysFrom, int daysTo)
        {
            try
            {
                return DbManager.ExecuteReader(SelectMoveOutRangeSql, MapRow,
                    new SqlParameter("@daysFrom", daysFrom),
                    new SqlParameter("@daysTo", daysTo));
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 범위 조회 실패 ({daysFrom}~{daysTo})", ex);
                return new List<MoveOutInfo>();
            }
        }

        #endregion

        #region 자동 발송

        public (int Total, int Success, int Fail, int Skip) ProcessAll(int daysBefore = DefaultDaysBefore)
        {
            var targets = GetTargets(daysBefore);
            int success = 0, fail = 0, skip = 0;
            AppLog.Info($"[퇴실] 대상 {targets.Count}건 (D-{daysBefore})");

            foreach (var info in targets)
            {
                try
                {
                    var r = ProcessOne(info);
                    switch (r.Status) { case "성공": success++; break; case "SKIP": skip++; break; default: fail++; break; }
                }
                catch (Exception ex) { fail++; AppLog.Error($"[퇴실] 오류 — MoveInId={info.MoveInId}", ex); }
            }

            AppLog.Info($"[퇴실] 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}");
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 발송

        public SendResult ProcessOne(MoveOutInfo info)
        {
            if (info == null) return SendResult.Fail("MoveOutInfo null");
            if (_logRepo.HasSentByMoveIn(MailTypes.MoveOut, info.MoveInId))
                return SendResult.Skip("이미 발송 완료");
            if (string.IsNullOrWhiteSpace(info.Email))
            { var s = SendResult.Skip("이메일 미등록"); LogResult(info, s); return s; }

            var tokens = BuildTokens(info);
            var subject = _template.RenderSubject(TemplateEngine.GetDefaultSubject(MailTypes.MoveOut), tokens);
            var body = _template.LoadAndRender(TemplateFiles.MoveOut, tokens);
            if (string.IsNullOrEmpty(body))
            { var f = SendResult.Fail("템플릿 로드 실패"); LogResult(info, f); return f; }

            var result = _smtp.SendByBranch(info.BranchId ?? 0, info.Email, subject, body);
            LogResult(info, result);
            return result;
        }

        #endregion

        #region 토큰 / 로그 / 매퍼

        private static Dictionary<string, string> BuildTokens(MoveOutInfo info)
        {
            return new Dictionary<string, string>
            {
                ["회사명"] = info.CustName ?? "", ["퇴실예정일"] = info.DateOut?.ToString("yyyy-MM-dd") ?? "",
                ["지점"] = info.BranchName ?? "", ["상품/분류"] = info.ProductName ?? "",
                ["호실"] = info.OfficeNum ?? "", ["예치금"] = info.Deposit.ToString("#,0") + "원",
                ["계약종료일"] = info.DateTo?.ToString("yyyy-MM-dd") ?? ""
            };
        }

        private void LogResult(MoveOutInfo info, SendResult result)
        {
            try
            {
                _logRepo.Insert(MailLogEntry.Create(MailTypes.MoveOut, info.MoveInId,
                    info.CustName, info.Email, info.BranchId, result));
            }
            catch (Exception ex) { AppLog.Error($"[퇴실] 로그 실패 — {info.MoveInId}", ex); }
        }

        private static MoveOutInfo MapRow(SqlDataReader reader)
        {
            return new MoveOutInfo
            {
                MoveInId = DbManager.GetSafeInt(reader, "movein_id"),
                CustName = DbManager.GetSafeString(reader, "cust_name"),
                Email = DbManager.GetSafeString(reader, "email"),
                BranchId = DbManager.GetSafeNullableInt(reader, "branch_id"),
                BranchName = DbManager.GetSafeString(reader, "branch_name"),
                ProductName = DbManager.GetSafeString(reader, "product_name"),
                OfficeNum = DbManager.GetSafeString(reader, "office_num"),
                Deposit = DbManager.GetSafeDecimal(reader, "deposit"),
                DateTo = DbManager.GetSafeDateTime(reader, "date_to"),
                DateOut = DbManager.GetSafeDateTime(reader, "date_out"),
                DaysUntilMoveOut = DbManager.GetSafeInt(reader, "days_until_moveout")
            };
        }

        #endregion
    }
}
