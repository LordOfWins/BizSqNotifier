using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 미납 안내 메일 자동 발송 서비스 (1차 / 2차 / 최종).
    ///
    /// 발송 조건 (확정):
    ///   ① 1차: 청구서 발송완료 AND 미납 AND 납부일+3일 경과 AND 1차 로그 없음
    ///   ② 2차: 청구서 발송완료 AND 미납 AND 납부일+10일 경과 AND 2차 로그 없음
    ///   ③ 최종: 청구서 발송완료 AND 미납 AND 납부일+15일 경과 AND 최종 로그 없음
    ///
    /// 납부기한 치환값 (확정):
    ///   1차: 납부일+7일 / 2차: 납부일+13일 / 최종: 납부일+18일
    ///
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

        // ──────────────────────────────────────────────────────────
        // [ESTIMATED] 추정 사항 — 실제 DB 확인 필요:
        //
        //   1) tb_invoice.status 컬럼: 청구서 발송완료 상태값
        //      → 추정: status = '발송' 또는 '완료' 또는 숫자 코드
        //      → 아래 SQL에서 i.chk_send = 1 로 추정 (발송완료 플래그)
        //
        //   2) 미납 판별: 수납 테이블(tb_receipt 등)에 해당 invoice의
        //      수납 기록이 없는 것으로 판별
        //      → 추정: i.chk_paid = 0 또는 i.date_paid IS NULL
        //      → 아래 SQL에서 i.chk_paid = 0 으로 추정
        //
        //   3) tb_inv_list JOIN: invoice_id 기준
        //      → 합계금액 = SUM(il.price + il.tax)
        //
        //   4) tb_invoice → tb_movein JOIN: i.movein_id = m.movein_id (추정)
        // ──────────────────────────────────────────────────────────

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

        #region 전체 자동 발송 (스케줄러 13:00 호출)

        /// <summary>
        /// 미납 1차/2차/최종 전체를 순차 처리합니다.
        /// 스케줄러에서 13:00에 이 메서드를 호출합니다.
        /// </summary>
        /// <returns>단계별 처리 결과 딕셔너리</returns>
        public Dictionary<string, (int Total, int Success, int Fail, int Skip)> ProcessAll()
        {
            var results = new Dictionary<string, (int, int, int, int)>();

            // 최종 → 2차 → 1차 순서가 아니라 1차 → 2차 → 최종 순서로 처리
            // (각 단계별 중복 체크가 invoice_id + mail_type 기반이므로 순서 무관)
            // 하지만 같은 invoice에 대해 1차 미발송 상태에서 15일 경과 시
            // 1차/2차/최종 모두 조건에 걸릴 수 있음
            // → 각 단계에서 이전 단계 로그 존재 여부는 체크하지 않음 (확정 스펙)
            // → 각 단계별 독립 체크: 해당 mail_type + invoice_id 로그만 확인

            results[MailTypes.Unpaid1st] = ProcessStage(
                mailType: MailTypes.Unpaid1st,
                templateFile: TemplateFiles.Unpaid1st,
                minDays: 3,
                deadlineOffset: 7);

            results[MailTypes.Unpaid2nd] = ProcessStage(
                mailType: MailTypes.Unpaid2nd,
                templateFile: TemplateFiles.Unpaid2nd,
                minDays: 10,
                deadlineOffset: 13);

            results[MailTypes.UnpaidFinal] = ProcessStage(
                mailType: MailTypes.UnpaidFinal,
                templateFile: TemplateFiles.UnpaidFinal,
                minDays: 15,
                deadlineOffset: 18);

            return results;
        }

        #endregion

        #region 단계별 처리

        /// <summary>
        /// 미납 안내 특정 단계를 처리합니다.
        /// </summary>
        /// <param name="mailType">메일 유형 (미납1차/미납2차/미납최종)</param>
        /// <param name="templateFile">HTML 템플릿 파일명</param>
        /// <param name="minDays">발송 조건 최소 경과일</param>
        /// <param name="deadlineOffset">납부기한 = 납부일 + N일</param>
        private (int Total, int Success, int Fail, int Skip) ProcessStage(
            string mailType,
            string templateFile,
            int minDays,
            int deadlineOffset)
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

        /// <summary>
        /// 단일 미납 건에 대해 중복 체크 → 템플릿 치환 → 발송 → 로그 기록을 수행합니다.
        /// </summary>
        public SendResult ProcessOne(
            UnpaidInfo info,
            string mailType,
            string templateFile,
            int deadlineOffset)
        {
            if (info == null)
                return SendResult.Fail("UnpaidInfo가 null입니다.");

            // ① 중복 발송 방지 체크 (invoice_id 기반)
            if (_logRepo.HasSentByInvoice(mailType, info.InvoiceId))
            {
                AppLog.Info($"[{mailType}] 이미 발송됨 — InvoiceId={info.InvoiceId}, {info.CustName}");
                return SendResult.Skip("이미 발송 완료");
            }

            // ② 이메일 미등록 체크
            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var skipResult = SendResult.Skip("이메일 미등록");
                LogResult(info, mailType, skipResult);
                return skipResult;
            }

            // ③ 치환 토큰 구성
            var tokens = BuildTokens(info, deadlineOffset);

            // ④ 제목 치환
            var subjectTemplate = TemplateEngine.GetDefaultSubject(mailType);
            var subject = _template.RenderSubject(subjectTemplate, tokens);

            // ⑤ 본문 템플릿 로드 + 치환
            var body = _template.LoadAndRender(templateFile, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + templateFile);
                LogResult(info, mailType, failResult);
                return failResult;
            }

            // ⑥ SMTP 발송
            var sendResult = _smtp.SendByBranch(
                info.BranchId ?? 0,
                info.Email,
                subject,
                body);

            // ⑦ 로그 기록
            LogResult(info, mailType, sendResult);

            return sendResult;
        }

        #endregion

        #region 대상 조회

        /// <summary>
        /// 미납 대상 목록을 DB에서 조회합니다.
        /// </summary>
        /// <param name="minDays">최소 경과일 필터</param>
        public List<UnpaidInfo> GetUnpaidTargets(int minDays)
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectUnpaidSql,
                    MapRow,
                    new SqlParameter("@minDays", minDays));
            }
            catch (Exception ex)
            {
                AppLog.Error($"미납 대상 조회 실패 (minDays={minDays})", ex);
                return new List<UnpaidInfo>();
            }
        }

        #endregion

        #region 치환 토큰 구성

        /// <summary>
        /// UnpaidInfo의 필드를 템플릿 치환용 Dictionary로 변환합니다.
        /// </summary>
        /// <param name="info">미납 정보</param>
        /// <param name="deadlineOffset">납부기한 오프셋 (납부일 + N일)</param>
        private static Dictionary<string, string> BuildTokens(
            UnpaidInfo info,
            int deadlineOffset)
        {
            // 납부기한 = 납부일 + deadlineOffset일
            var deadline = info.DatePay.AddDays(deadlineOffset);

            return new Dictionary<string, string>
            {
                ["회사명"]     = info.CustName ?? string.Empty,
                ["지점"]       = info.BranchName ?? string.Empty,
                ["상품/분류"]  = info.ProductName ?? string.Empty,
                ["호실"]       = info.OfficeNum ?? string.Empty,
                ["합계금액"]   = info.TotalAmount.ToString("#,0") + "원",
                ["납부기한"]   = deadline.ToString("yyyy-MM-dd"),
                ["납부계좌"]   = info.BankAccount ?? string.Empty,
                ["예금주"]     = info.BankHolder ?? string.Empty
            };
        }

        #endregion

        #region 로그 기록

        /// <summary>
        /// 발송 결과를 tb_mail_log에 기록합니다.
        /// </summary>
        private void LogResult(UnpaidInfo info, string mailType, SendResult result)
        {
            try
            {
                var entry = MailLogEntry.Create(
                    mailType: mailType,
                    moveInId: info.MoveInId,
                    custName: info.CustName,
                    email: info.Email,
                    branchId: info.BranchId,
                    result: result,
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

        /// <summary>SqlDataReader → UnpaidInfo 매핑.</summary>
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
