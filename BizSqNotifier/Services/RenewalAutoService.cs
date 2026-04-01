using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 계약 갱신 (adBox/회원제) 자동 발송 서비스.
    ///
    /// 워크플로:
    ///   1) 스케줄러가 매일 09:00(설정 변경 가능)에 ProcessAll() 호출
    ///   2) 계약종료일 8일 전 대상 조회 (adBox/회원제 상품 필터)
    ///   3) 중복 발송 방지 (movein_id 기반 1회성)
    ///   4) 템플릿 치환 + SMTP 발송 + 로그 기록
    ///
    /// 발송 조건 (Step1 확정):
    ///   - adBox 또는 회원제 상품
    ///   - DATEDIFF(DAY, GETDATE(), 계약종료일) = 8
    ///   - 동일 movein_id에 '갱신자동' 로그 없음
    ///
    /// 템플릿 변수:
    ///   {회사명}, {계약종료일}, {회신기한} = 계약종료일 포함 3일전
    ///
    /// [ESTIMATED] 상품 필터 조건:
    ///   prd_prd NOT LIKE '%오피스%' (= 오피스가 아닌 모든 상품 = adBox/회원제)
    ///   → 실제 DB의 prd_prd 값에 따라 조정 필요
    ///   → RenewalManualService가 '%오피스%'를 사용하므로 반대 조건 적용
    /// </summary>
    public sealed class RenewalAutoService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        /// <summary>
        /// 발송 기준일수 기본값 (계약종료일 N일 전).
        /// Step1 확정: 8일 전 (DATEDIFF = 8 시점에 발송).
        /// 관리자 설정에서 변경 가능하도록 파라미터화.
        /// </summary>
        public const int DefaultDaysBefore = 8;

        public RenewalAutoService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region 대상 조회 SQL

        // [ESTIMATED] adBox/회원제 상품 식별 조건:
        //   RenewalManualService에서 '%오피스%'로 오피스를 필터하므로
        //   여기서는 NOT LIKE '%오피스%' 로 나머지 상품(adBox/회원제)을 필터
        //   → 실제 DB prd_prd 값 확인 후 조정 필요
        //
        // [ESTIMATED] 퇴실 예정 건 제외:
        //   date_out이 설정된 건은 갱신 대상이 아님
        //   → (date_out IS NULL OR date_out >= date_to)
        //
        // 조회 기준: DATEDIFF(DAY, GETDATE(), date_to) = @daysBefore
        //   → 정확히 N일 전인 건만 조회 (매일 1회 실행이므로 = 조건)
        private const string SelectRenewalAutoTargetsSql = @"
SELECT
    m.movein_id,
    m.cust                  AS cust_name,
    c.email                 AS email,
    m.br_id                 AS branch_id,
    b.br_name               AS branch_name,
    b.bank_accnt            AS bank_account,
    b.bank_holder           AS bank_holder,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    ISNULL(m.price, 0)      AS price,
    m.date_from,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, GETDATE(), m.date_to) AS days_until_expiry
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE m.date_to IS NOT NULL
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) = @daysBefore
  AND m.prd_prd NOT LIKE @officeFilter
  AND (m.date_out IS NULL OR m.date_out >= m.date_to)
ORDER BY m.date_to ASC, m.cust;";

        #endregion

        #region 발송 대상 조회

        /// <summary>
        /// 계약종료일 N일 전인 adBox/회원제 건을 조회합니다.
        /// </summary>
        /// <param name="daysBefore">계약종료일 기준 일수 (기본 8)</param>
        /// <param name="officeFilter">오피스 상품 필터 (제외 대상, 기본 '%오피스%')</param>
        /// <returns>갱신 대상 목록</returns>
        public List<RenewalInfo> GetTargets(int daysBefore = DefaultDaysBefore, string officeFilter = "%오피스%")
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectRenewalAutoTargetsSql,
                    MapRow,
                    new SqlParameter("@daysBefore", daysBefore),
                    new SqlParameter("@officeFilter", officeFilter));
            }
            catch (Exception ex)
            {
                AppLog.Error($"[갱신자동] 대상 조회 실패 (daysBefore={daysBefore})", ex);
                return new List<RenewalInfo>();
            }
        }

        #endregion

        #region 자동 발송 (전체 처리) — 스케줄러에서 호출

        /// <summary>
        /// 갱신 대상 전체에 대해 자동으로 메일을 발송합니다.
        /// SchedulerService에서 09:00에 호출됩니다.
        /// </summary>
        /// <param name="daysBefore">기준 일수 (기본 8)</param>
        /// <returns>(전체건수, 성공건수, 실패건수, SKIP건수)</returns>
        public (int Total, int Success, int Fail, int Skip) ProcessAll(int daysBefore = DefaultDaysBefore)
        {
            var targets = GetTargets(daysBefore);
            int success = 0, fail = 0, skip = 0;

            AppLog.Info($"[갱신자동] 대상 {targets.Count}건 조회됨 (D-{daysBefore})");

            foreach (var info in targets)
            {
                try
                {
                    var result = ProcessOne(info);

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
                    AppLog.Error($"[갱신자동] 개별 처리 오류 — MoveInId={info.MoveInId}", ex);
                }
            }

            AppLog.Info($"[갱신자동] 처리 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}");
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 발송

        /// <summary>
        /// 단일 갱신 건에 대해 중복 체크 → 템플릿 치환 → 발송 → 로그 기록을 수행합니다.
        /// </summary>
        public SendResult ProcessOne(RenewalInfo info)
        {
            if (info == null)
                return SendResult.Fail("RenewalInfo가 null입니다.");

            // ① 중복 발송 방지 (movein_id 기반 1회성)
            if (_logRepo.HasSentByMoveIn(MailTypes.RenewalAuto, info.MoveInId))
            {
                AppLog.Info($"[갱신자동] 이미 발송됨 — MoveInId={info.MoveInId}, {info.CustName}");
                return SendResult.Skip("이미 발송 완료");
            }

            // ② 이메일 미등록 체크
            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var skipResult = SendResult.Skip("이메일 미등록");
                LogResult(info, skipResult);
                return skipResult;
            }

            // ③ 치환 토큰 구성
            var tokens = BuildTokens(info);

            // ④ 제목 치환
            var subjectTemplate = TemplateEngine.GetDefaultSubject(MailTypes.RenewalAuto);
            var subject = _template.RenderSubject(subjectTemplate, tokens);

            // ⑤ 본문 템플릿 로드 + 치환
            var body = _template.LoadAndRender(TemplateFiles.RenewalAuto, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + TemplateFiles.RenewalAuto);
                LogResult(info, failResult);
                return failResult;
            }

            // ⑥ SMTP 발송 (지점별 SMTP 설정 활용)
            var sendResult = _smtp.SendByBranch(
                info.BranchId ?? 0,
                info.Email,
                subject,
                body);

            // ⑦ 로그 기록
            LogResult(info, sendResult);

            return sendResult;
        }

        #endregion

        #region 치환 토큰 구성

        /// <summary>
        /// RenewalInfo의 필드를 RenewalAuto.html 템플릿 치환용 Dictionary로 변환합니다.
        ///
        /// 템플릿 변수 매핑:
        ///   {회사명}       → info.CustName
        ///   {계약종료일}   → info.DateTo (yyyy-MM-dd)
        ///   {회신기한}     → 계약종료일 포함 3일전 = DATEADD(DAY, -2, date_to)
        ///
        /// Step1 확정: {계약종료일 포함 3일전} = 계약종료일 - 2일
        ///   예) 계약종료일 4/10 → 회신기한 4/8 (4/8, 4/9, 4/10 = 3일)
        /// </summary>
        private static Dictionary<string, string> BuildTokens(RenewalInfo info)
        {
            // 회신기한 계산: 계약종료일 포함 3일전 = date_to - 2일
            var replyDeadline = info.DateTo?.AddDays(-2);

            return new Dictionary<string, string>
            {
                // DB 기반 — RenewalAuto.html에서 사용하는 변수
                ["회사명"]       = info.CustName ?? string.Empty,
                ["계약종료일"]   = info.DateTo?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["회신기한"]     = replyDeadline?.ToString("yyyy-MM-dd") ?? string.Empty,

                // RenewalAuto.html에 직접 사용하진 않지만
                // 향후 템플릿 확장 시 활용 가능한 추가 변수
                ["지점"]         = info.BranchName ?? string.Empty,
                ["상품/분류"]    = info.ProductName ?? string.Empty,
                ["호실"]         = info.OfficeNum ?? string.Empty,
                ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                ["임대료"]       = info.Price.ToString("#,0") + "원",
                ["납부계좌"]     = info.BankAccount ?? string.Empty,
                ["예금주"]       = info.BankHolder ?? string.Empty,

                // 계약종료일 포함 3일전 (별칭 — 원문 템플릿 매칭용)
                ["계약종료일 포함 3일전"] = replyDeadline?.ToString("yyyy-MM-dd") ?? string.Empty
            };
        }

        #endregion

        #region 로그 기록

        /// <summary>
        /// 발송 결과를 tb_mail_log에 기록합니다.
        /// </summary>
        private void LogResult(RenewalInfo info, SendResult result)
        {
            try
            {
                var entry = MailLogEntry.Create(
                    mailType: MailTypes.RenewalAuto,
                    moveInId: info.MoveInId,
                    custName: info.CustName,
                    email: info.Email,
                    branchId: info.BranchId,
                    result: result);

                _logRepo.Insert(entry);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[갱신자동] 로그 기록 실패 — MoveInId={info.MoveInId}", ex);
            }
        }

        #endregion

        #region Row Mapper

        /// <summary>SqlDataReader → RenewalInfo 매핑 (RenewalManualService와 동일 구조).</summary>
        private static RenewalInfo MapRow(SqlDataReader reader)
        {
            return new RenewalInfo
            {
                MoveInId        = DbManager.GetSafeInt(reader, "movein_id"),
                CustName        = DbManager.GetSafeString(reader, "cust_name"),
                Email           = DbManager.GetSafeString(reader, "email"),
                BranchId        = DbManager.GetSafeNullableInt(reader, "branch_id"),
                BranchName      = DbManager.GetSafeString(reader, "branch_name"),
                BankAccount     = DbManager.GetSafeString(reader, "bank_account"),
                BankHolder      = DbManager.GetSafeString(reader, "bank_holder"),
                ProductName     = DbManager.GetSafeString(reader, "product_name"),
                OfficeNum       = DbManager.GetSafeString(reader, "office_num"),
                Deposit         = DbManager.GetSafeDecimal(reader, "deposit"),
                Price           = DbManager.GetSafeDecimal(reader, "price"),
                DateFrom        = DbManager.GetSafeDateTime(reader, "date_from"),
                DateTo          = DbManager.GetSafeDateTime(reader, "date_to"),
                DateOut         = DbManager.GetSafeDateTime(reader, "date_out"),
                DaysUntilExpiry = DbManager.GetSafeInt(reader, "days_until_expiry")
            };
        }

        #endregion
    }
}
