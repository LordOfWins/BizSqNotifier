using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 퇴실 안내 메일 자동 발송.
    /// 발송 조건: date_out N일 전 AND 로그 없음
    /// date_out은 varchar(10) 'yyyy-MM-dd'
    /// </summary>
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

        private const string SelectTargetsSql =
            @"
SELECT
    m.id                    AS movein_id,
    m.br_code,
    m.cust                  AS cust_name,
    c.email,
    b.br_name               AS branch_name,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) AS days_until_moveout
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id   = c.id
    LEFT JOIN dbo.tb_branch   b ON m.br_code = b.br_code
WHERE m.date_to IS NOT NULL
  AND m.date_to <> ''
  AND ISDATE(m.date_to) = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) BETWEEN 0 AND @maxDays
  AND (m.date_out IS NULL OR m.date_out = '')
  AND ISNULL(m.prd_prd, '') <> '모아즈'
ORDER BY m.date_to ASC, m.cust;";

        private const string SelectTargetsRangeSql =
            @"
SELECT
    m.id                    AS movein_id,
    m.br_code,
    m.cust                  AS cust_name,
    c.email,
    b.br_name               AS branch_name,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) AS days_until_moveout
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id   = c.id
    LEFT JOIN dbo.tb_branch   b ON m.br_code = b.br_code
WHERE m.date_out IS NOT NULL
  AND m.date_out <> ''
  AND ISDATE(m.date_out) = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) BETWEEN 1 AND @maxDays
  AND ISNULL(m.prd_prd, '') <> '모아즈'
ORDER BY m.date_out ASC, m.cust;";

        #endregion

        #region 대상 조회

        public List<MoveOutInfo> GetTargets(int daysBefore = DefaultDaysBefore)
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectTargetsSql,
                    MapRow,
                    new SqlParameter("@daysBefore", daysBefore)
                );
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 조회 실패 (D-{daysBefore})", ex);
                return new List<MoveOutInfo>();
            }
        }

        /// <summary>오늘~maxDays일 이내 퇴실 대상 범위 조회.</summary>
        public List<MoveOutInfo> GetTargetsWithinDays(int maxDays = 7)
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectTargetsRangeSql,
                    MapRow,
                    new SqlParameter("@maxDays", maxDays)
                );
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 범위 조회 실패 (0~{maxDays}일)", ex);
                return new List<MoveOutInfo>();
            }
        }

        #endregion

        #region 전체 처리

        public (int Total, int Success, int Fail, int Skip) ProcessAll(
            int daysBefore = DefaultDaysBefore
        )
        {
            var targets = GetTargets(daysBefore);
            int success = 0,
                fail = 0,
                skip = 0;
            AppLog.Info($"[퇴실] 대상 {targets.Count}건 (D-{daysBefore})");

            foreach (var info in targets)
            {
                try
                {
                    var r = ProcessOne(info);
                    switch (r.Status)
                    {
                        case "성공":
                            success++;
                            break;
                        case "SKIP":
                            skip++;
                            break;
                        default:
                            fail++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    AppLog.Error($"[퇴실] 오류 {info.MoveInId}", ex);
                }
            }

            AppLog.Info(
                $"[퇴실] 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}"
            );
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 처리

        public SendResult ProcessOne(MoveOutInfo info)
        {
            if (info == null)
                return SendResult.Fail("null");

            if (_logRepo.HasSentByMoveIn(MailTypes.MoveOut, info.MoveInId))
                return SendResult.Skip("이미 발송 완료");

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var s = SendResult.Skip("이메일 미등록");
                LogResult(info, s);
                return s;
            }

            var tokens = BuildTokens(info);
            var subject = _template.RenderSubject(
                TemplateEngine.GetDefaultSubject(MailTypes.MoveOut),
                tokens
            );
            var body = _template.LoadAndRender(TemplateFiles.MoveOut, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var f = SendResult.Fail("템플릿 로드 실패");
                LogResult(info, f);
                return f;
            }

            var result = _smtp.SendByBranch(info.BranchCode, info.Email, subject, body);
            LogResult(info, result);
            return result;
        }

        #endregion

        #region 토큰

        private static Dictionary<string, string> BuildTokens(MoveOutInfo info)
        {
            return new Dictionary<string, string>
            {
                ["회사명"] = info.CustName ?? "",
                ["퇴실예정일"] = info.DateOut ?? "",
                ["지점"] = info.BranchName ?? "",
                ["상품/분류"] = info.ProductName ?? "",
                ["호실"] = info.OfficeNum ?? "",
                ["예치금"] = info.Deposit.ToString("#,0") + "원",
                ["계약종료일"] = info.DateTo ?? "",
            };
        }

        #endregion

        #region 로그

        private void LogResult(MoveOutInfo info, SendResult result)
        {
            try
            {
                _logRepo.Insert(
                    MailLogEntry.Create(
                        MailTypes.MoveOut,
                        info.MoveInId,
                        info.CustName,
                        info.Email,
                        info.BranchCode,
                        result
                    )
                );
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 로그 실패 {info.MoveInId}", ex);
            }
        }

        #endregion

        #region Mapper

        private static MoveOutInfo MapRow(SqlDataReader r)
        {
            return new MoveOutInfo
            {
                MoveInId = DbManager.GetSafeInt(r, "movein_id"),
                BranchCode = DbManager.GetSafeString(r, "br_code"),
                CustName = DbManager.GetSafeString(r, "cust_name"),
                Email = DbManager.GetSafeString(r, "email"),
                BranchName = DbManager.GetSafeString(r, "branch_name"),
                ProductName = DbManager.GetSafeString(r, "product_name"),
                OfficeNum = DbManager.GetSafeString(r, "office_num"),
                Deposit = DbManager.GetSafeInt(r, "deposit"),
                DateTo = DbManager.GetSafeString(r, "date_to"),
                DateOut = DbManager.GetSafeString(r, "date_out"),
                DaysUntilMoveOut = DbManager.GetSafeInt(r, "days_until_moveout"),
            };
        }

        #endregion
    }
}
