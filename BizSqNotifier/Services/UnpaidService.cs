using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 미납 안내 메일 자동 발송 서비스 (1차 / 2차 / 최종).
    /// 발송 시각: 13:00 KST (스케줄러에서 호출)
    /// 3차(최종) 이후 중단 — 이후 내용증명은 오프라인 처리
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

        #region 미납 대상 조회 SQL

        // [ESTIMATED] tb_invoice.chk_send / chk_paid 컬럼명 추정
        // 실제 DB 확인 후 조정 필요

        private const string SelectUnpaidSql = @"
SELECT
    m.movein_id,
    i.invoice_id,
    m.cust                      AS cust_name,
    c.email                     AS email,
    m.br_id                     AS branch_id,
    b.br_name                   AS branch_name,
    b.bank_accnt                AS bank_account,
    b.bank_holder               AS bank_holder,
    m.prd_prd                   AS product_name,
    m.off_num                   AS office_num,
    ISNULL(inv_sum.total_amt, 0) AS total_amount,
    i.date_pay,
    DATEDIFF(DAY, i.date_pay, GETDATE()) AS days_overdue
FROM dbo.tb_invoice i
    INNER JOIN dbo.tb_movein    m ON i.movein_id = m.movein_id
    LEFT  JOIN dbo.tb_customer  c ON m.cust_id   = c.cust_id
    LEFT  JOIN dbo.tb_branch    b ON m.br_id     = b.br_id
    LEFT  JOIN (
        SELECT invoice_id,
               SUM(ISNULL(price, 0) + ISNULL(tax, 0)) AS total_amt
        FROM dbo.tb_inv_list
        GROUP BY invoice_id
    ) inv_sum ON i.invoice_id = inv_sum.invoice_id
WHERE i.chk_send = 1
  AND i.chk_paid = 0
  AND DATEDIFF(DAY, i.date_pay, GETDATE()) >= @minDays
ORDER BY m.movein_id, i.invoice_id;";

        #endregion

        #region 전체 자동 발송

        /// <summary>
        /// UserSettings 기준일로 미납 1차/2차/최종 전체 처리.
        /// 스케줄러에서 13:00에 호출.
        /// </summary>
        public Dictionary<string, (int Total, int Success, int Fail, int Skip)> ProcessAll()
        {
            var s = UserSettings.Current;
            return ProcessAll(s.Unpaid1stDays, s.Unpaid2ndDays, s.UnpaidFinalDays);
        }

        /// <summary>
        /// 지정된 기준일로 미납 1차/2차/최종 전체 처리.
        /// </summary>
        public Dictionary<string, (int Total, int Success, int Fail, int Skip)> ProcessAll(
            int days1st, int days2nd, int daysFinal)
        {
            var results = new Dictionary<string, (int, int, int, int)>();

            results[MailTypes.Unpaid1st] = ProcessStage(
                mailType: MailTypes.Unpaid1st,
                templateFile: TemplateFiles.Unpaid1st,
                minDays: days1st,
                deadlineOffset: days1st + 4);

            results[MailTypes.Unpaid2nd] = ProcessStage(
                mailType: MailTypes.Unpaid2nd,
                templateFile: TemplateFiles.Unpaid2nd,
                minDays: days2nd,
                deadlineOffset: days2nd + 3);

            results[MailTypes.UnpaidFinal] = ProcessStage(
                mailType: MailTypes.UnpaidFinal,
                templateFile: TemplateFiles.UnpaidFinal,
                minDays: daysFinal,
                deadlineOffset: daysFinal + 3);

            return results;
        }

        #endregion

        #region 단계별 처리

        private (int Total, int Success, int Fail, int Skip) ProcessStage(
            string mailType, string templateFile, int minDays, int deadlineOffset)
        {
            var targets = GetUnpaidTargets(minDays);
            int success = 0, fail = 0, skip = 0;

            AppLog.Info($"[{mailType}] 대상 {targets.Count}건 조회됨 (경과일>={minDays})");

            foreach (var info in targets)
            {
                try
                {
                    var result = ProcessOne(info, mailType, templateFile, deadlineOffset);
                    switch (result.Status)
                    {
                        case "성공": success++; break;
                        case "SKIP": skip++; break;
                        default: fail++; break;
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    AppLog.Error($"[{mailType}] 개별 처리 오류 — InvoiceId={info.InvoiceId}", ex);
                }
            }

            AppLog.Info($"[{mailType}] 처리 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}");
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 발송

        public SendResult ProcessOne(
            UnpaidInfo info, string mailType, string templateFile, int deadlineOffset)
        {
            if (info == null) return SendResult.Fail("UnpaidInfo가 null입니다.");

            if (_logRepo.HasSentByInvoice(mailType, info.InvoiceId))
            {
                AppLog.Info($"[{mailType}] 이미 발송됨 — InvoiceId={info.InvoiceId}, {info.CustName}");
                return SendResult.Skip("이미 발송 완료");
            }

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var skipResult = SendResult.Skip("이메일 미등록");
                LogResult(info, mailType, skipResult);
                return skipResult;
            }

            var tokens = BuildTokens(info, deadlineOffset);
            var subjectTemplate = TemplateEngine.GetDefaultSubject(mailType);
            var subject = _template.RenderSubject(subjectTemplate, tokens);
            var body = _template.LoadAndRender(templateFile, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + templateFile);
                LogResult(info, mailType, failResult);
                return failResult;
            }

            var sendResult = _smtp.SendByBranch(info.BranchId ?? 0, info.Email, subject, body);
            LogResult(info, mailType, sendResult);
            return sendResult;
        }

        #endregion

        #region 대상 조회

        public List<UnpaidInfo> GetUnpaidTargets(int minDays)
        {
            try
            {
                return DbManager.ExecuteReader(SelectUnpaidSql, MapRow,
                    new SqlParameter("@minDays", minDays));
            }
            catch (Exception ex)
            {
                AppLog.Error($"미납 대상 조회 실패 (minDays={minDays})", ex);
                return new List<UnpaidInfo>();
            }
        }

        #endregion

        #region 치환 토큰

        private static Dictionary<string, string> BuildTokens(UnpaidInfo info, int deadlineOffset)
        {
            var deadline = info.DatePay.AddDays(deadlineOffset);
            return new Dictionary<string, string>
            {
                ["회사명"]       = info.CustName ?? string.Empty,
                ["지점"]         = info.BranchName ?? string.Empty,
                ["상품/분류"]    = info.ProductName ?? string.Empty,
                ["호실"]         = info.OfficeNum ?? string.Empty,
                ["합계금액"]     = info.TotalAmount.ToString("#,0") + "원",
                ["납부기한"]     = deadline.ToString("yyyy-MM-dd"),
                ["최종납부기한"] = deadline.ToString("yyyy-MM-dd"),
                ["납부계좌"]     = info.BankAccount ?? string.Empty,
                ["예금주"]       = info.BankHolder ?? string.Empty
            };
        }

        #endregion

        #region 로그 기록

        private void LogResult(UnpaidInfo info, string mailType, SendResult result)
        {
            try
            {
                var entry = MailLogEntry.Create(
                    mailType: mailType, moveInId: info.MoveInId,
                    custName: info.CustName, email: info.Email,
                    branchId: info.BranchId, result: result,
                    invoiceId: info.InvoiceId);
                _logRepo.Insert(entry);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[{mailType}] 로그 기록 실패 — InvoiceId={info.InvoiceId}", ex);
            }
        }

        #endregion

        #region Row Mapper

        private static UnpaidInfo MapRow(SqlDataReader reader)
        {
            return new UnpaidInfo
            {
                MoveInId    = DbManager.GetSafeInt(reader, "movein_id"),
                InvoiceId   = DbManager.GetSafeInt(reader, "invoice_id"),
                CustName    = DbManager.GetSafeString(reader, "cust_name"),
                Email       = DbManager.GetSafeString(reader, "email"),
                BranchId    = DbManager.GetSafeNullableInt(reader, "branch_id"),
                BranchName  = DbManager.GetSafeString(reader, "branch_name"),
                BankAccount = DbManager.GetSafeString(reader, "bank_account"),
                BankHolder  = DbManager.GetSafeString(reader, "bank_holder"),
                ProductName = DbManager.GetSafeString(reader, "product_name"),
                OfficeNum   = DbManager.GetSafeString(reader, "office_num"),
                TotalAmount = DbManager.GetSafeDecimal(reader, "total_amount"),
                DatePay     = DbManager.GetSafeDateTimeValue(reader, "date_pay", DateTime.Today),
                DaysOverdue = DbManager.GetSafeInt(reader, "days_overdue")
            };
        }

        #endregion
    }
}
