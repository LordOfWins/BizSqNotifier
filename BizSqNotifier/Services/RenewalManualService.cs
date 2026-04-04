using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 계약 갱신 (오피스 수동) 서비스.
    /// 오피스 = prd_prd LIKE '%인실' (1인실~6인실)
    /// 33일 전 목록 표시 → 수기 입력 → 수동 발송
    /// </summary>
    public sealed class RenewalManualService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        public RenewalManualService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region SQL

        private const string SelectTargetsSql = @"
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
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), TRY_CAST(m.date_to AS DATE)) AS days_until_expiry
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id   = c.id
    LEFT JOIN dbo.tb_branch   b ON m.br_code = b.br_code
WHERE m.date_to IS NOT NULL
  AND m.date_to <> ''
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), TRY_CAST(m.date_to AS DATE)) BETWEEN 0 AND @maxDays
  AND m.prd_prd LIKE '%인실'
  AND (m.date_out IS NULL OR m.date_out = '' OR TRY_CAST(m.date_out AS DATE) >= TRY_CAST(m.date_to AS DATE))
ORDER BY m.date_to ASC, m.cust;";

        #endregion

        #region 대상 조회

        public List<RenewalInfo> GetRenewalTargets(int maxDays = 33)
        {
            try
            {
                return DbManager.ExecuteReader(SelectTargetsSql, MapRow,
                    new SqlParameter("@maxDays", maxDays));
            }
            catch (Exception ex) { AppLog.Error($"갱신 대상(오피스) 조회 실패 (maxDays={maxDays})", ex); return new List<RenewalInfo>(); }
        }

        #endregion

        #region 수동 발송

        public SendResult SendOne(RenewalInfo info)
        {
            if (info == null) return SendResult.Fail("null");

            if (_logRepo.HasSentManualRenewalToday(info.MoveInId))
                return SendResult.Skip("오늘 이미 발송됨");

            if (string.IsNullOrWhiteSpace(info.Email))
            { var s = SendResult.Skip("이메일 미등록"); LogResult(info, s); return s; }

            var err = ValidateInput(info);
            if (err != null) return SendResult.Fail(err);

            var tokens = BuildTokens(info);
            var subject = _template.RenderSubject(TemplateEngine.GetDefaultSubject(MailTypes.RenewalManual), tokens);
            var body = _template.LoadAndRender(TemplateFiles.RenewalOffice, tokens);

            if (string.IsNullOrEmpty(body))
            { var f = SendResult.Fail("템플릿 로드 실패"); LogResult(info, f); return f; }

            var result = _smtp.SendByBranch(info.BranchCode, info.Email, subject, body);
            LogResult(info, result);
            return result;
        }

        #endregion

        #region 검증

        private static string ValidateInput(RenewalInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.NewDeposit)) return "변경 예치금 미입력";
            if (string.IsNullOrWhiteSpace(info.NewPrice)) return "변경 이용료 미입력";
            if (string.IsNullOrWhiteSpace(info.ReplyDeadline)) return "회신 요청일 미입력";
            return null;
        }

        #endregion

        #region 토큰

        private static Dictionary<string, string> BuildTokens(RenewalInfo info)
        {
            return new Dictionary<string, string>
            {
                ["회사명"]       = info.CustName ?? "",
                ["지점"]         = info.BranchName ?? "",
                ["상품/분류"]    = info.ProductName ?? "",
                ["호실"]         = info.OfficeNum ?? "",
                ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                ["임대료"]       = info.Price.ToString("#,0") + "원",
                ["계약종료일"]   = info.DateTo ?? "",
                ["납부계좌"]     = info.BankAccount ?? "",
                ["예금주"]       = info.BankHolder ?? "",
                ["변경예치금"]   = info.NewDeposit ?? "",
                ["변경이용료"]   = info.NewPrice ?? "",
                ["회신요청일"]   = info.ReplyDeadline ?? "",
                ["참조안내사항"] = info.ReferenceNote ?? ""
            };
        }

        #endregion

        #region 로그

        private void LogResult(RenewalInfo info, SendResult result)
        {
            try
            {
                _logRepo.Insert(MailLogEntry.Create(
                    MailTypes.RenewalManual, info.MoveInId, info.CustName,
                    info.Email, info.BranchCode, result));
            }
            catch (Exception ex) { AppLog.Error($"[갱신수동] 로그 실패 {info.MoveInId}", ex); }
        }

        #endregion

        #region Mapper

        private static RenewalInfo MapRow(SqlDataReader r)
        {
            return new RenewalInfo
            {
                MoveInId        = DbManager.GetSafeInt(r, "movein_id"),
                BranchCode      = DbManager.GetSafeString(r, "br_code"),
                CustName        = DbManager.GetSafeString(r, "cust_name"),
                Email           = DbManager.GetSafeString(r, "email"),
                BranchName      = DbManager.GetSafeString(r, "branch_name"),
                BankAccount     = DbManager.GetSafeString(r, "bank_account"),
                BankHolder      = DbManager.GetSafeString(r, "bank_holder"),
                ProductName     = DbManager.GetSafeString(r, "product_name"),
                OfficeNum       = DbManager.GetSafeString(r, "office_num"),
                Deposit         = DbManager.GetSafeInt(r, "deposit"),
                Price           = DbManager.GetSafeInt(r, "price"),
                DateFrom        = DbManager.GetSafeString(r, "date_from"),
                DateTo          = DbManager.GetSafeString(r, "date_to"),
                DateOut         = DbManager.GetSafeString(r, "date_out"),
                DaysUntilExpiry = DbManager.GetSafeInt(r, "days_until_expiry")
            };
        }

        #endregion
    }
}
