using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// 입주 안내 메일 자동 발송 서비스.
    /// 발송 조건: tb_movein.date_from = 오늘 AND 로그 없음
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

        #region SQL

        // tb_movein.date_from은 varchar(10) 'yyyy-MM-dd' 형식
        // tb_movein.br_code → tb_branch.br_code (varchar FK)
        // tb_movein.cu_id → tb_customer.id (int FK)
        private const string SelectTodayMoveInSql =
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
    ISNULL(m.price, 0)      AS price,
    m.date_from,
    m.date_to
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id    = c.id
    LEFT JOIN dbo.tb_branch   b ON m.br_code  = b.br_code
WHERE m.date_from = CONVERT(VARCHAR(10), GETDATE(), 120)
  AND ISNULL(m.prd_prd, '') <> '모아즈'
ORDER BY m.id;";

        #endregion

        #region 대상 조회

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

        #region 전체 처리

        public (int Total, int Success, int Fail, int Skip) ProcessAll(
            string printerLoginId,
            string printerLoginPw
        )
        {
            var targets = GetTodayTargets();
            int success = 0,
                fail = 0,
                skip = 0;
            AppLog.Info($"[입주] 오늘 대상 {targets.Count}건");

            foreach (var info in targets)
            {
                try
                {
                    var r = ProcessOne(info, printerLoginId, printerLoginPw);
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
                    AppLog.Error($"[입주] 오류 MoveInId={info.MoveInId}", ex);
                }
            }

            AppLog.Info(
                $"[입주] 완료 — 전체={targets.Count} 성공={success} 실패={fail} SKIP={skip}"
            );
            return (targets.Count, success, fail, skip);
        }

        #endregion

        #region 개별 처리

        public SendResult ProcessOne(MoveInInfo info, string printerLoginId, string printerLoginPw)
        {
            if (info == null)
                return SendResult.Fail("MoveInInfo null");

            // 중복 체크
            if (_logRepo.HasSentByMoveIn(MailTypes.MoveIn, info.MoveInId))
                return SendResult.Skip("이미 발송 완료");

            // 이메일 미등록
            if (string.IsNullOrWhiteSpace(info.Email))
            {
                var s = SendResult.Skip("이메일 미등록");
                LogResult(info, s);
                return s;
            }

            var tokens = BuildTokens(info, printerLoginId, printerLoginPw);
            var subject = _template.RenderSubject(
                TemplateEngine.GetDefaultSubject(MailTypes.MoveIn),
                tokens
            );
            var body = _template.LoadAndRender(TemplateFiles.MoveIn, tokens);

            if (string.IsNullOrEmpty(body))
            {
                var f = SendResult.Fail("템플릿 로드 실패");
                LogResult(info, f);
                return f;
            }

            // 복합기 ID/PW 미입력 시 복합기 안내 섹션 통째로 제거
            if (string.IsNullOrWhiteSpace(printerLoginId) && string.IsNullOrWhiteSpace(printerLoginPw))
            {
                var startTag = "<!-- PRINTER_SECTION_START -->";
                var endTag = "<!-- PRINTER_SECTION_END -->";
                var si = body.IndexOf(startTag);
                var ei = body.IndexOf(endTag);
                if (si >= 0 && ei > si)
                    body = body.Remove(si, ei + endTag.Length - si);
            }

            var result = _smtp.SendByBranch(info.BranchCode, info.Email, subject, body);
            LogResult(info, result);
            return result;
        }

        #endregion

        #region 토큰

        private static Dictionary<string, string> BuildTokens(
            MoveInInfo info,
            string pid,
            string ppw
        )
        {
            return new Dictionary<string, string>
            {
                ["회사명"] = info.CustName ?? "",
                ["청구서수신이메일"] = info.Email ?? "",
                ["지점"] = info.BranchName ?? "",
                ["상품/분류"] = info.ProductName ?? "",
                ["호실"] = info.OfficeNum ?? "",
                ["예치금"] = info.Deposit.ToString("#,0"),
                ["임대료"] = info.Price.ToString("#,0"),
                ["계약종료일"] = info.DateTo ?? "",
                ["복합기ID"] = pid ?? "",
                ["복합기PW"] = ppw ?? "",
            };
        }

        #endregion

        #region 로그

        private void LogResult(MoveInInfo info, SendResult result)
        {
            try
            {
                _logRepo.Insert(
                    MailLogEntry.Create(
                        MailTypes.MoveIn,
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
                AppLog.Error($"[입주] 로그 실패 {info.MoveInId}", ex);
            }
        }

        #endregion

        #region Mapper

        private static MoveInInfo MapRow(SqlDataReader r)
        {
            return new MoveInInfo
            {
                MoveInId = DbManager.GetSafeInt(r, "movein_id"),
                BranchCode = DbManager.GetSafeString(r, "br_code"),
                CustName = DbManager.GetSafeString(r, "cust_name"),
                Email = DbManager.GetSafeString(r, "email"),
                BranchName = DbManager.GetSafeString(r, "branch_name"),
                ProductName = DbManager.GetSafeString(r, "product_name"),
                OfficeNum = DbManager.GetSafeString(r, "office_num"),
                Deposit = DbManager.GetSafeInt(r, "deposit"),
                Price = DbManager.GetSafeInt(r, "price"),
                DateFrom = DbManager.GetSafeString(r, "date_from"),
                DateTo = DbManager.GetSafeString(r, "date_to"),
            };
        }

        #endregion
    }
}
