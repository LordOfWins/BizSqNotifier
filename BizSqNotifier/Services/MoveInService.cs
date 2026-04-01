using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 입주 안내 메일 자동 발송 서비스.
    ///
    /// 발송 조건:
    ///   tb_movein.date_from = 오늘(GETDATE())
    ///   AND 해당 movein_id에 대해 '입주' 로그가 없음
    ///
    /// 치환 변수:
    ///   {회사명}, {청구서수신이메일}, {복합기ID}, {복합기PW}
    ///
    /// 복합기 로그인 정보는 settings.json에서 로드합니다.
    /// </summary>
    public sealed class MoveInService
    {
        private readonly MailLogRepository _logRepo;
        private readonly SmtpService _smtp;
        private readonly TemplateEngine _template;

        public MoveInService()
        {
            _logRepo = new MailLogRepository();
            _smtp = new SmtpService();
            _template = new TemplateEngine();
        }

        #region 조건 조회 SQL

        // [ESTIMATED] tb_movein.date_from 컬럼명 추정
        //   MOS VB.NET에서 입주일로 사용하는 컬럼 기준
        //   tb_customer JOIN 조건: tb_movein.cust_id = tb_customer.cust_id (추정)
        //   tb_branch JOIN 조건: tb_movein.br_id = tb_branch.br_id (추정)
        private const string SelectTodayMoveInSql = @"
SELECT
    m.movein_id,
    m.cust                  AS cust_name,
    c.email                 AS email,
    m.br_id                 AS branch_id,
    b.br_name               AS branch_name,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    ISNULL(m.price, 0)      AS price,
    m.date_from,
    m.date_to
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE CAST(m.date_from AS DATE) = CAST(GETDATE() AS DATE)
ORDER BY m.movein_id;";

        #endregion

        #region 발송 대상 조회

        /// <summary>
        /// 오늘 입주 예정인 건을 DB에서 조회합니다.
        /// </summary>
        public List<MoveInInfo> GetTodayTargets()
        {
            try
            {
                return DbManager.ExecuteReader(SelectTodayMoveInSql, MapRow);
            }
            catch (Exception ex)
            {
                AppLog.Error("입주 대상 조회 실패", ex);
                return new List<MoveInInfo>();
            }
        }

        #endregion

        #region 자동 발송 (전체 처리)

        /// <summary>
        /// 오늘 입주 대상 전체에 대해 메일을 발송합니다.
        /// 스케줄러에서 09:00에 호출됩니다.
        /// </summary>
        /// <param name="printerLoginId">복합기 ID (설정 화면에서 입력한 값)</param>
        /// <param name="printerLoginPw">복합기 PW (설정 화면에서 입력한 값)</param>
        /// <returns>(전체건수, 성공건수, 실패건수, SKIP건수)</returns>
        public (int Total, int Success, int Fail, int Skip) ProcessAll(
            string printerLoginId,
            string printerLoginPw)
        {
            var targets = GetTodayTargets();
            int success = 0, fail = 0, skip = 0;

            AppLog.Info($"[입주] 오늘 대상 {targets.Count}건 조회됨");

            foreach (var info in targets)
            {
                try
                {
                    var result = ProcessOne(info, printerLoginId, printerLoginPw);

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
                    AppLog.Error($"[입주] 개별 처리 오류 — MoveInId={info.MoveInId}", ex);
                }
            }

            AppLog.Info($"[입주] 처리 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}");
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 발송

        /// <summary>
        /// 단일 입주 건에 대해 중복 체크 → 템플릿 치환 → 발송 → 로그 기록을 수행합니다.
        /// </summary>
        public SendResult ProcessOne(
            MoveInInfo info,
            string printerLoginId,
            string printerLoginPw)
        {
            if (info == null)
                return SendResult.Fail("MoveInInfo가 null입니다.");

            // ① 중복 발송 방지 체크
            if (_logRepo.HasSentByMoveIn(MailTypes.MoveIn, info.MoveInId))
            {
                AppLog.Info($"[입주] 이미 발송됨 — MoveInId={info.MoveInId}, {info.CustName}");
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
            var tokens = BuildTokens(info, printerLoginId, printerLoginPw);

            // ④ 제목 치환
            var subjectTemplate = TemplateEngine.GetDefaultSubject(MailTypes.MoveIn);
            var subject = _template.RenderSubject(subjectTemplate, tokens);

            // ⑤ 본문 템플릿 로드 + 치환
            var body = _template.LoadAndRender(TemplateFiles.MoveIn, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var failResult = SendResult.Fail("템플릿 로드 실패: " + TemplateFiles.MoveIn);
                LogResult(info, failResult);
                return failResult;
            }

            // ⑥ SMTP 발송
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
        /// MoveInInfo의 필드를 템플릿 치환용 Dictionary로 변환합니다.
        /// </summary>
        private static Dictionary<string, string> BuildTokens(
            MoveInInfo info,
            string printerLoginId,
            string printerLoginPw)
        {
            return new Dictionary<string, string>
            {
                ["회사명"]         = info.CustName ?? string.Empty,
                ["청구서수신이메일"] = info.Email ?? string.Empty,
                ["지점"]           = info.BranchName ?? string.Empty,
                ["상품/분류"]      = info.ProductName ?? string.Empty,
                ["호실"]           = info.OfficeNum ?? string.Empty,
                ["예치금"]         = info.Deposit.ToString("#,0"),
                ["임대료"]         = info.Price.ToString("#,0"),
                ["계약종료일"]     = info.DateTo?.ToString("yyyy-MM-dd") ?? string.Empty,

                // 복합기 로그인 정보 (설정 화면에서 입력한 값)
                ["복합기ID"]       = printerLoginId ?? string.Empty,
                ["복합기PW"]       = printerLoginPw ?? string.Empty
            };
        }

        #endregion

        #region 로그 기록

        /// <summary>
        /// 발송 결과를 tb_mail_log에 기록합니다.
        /// </summary>
        private void LogResult(MoveInInfo info, SendResult result)
        {
            try
            {
                var entry = MailLogEntry.Create(
                    mailType: MailTypes.MoveIn,
                    moveInId: info.MoveInId,
                    custName: info.CustName,
                    email: info.Email,
                    branchId: info.BranchId,
                    result: result);

                _logRepo.Insert(entry);
            }
            catch (Exception ex)
            {
                AppLog.Error($"[입주] 로그 기록 실패 — MoveInId={info.MoveInId}", ex);
            }
        }

        #endregion

        #region Row Mapper

        /// <summary>SqlDataReader → MoveInInfo 매핑.</summary>
        private static MoveInInfo MapRow(SqlDataReader reader)
        {
            return new MoveInInfo
            {
                MoveInId    = DbManager.GetSafeInt(reader, "movein_id"),
                CustName    = DbManager.GetSafeString(reader, "cust_name"),
                Email       = DbManager.GetSafeString(reader, "email"),
                BranchId    = DbManager.GetSafeNullableInt(reader, "branch_id"),
                BranchName  = DbManager.GetSafeString(reader, "branch_name"),
                ProductName = DbManager.GetSafeString(reader, "product_name"),
                OfficeNum   = DbManager.GetSafeString(reader, "office_num"),
                Deposit     = DbManager.GetSafeDecimal(reader, "deposit"),
                Price       = DbManager.GetSafeDecimal(reader, "price"),
                DateFrom    = DbManager.GetSafeDateTime(reader, "date_from"),
                DateTo      = DbManager.GetSafeDateTime(reader, "date_to")
            };
        }

        #endregion
    }
}
