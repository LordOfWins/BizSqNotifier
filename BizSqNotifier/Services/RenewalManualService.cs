using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 계약 갱신 (오피스 수동) 서비스.
    ///
    /// 워크플로:
    ///   1) 33일 전 갱신 대상 목록 조회 → RenewalManualForm DataGridView에 표시
    ///   2) 담당자가 특정 건 선택 → 수기 입력 (변경예치금/변경이용료/회신요청일/참조안내사항)
    ///   3) 수동 발송 버튼 클릭 → 템플릿 치환 + SMTP 발송 + 로그 기록
    ///
    /// 발송 조건:
    ///   - 오피스 상품 (prd_prd 기준 필터)
    ///   - 계약종료일 33일 전 기준 목록 표시
    ///   - 같은 날 동일 movein_id 중복 발송 방지 (조건 변경 후 재발송 허용)
    ///
    /// [ESTIMATED] prd_prd에서 '오피스' 상품 식별 조건:
    ///   → 추정: prd_prd LIKE '%오피스%' 또는 특정 코드값
    ///   → 아래 SQL에서 prd_prd LIKE '%오피스%' 로 추정
    ///   → 실제 DB의 prd_prd 값 확인 후 조정 필요
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

        #region 33일 전 갱신 대상 조회 SQL

        // [ESTIMATED] 오피스 상품 필터 조건
        //   prd_prd LIKE '%오피스%' 로 추정
        //   실제 값이 코드(예: 'OFF', 'OFFICE') 또는 한글('오피스')인지 확인 필요
        //
        // [ESTIMATED] 퇴실 예정(date_out) 이 설정된 건은 갱신 대상에서 제외
        //   → date_out IS NULL 또는 date_out > date_to
        //
        // [ESTIMATED] tb_movein.status 컬럼: 계약 상태 (입주중/퇴실 등)
        //   → 활성 계약만 조회: status가 '입주' 또는 활성 상태 코드
        //   → 아래에서는 date_out IS NULL (퇴실 미예정) 조건으로 대체
        private const string SelectRenewalTargetsSql = @"
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
  AND DATEDIFF(DAY, GETDATE(), m.date_to) BETWEEN 0 AND @maxDays
  AND m.prd_prd LIKE @productFilter
  AND (m.date_out IS NULL OR m.date_out >= m.date_to)
ORDER BY m.date_to ASC, m.cust;";

        #endregion

        #region 대상 목록 조회 (Form에서 호출)

        /// <summary>
        /// 오피스 상품의 갱신 대상 목록을 조회합니다.
        /// 기본: 계약종료일 33일 이내.
        /// </summary>
        /// <param name="maxDays">기준 일수 (기본 33)</param>
        /// <param name="productFilter">상품 필터 (기본 '%오피스%')</param>
        /// <returns>갱신 대상 목록</returns>
        public List<RenewalInfo> GetRenewalTargets(int maxDays = 33, string productFilter = "%오피스%")
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectRenewalTargetsSql,
                    MapRow,
                    new SqlParameter("@maxDays", maxDays),
                    new SqlParameter("@productFilter", productFilter));
            }
            catch (Exception ex)
            {
                AppLog.Error($"갱신 대상(오피스) 조회 실패 (maxDays={maxDays})", ex);
                return new List<RenewalInfo>();
            }
        }

        #endregion

        #region 수동 발송 (단일 건)

        /// <summary>
        /// 수기 입력 완료된 단일 건에 대해 메일을 발송합니다.
        /// </summary>
        /// <param name="info">DB 조회 데이터 + 수기 입력 데이터가 채워진 RenewalInfo</param>
        /// <returns>발송 결과</returns>
        public SendResult SendOne(RenewalInfo info)
        {
            if (info == null)
                return SendResult.Fail("RenewalInfo가 null입니다.");

            // ① 당일 중복 발송 방지 체크
            if (_logRepo.HasSentManualRenewalToday(info.MoveInId))
            {
                AppLog.Info($"[갱신수동] 오늘 이미 발송됨 — MoveInId={info.MoveInId}, {info.CustName}");
                return SendResult.Skip("오늘 이미 발송 완료 (재발송은 내일 가능)");
            }

            // ② 이메일 미등록 체크
            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var skipResult = SendResult.Skip("이메일 미등록");
                LogResult(info, skipResult);
                return skipResult;
            }

            // ③ 수기 입력 필수값 검증
            var validationError = ValidateManualInput(info);
            if (validationError != null)
            {
                return SendResult.Fail(validationError);
            }

            // ④ 치환 토큰 구성
            var tokens = BuildTokens(info);

            // ⑤ 제목 치환
            var subjectTemplate = TemplateEngine.GetDefaultSubject(MailTypes.RenewalManual);
            var subject = _template.RenderSubject(subjectTemplate, tokens);

            // ⑥ 본문 템플릿 로드 + 치환
            var body = _template.LoadAndRender(TemplateFiles.RenewalOffice, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + TemplateFiles.RenewalOffice);
                LogResult(info, failResult);
                return failResult;
            }

            // ⑦ SMTP 발송
            var sendResult = _smtp.SendByBranch(
                info.BranchId ?? 0,
                info.Email,
                subject,
                body);

            // ⑧ 로그 기록
            LogResult(info, sendResult);

            return sendResult;
        }

        #endregion

        #region 수기 입력 검증

        /// <summary>
        /// 수기 입력 필수 필드를 검증합니다.
        /// </summary>
        /// <returns>에러 메시지 (정상이면 null)</returns>
        private static string ValidateManualInput(RenewalInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.NewDeposit))
                return "변경 예치금을 입력해 주세요.";

            if (string.IsNullOrWhiteSpace(info.NewPrice))
                return "변경 이용료를 입력해 주세요.";

            if (string.IsNullOrWhiteSpace(info.ReplyDeadline))
                return "회신 요청일을 입력해 주세요.";

            // 참조안내사항은 선택 (빈 값 허용)

            return null;
        }

        #endregion

        #region 치환 토큰 구성

        /// <summary>
        /// RenewalInfo의 필드를 템플릿 치환용 Dictionary로 변환합니다.
        /// RenewalOffice.html 템플릿의 플레이스홀더와 매칭됩니다.
        /// </summary>
        private static Dictionary<string, string> BuildTokens(RenewalInfo info)
        {
            return new Dictionary<string, string>
            {
                // DB 기반
                ["회사명"]       = info.CustName ?? string.Empty,
                ["지점"]         = info.BranchName ?? string.Empty,
                ["상품/분류"]    = info.ProductName ?? string.Empty,
                ["호실"]         = info.OfficeNum ?? string.Empty,
                ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                ["임대료"]       = info.Price.ToString("#,0") + "원",
                ["계약종료일"]   = info.DateTo?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["납부계좌"]     = info.BankAccount ?? string.Empty,
                ["예금주"]       = info.BankHolder ?? string.Empty,

                // 수기 입력
                ["변경예치금"]   = info.NewDeposit ?? string.Empty,
                ["변경이용료"]   = info.NewPrice ?? string.Empty,
                ["회신요청일"]   = info.ReplyDeadline ?? string.Empty,
                ["참조안내사항"] = info.ReferenceNote ?? string.Empty
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
                    mailType: MailTypes.RenewalManual,
                    moveInId: info.MoveInId,
                    custName: info.CustName,
                    email: info.Email,
                    branchId: info.BranchId,
                    result: result);

                _logRepo.Insert(entry);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[갱신수동] 로그 기록 실패 — MoveInId={info.MoveInId}", ex);
            }
        }

        #endregion

        #region Row Mapper

        /// <summary>SqlDataReader → RenewalInfo 매핑.</summary>
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
