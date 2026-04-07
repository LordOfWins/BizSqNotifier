using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 계약 갱신 (adBox/주소지/스마트데스크) 자동 발송.
    /// 대상: prd_prd IN ('개인사업자', '법인사업자', '스마트데스크')
    /// 제외: 오피스(~인실) → RenewalManualService에서 수동 처리
    /// 제외: 모아즈 → 발송 대상 아님
    /// 기준: 계약종료일 8일 전 (설정 변경 가능)
    /// </summary>
    public sealed class RenewalAutoService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        public const int DefaultDaysBefore = 8;

        public RenewalAutoService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region SQL

        // date_to는 varchar(10) 'yyyy-MM-dd'
        // 오피스가 아닌 모든 상품 = adBox/회원제
        private const string SelectTargetsSql =
            @"
SELECT
    m.id                    AS movein_id,
    m.br_code,
    m.cust                  AS cust_name,
    c.email,
    b.br_name               AS branch_name,
    b.bank_accnt            AS bank_account,
    b.bank_holder,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    ISNULL(m.price, 0)      AS price,
    m.date_from,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) AS days_until_expiry
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id   = c.id
    LEFT JOIN dbo.tb_branch   b ON m.br_code = b.br_code
WHERE m.date_to IS NOT NULL
  AND m.date_to <> ''
  AND ISDATE(m.date_to) = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) = @daysBefore
  AND ISNULL(m.prd_prd, '') IN ('개인사업자', '법인사업자', '스마트데스크')
  AND (m.date_out IS NULL OR m.date_out = ''
       OR (ISDATE(m.date_out) = 1 AND CAST(m.date_out AS DATE) >= CAST(m.date_to AS DATE)))
ORDER BY m.date_to ASC, m.cust;";

        #endregion

        #region 대상 조회

        public List<RenewalInfo> GetTargets(int daysBefore = DefaultDaysBefore)
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
                AppLog.Error($"[갱신자동] 조회 실패 (D-{daysBefore})", ex);
                return new List<RenewalInfo>();
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
            AppLog.Info($"[갱신자동] 대상 {targets.Count}건 (D-{daysBefore})");

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
                    AppLog.Error($"[갱신자동] 오류 {info.MoveInId}", ex);
                }
            }

            AppLog.Info(
                $"[갱신자동] 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}"
            );
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 처리

        public SendResult ProcessOne(RenewalInfo info)
        {
            if (info == null)
                return SendResult.Fail("null");

            if (_logRepo.HasSentByMoveIn(MailTypes.RenewalAuto, info.MoveInId))
                return SendResult.Skip("이미 발송 완료");

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var s = SendResult.Skip("이메일 미등록");
                LogResult(info, s);
                return s;
            }

            var tokens = BuildTokens(info);
            var subject = _template.RenderSubject(
                TemplateEngine.GetDefaultSubject(MailTypes.RenewalAuto),
                tokens
            );
            var body = _template.LoadAndRender(TemplateFiles.RenewalAuto, tokens);

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

        private static Dictionary<string, string> BuildTokens(RenewalInfo info)
        {
            // 회신기한 = 계약종료일 포함 3일전 = date_to - 2일
            string replyDeadline = "";
            if (DateTime.TryParse(info.DateTo, out var dt))
                replyDeadline = dt.AddDays(-2).ToString("yyyy-MM-dd");

            return new Dictionary<string, string>
            {
                ["회사명"] = info.CustName ?? "",
                ["계약종료일"] = info.DateTo ?? "",
                ["회신기한"] = replyDeadline,
                ["계약종료일 포함 3일전"] = replyDeadline,
                ["지점"] = info.BranchName ?? "",
                ["상품/분류"] = info.ProductName ?? "",
                ["호실"] = info.OfficeNum ?? "",
                ["예치금"] = info.Deposit.ToString("#,0") + "원",
                ["임대료"] = info.Price.ToString("#,0") + "원",
                ["납부계좌"] = info.BankAccount ?? "",
                ["예금주"] = info.BankHolder ?? "",
            };
        }

        #endregion

        #region 로그

        private void LogResult(RenewalInfo info, SendResult result)
        {
            try
            {
                _logRepo.Insert(
                    MailLogEntry.Create(
                        MailTypes.RenewalAuto,
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
                AppLog.Error($"[갱신자동] 로그 실패 {info.MoveInId}", ex);
            }
        }

        #endregion

        #region Mapper

        private static RenewalInfo MapRow(SqlDataReader r)
        {
            return new RenewalInfo
            {
                MoveInId = DbManager.GetSafeInt(r, "movein_id"),
                BranchCode = DbManager.GetSafeString(r, "br_code"),
                CustName = DbManager.GetSafeString(r, "cust_name"),
                Email = DbManager.GetSafeString(r, "email"),
                BranchName = DbManager.GetSafeString(r, "branch_name"),
                BankAccount = DbManager.GetSafeString(r, "bank_account"),
                BankHolder = DbManager.GetSafeString(r, "bank_holder"),
                ProductName = DbManager.GetSafeString(r, "product_name"),
                OfficeNum = DbManager.GetSafeString(r, "office_num"),
                Deposit = DbManager.GetSafeInt(r, "deposit"),
                Price = DbManager.GetSafeInt(r, "price"),
                DateFrom = DbManager.GetSafeString(r, "date_from"),
                DateTo = DbManager.GetSafeString(r, "date_to"),
                DateOut = DbManager.GetSafeString(r, "date_out"),
                DaysUntilExpiry = DbManager.GetSafeInt(r, "days_until_expiry"),
            };
        }

        #endregion
    }
}
