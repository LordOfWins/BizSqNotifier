using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 미납 안내 메일 자동 발송 서비스 (1차/2차/최종).
    ///
    /// 실제 DB 컬럼 확정:
    ///    tb_invoice.send_yn (varchar) = 발송완료 여부 ('Y' = 발송됨)
    ///   tb_invoice.dep_yn (int)      = 입금 여부 (0=미납, 1=납부)
    ///   tb_invoice.date_pay (varchar) = 납부일 'yyyy-MM-dd'
    ///   tb_invoice.mi_id             = tb_movein.id FK
    ///   tb_inv_list.iv_id            = tb_invoice.id FK
    ///
    /// 발송 시각: 13:00 KST
    /// </summary>
    public sealed class UnpaidService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        public UnpaidService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region SQL

        private const string SelectUnpaidSql =
            @"
SELECT
    i.mi_id                      AS movein_id,
    i.id                         AS invoice_id,
    i.br_code,
    i.cust                       AS cust_name,
    c.email,
    b.br_name                    AS branch_name,
    b.bank_accnt                 AS bank_account,
    b.bank_holder,
    i.prd_prd                    AS product_name,
    i.off_num                    AS office_num,
    ISNULL(inv_sum.total_amt, 0) AS total_amount,
    i.date_pay,
    DATEDIFF(DAY, CAST(i.date_pay AS DATE), CAST(GETDATE() AS DATE)) AS days_overdue
FROM dbo.tb_invoice i
    LEFT JOIN dbo.tb_customer c  ON i.cu_id   = c.id
    LEFT JOIN dbo.tb_branch   b  ON i.br_code = b.br_code
    LEFT JOIN dbo.tb_movein   m  ON i.mi_id   = m.id
    LEFT JOIN (
        SELECT iv_id,
               SUM(ISNULL(price, 0) + ISNULL(tax, 0)) AS total_amt
        FROM dbo.tb_inv_list
        GROUP BY iv_id
    ) inv_sum ON i.id = inv_sum.iv_id
WHERE i.send_yn = 'Y'
  AND i.dep_yn  = 0
  AND i.date_pay IS NOT NULL
  AND i.date_pay <> ''
  AND ISDATE(i.date_pay) = 1
  AND CAST(i.date_pay AS DATE) < CAST(GETDATE() AS DATE)
  AND DATEDIFF(DAY, CAST(i.date_pay AS DATE), CAST(GETDATE() AS DATE)) >= @minDays
  AND DATEDIFF(DAY, CAST(i.date_pay AS DATE), CAST(GETDATE() AS DATE)) <= 90
  AND (m.date_out IS NULL OR m.date_out = ''
       OR (ISDATE(m.date_out) = 1 AND CAST(m.date_out AS DATE) >= CAST(GETDATE() AS DATE)))
  AND ISNULL(m.prd_prd, '') <> '모아즈'
ORDER BY i.mi_id, i.id;
ORDER BY i.mi_id, i.id;";

        #endregion

        #region 전체 처리 (UserSettings 기준일 반영)

        /// <summary>미납 1차/2차/최종 전체를 UserSettings 기준일로 처리.</summary>
        public Dictionary<string, (int Total, int Success, int Fail, int Skip)> ProcessAll()
        {
            var s = UserSettings.Current;
            return ProcessAll(s.Unpaid1stDays, s.Unpaid2ndDays, s.UnpaidFinalDays);
        }

        /// <summary>기준일 명시 오버로드.</summary>
        public Dictionary<string, (int Total, int Success, int Fail, int Skip)> ProcessAll(
            int days1st,
            int days2nd,
            int daysFinal
        )
        {
            var results = new Dictionary<string, (int, int, int, int)>();

            results[MailTypes.Unpaid1st] = ProcessStage(
                MailTypes.Unpaid1st,
                TemplateFiles.Unpaid1st,
                days1st,
                days1st + 4
            );

            results[MailTypes.Unpaid2nd] = ProcessStage(
                MailTypes.Unpaid2nd,
                TemplateFiles.Unpaid2nd,
                days2nd,
                days2nd + 3
            );

            results[MailTypes.UnpaidFinal] = ProcessStage(
                MailTypes.UnpaidFinal,
                TemplateFiles.UnpaidFinal,
                daysFinal,
                daysFinal + 3
            );

            return results;
        }

        #endregion

        #region 단계별 처리

        private (int Total, int Success, int Fail, int Skip) ProcessStage(
            string mailType,
            string templateFile,
            int minDays,
            int deadlineOffset
        )
        {
            var targets = GetUnpaidTargets(minDays);
            int success = 0,
                fail = 0,
                skip = 0;
            AppLog.Info($"[{mailType}] 대상 {targets.Count}건 (>={minDays}일)");

            foreach (var info in targets)
            {
                try
                {
                    var r = ProcessOne(info, mailType, templateFile, deadlineOffset);
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
                    AppLog.Error($"[{mailType}] 오류 InvoiceId={info.InvoiceId}", ex);
                }
            }

            AppLog.Info(
                $"[{mailType}] 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}"
            );
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 처리

        public SendResult ProcessOne(
            UnpaidInfo info,
            string mailType,
            string templateFile,
            int deadlineOffset
        )
        {
            if (info == null)
                return SendResult.Fail("UnpaidInfo null");

            if (_logRepo.HasSentByInvoice(mailType, info.InvoiceId))
                return SendResult.Skip("이미 발송 완료");

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var s = SendResult.Skip("이메일 미등록");
                LogResult(info, mailType, s);
                return s;
            }

            var tokens = BuildTokens(info, deadlineOffset);
            var subject = _template.RenderSubject(
                TemplateEngine.GetDefaultSubject(mailType),
                tokens
            );
            var body = _template.LoadAndRender(templateFile, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var f = SendResult.Fail("템플릿 로드 실패");
                LogResult(info, mailType, f);
                return f;
            }

            var result = _smtp.SendByBranch(info.BranchCode, info.Email, subject, body);
            LogResult(info, mailType, result);
            return result;
        }

        #endregion

        #region 대상 조회

        public List<UnpaidInfo> GetUnpaidTargets(int minDays)
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectUnpaidSql,
                    MapRow,
                    new SqlParameter("@minDays", minDays)
                );
            }
            catch (Exception ex)
            {
                AppLog.Error($"미납 대상 조회 실패 (minDays={minDays})", ex);
                return new List<UnpaidInfo>();
            }
        }

        #endregion

        #region 토큰

        private static Dictionary<string, string> BuildTokens(UnpaidInfo info, int deadlineOffset)
        {
            // 납부기한 = date_pay + deadlineOffset일
            string deadline = "";
            if (DateTime.TryParse(info.DatePay, out var dp))
                deadline = dp.AddDays(deadlineOffset).ToString("yyyy-MM-dd");

            return new Dictionary<string, string>
            {
                ["회사명"] = info.CustName ?? "",
                ["지점"] = info.BranchName ?? "",
                ["상품/분류"] = info.ProductName ?? "",
                ["호실"] = info.OfficeNum ?? "",
                ["합계금액"] = info.TotalAmount.ToString("#,0") + "원",
                ["납부기한"] = deadline,
                ["납부계좌"] = info.BankAccount ?? "",
                ["예금주"] = info.BankHolder ?? "",
            };
        }

        #endregion

        #region 로그

        private void LogResult(UnpaidInfo info, string mailType, SendResult result)
        {
            try
            {
                _logRepo.Insert(
                    MailLogEntry.Create(
                        mailType,
                        info.MoveInId,
                        info.CustName,
                        info.Email,
                        info.BranchCode,
                        result,
                        info.InvoiceId
                    )
                );
            }
            catch (Exception ex)
            {
                AppLog.Error($"[{mailType}] 로그 실패 InvoiceId={info.InvoiceId}", ex);
            }
        }

        #endregion

        #region Mapper

        private static UnpaidInfo MapRow(SqlDataReader r)
        {
            return new UnpaidInfo
            {
                MoveInId = DbManager.GetSafeInt(r, "movein_id"),
                InvoiceId = DbManager.GetSafeInt(r, "invoice_id"),
                BranchCode = DbManager.GetSafeString(r, "br_code"),
                CustName = DbManager.GetSafeString(r, "cust_name"),
                Email = DbManager.GetSafeString(r, "email"),
                BranchName = DbManager.GetSafeString(r, "branch_name"),
                BankAccount = DbManager.GetSafeString(r, "bank_account"),
                BankHolder = DbManager.GetSafeString(r, "bank_holder"),
                ProductName = DbManager.GetSafeString(r, "product_name"),
                OfficeNum = DbManager.GetSafeString(r, "office_num"),
                TotalAmount = DbManager.GetSafeInt(r, "total_amount"),
                DatePay = DbManager.GetSafeString(r, "date_pay"),
                DaysOverdue = DbManager.GetSafeInt(r, "days_overdue"),
            };
        }

        #endregion
    }
}
