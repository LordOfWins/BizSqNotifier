using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 퇴실 안내 메일 자동 발송 서비스.
    ///
    /// 워크플로:
    ///   1) 스케줄러가 매일 09:00(설정 변경 가능)에 ProcessAll() 호출
    ///   2) 퇴실예정일 1일 전 대상 조회
    ///   3) 중복 발송 방지 (movein_id 기반 1회성)
    ///   4) 템플릿 치환 + SMTP 발송 + 로그 기록
    ///
    /// 발송 조건 (Step1 확정):
    ///   - DATEDIFF(DAY, GETDATE(), tb_movein.date_out) = 1
    ///   - 동일 movein_id에 '퇴실' 로그 없음
    ///
    /// 템플릿 변수:
    ///   {회사명}, {퇴실예정일}
    ///
    /// [ESTIMATED] date_out 기준:
    ///   → 퇴실예정일(date_out)이 설정된 건만 대상
    ///   → date_out IS NOT NULL 조건으로 필터
    /// </summary>
    public sealed class MoveOutService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        /// <summary>
        /// 발송 기준일수 기본값 (퇴실예정일 N일 전).
        /// Step1 확정: 1일 전.
        /// </summary>
        public const int DefaultDaysBefore = 1;

        public MoveOutService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region 대상 조회 SQL

        // [ESTIMATED] tb_movein.date_out 컬럼:
        //   퇴실예정일로 사용. 값이 설정된 건만 퇴실 예정 대상.
        //   date_out IS NOT NULL AND DATEDIFF = @daysBefore
        //
        // [ESTIMATED] JOIN 조건: RenewalManualService / MoveInService와 동일 구조
        private const string SelectMoveOutTargetsSql = @"
SELECT
    m.movein_id,
    m.cust                  AS cust_name,
    c.email                 AS email,
    m.br_id                 AS branch_id,
    b.br_name               AS branch_name,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) AS days_until_moveout
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE m.date_out IS NOT NULL
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_out AS DATE)) = @daysBefore
ORDER BY m.date_out ASC, m.cust;";

        #endregion

        #region 발송 대상 조회

        /// <summary>
        /// 퇴실예정일 N일 전인 건을 조회합니다.
        /// </summary>
        /// <param name="daysBefore">퇴실예정일 기준 일수 (기본 1)</param>
        /// <returns>퇴실 대상 목록</returns>
        public List<MoveOutInfo> GetTargets(int daysBefore = DefaultDaysBefore)
        {
            try
            {
                return DbManager.ExecuteReader(
                    SelectMoveOutTargetsSql,
                    MapRow,
                    new SqlParameter("@daysBefore", daysBefore));
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 대상 조회 실패 (daysBefore={daysBefore})", ex);
                return new List<MoveOutInfo>();
            }
        }

        #endregion

        #region 자동 발송 (전체 처리) — 스케줄러에서 호출

        /// <summary>
        /// 퇴실 대상 전체에 대해 자동으로 메일을 발송합니다.
        /// SchedulerService에서 09:00에 호출됩니다.
        /// </summary>
        /// <param name="daysBefore">기준 일수 (기본 1)</param>
        /// <returns>(전체건수, 성공건수, 실패건수, SKIP건수)</returns>
        public (int Total, int Success, int Fail, int Skip) ProcessAll(int daysBefore = DefaultDaysBefore)
        {
            var targets = GetTargets(daysBefore);
            int success = 0, fail = 0, skip = 0;

            AppLog.Info($"[퇴실] 대상 {targets.Count}건 조회됨 (D-{daysBefore})");

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
                    AppLog.Error($"[퇴실] 개별 처리 오류 — MoveInId={info.MoveInId}", ex);
                }
            }

            AppLog.Info($"[퇴실] 처리 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}");
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 발송

        /// <summary>
        /// 단일 퇴실 건에 대해 중복 체크 → 템플릿 치환 → 발송 → 로그 기록을 수행합니다.
        /// </summary>
        public SendResult ProcessOne(MoveOutInfo info)
        {
            if (info == null)
                return SendResult.Fail("MoveOutInfo가 null입니다.");

            // ① 중복 발송 방지 (movein_id 기반 1회성)
            if (_logRepo.HasSentByMoveIn(MailTypes.MoveOut, info.MoveInId))
            {
                AppLog.Info($"[퇴실] 이미 발송됨 — MoveInId={info.MoveInId}, {info.CustName}");
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
            var subjectTemplate = TemplateEngine.GetDefaultSubject(MailTypes.MoveOut);
            var subject = _template.RenderSubject(subjectTemplate, tokens);

            // ⑤ 본문 템플릿 로드 + 치환
            var body = _template.LoadAndRender(TemplateFiles.MoveOut, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + TemplateFiles.MoveOut);
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
        /// MoveOutInfo의 필드를 MoveOut.html 템플릿 치환용 Dictionary로 변환합니다.
        ///
        /// MoveOut.html 사용 변수:
        ///   {회사명}       → info.CustName
        ///   {퇴실예정일}   → info.DateOut (yyyy-MM-dd)
        ///
        /// 추가 변수: 향후 템플릿 확장 대비
        /// </summary>
        private static Dictionary<string, string> BuildTokens(MoveOutInfo info)
        {
            return new Dictionary<string, string>
            {
                // MoveOut.html에서 사용하는 핵심 변수
                ["회사명"]       = info.CustName ?? string.Empty,
                ["퇴실예정일"]   = info.DateOut?.ToString("yyyy-MM-dd") ?? string.Empty,

                // 향후 템플릿 확장 시 활용 가능한 추가 변수
                ["지점"]         = info.BranchName ?? string.Empty,
                ["상품/분류"]    = info.ProductName ?? string.Empty,
                ["호실"]         = info.OfficeNum ?? string.Empty,
                ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                ["계약종료일"]   = info.DateTo?.ToString("yyyy-MM-dd") ?? string.Empty
            };
        }

        #endregion

        #region 로그 기록

        /// <summary>
        /// 발송 결과를 tb_mail_log에 기록합니다.
        /// </summary>
        private void LogResult(MoveOutInfo info, SendResult result)
        {
            try
            {
                var entry = MailLogEntry.Create(
                    mailType: MailTypes.MoveOut,
                    moveInId: info.MoveInId,
                    custName: info.CustName,
                    email: info.Email,
                    branchId: info.BranchId,
                    result: result);

                _logRepo.Insert(entry);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[퇴실] 로그 기록 실패 — MoveInId={info.MoveInId}", ex);
            }
        }

        #endregion

        #region Row Mapper

        /// <summary>SqlDataReader → MoveOutInfo 매핑.</summary>
        private static MoveOutInfo MapRow(SqlDataReader reader)
        {
            return new MoveOutInfo
            {
                MoveInId         = DbManager.GetSafeInt(reader, "movein_id"),
                CustName         = DbManager.GetSafeString(reader, "cust_name"),
                Email            = DbManager.GetSafeString(reader, "email"),
                BranchId         = DbManager.GetSafeNullableInt(reader, "branch_id"),
                BranchName       = DbManager.GetSafeString(reader, "branch_name"),
                ProductName      = DbManager.GetSafeString(reader, "product_name"),
                OfficeNum        = DbManager.GetSafeString(reader, "office_num"),
                Deposit          = DbManager.GetSafeDecimal(reader, "deposit"),
                DateTo           = DbManager.GetSafeDateTime(reader, "date_to"),
                DateOut          = DbManager.GetSafeDateTime(reader, "date_out"),
                DaysUntilMoveOut = DbManager.GetSafeInt(reader, "days_until_moveout")
            };
        }

        #endregion
    }
}
