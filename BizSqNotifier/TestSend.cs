using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BizSqNotifier.Data;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    public static class TestSend
    {
        private const string TestEmail = "sjyun011234@kakao.com";

        public static void Run()
        {
            AppLog.Info("[테스트] === 테스트 발송 시작 ===");
            AppLog.Info("[테스트] 수신: " + TestEmail);

            try
            {
                var branchRepo = new BranchRepository();
                var branches = branchRepo.GetAll();
                BranchSmtpInfo testBranch = null;

                AppLog.Info("[테스트] 지점 수: " + branches.Count);

                foreach (var br in branches)
                {
                    if (!string.IsNullOrWhiteSpace(br.SmtpHost)
                        && !string.IsNullOrWhiteSpace(br.SmtpEmail)
                        && !string.IsNullOrWhiteSpace(br.SmtpPassword))
                    {
                        testBranch = br;
                        break;
                    }
                }

                if (testBranch == null)
                {
                    AppLog.Error("[테스트] SMTP 설정된 지점 없음");
                    return;
                }

                AppLog.Info("[테스트] 발송 지점: " + testBranch.BranchName + " / " + testBranch.SmtpEmail);

                var smtp = new SmtpService();
                var template = new TemplateEngine();

                // ── 1. 입주 안내 ──
                AppLog.Info("[테스트] [1/4] 입주 안내 발송 시도...");
                try
                {
                    var tokens = new Dictionary<string, string>
                    {
                        ["회사명"] = "테스트회사(주)",
                        ["청구서수신이메일"] = TestEmail,
                        ["지점"] = testBranch.BranchName,
                        ["상품/분류"] = "2인실",
                        ["호실"] = "301호",
                        ["예치금"] = "500,000",
                        ["임대료"] = "350,000",
                        ["계약종료일"] = "2026-12-31",
                        ["복합기ID"] = "test_printer",
                        ["복합기PW"] = "1234"
                    };
                    var subject = template.RenderSubject(
                        TemplateEngine.GetDefaultSubject(MailTypes.MoveIn), tokens);
                    var body = template.LoadAndRender(TemplateFiles.MoveIn, tokens);
                    AppLog.Info("[테스트] 입주 템플릿 로드 " + (string.IsNullOrEmpty(body) ? "실패" : "성공 (" + body.Length + "자)"));
                    var r = smtp.SendByBranch(testBranch.BranchCode, TestEmail, subject, body);
                    AppLog.Info("[테스트] [1/4] 입주 결과: " + r.Status + (r.ErrorMessage != null ? " - " + r.ErrorMessage : ""));
                }
                catch (Exception ex) { AppLog.Error("[테스트] [1/4] 입주 오류", ex); }

                // ── 2. 미납 1차 ──
                AppLog.Info("[테스트] [2/4] 미납 1차 발송 시도...");
                try
                {
                    var tokens = new Dictionary<string, string>
                    {
                        ["회사명"] = "테스트회사(주)",
                        ["지점"] = testBranch.BranchName,
                        ["상품/분류"] = "2인실",
                        ["호실"] = "301호",
                        ["합계금액"] = "385,000원",
                        ["납부기한"] = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd"),
                        ["납부계좌"] = testBranch.BankAccount ?? "테스트계좌",
                        ["예금주"] = testBranch.BankHolder ?? "테스트예금주"
                    };
                    var subject = template.RenderSubject(
                        TemplateEngine.GetDefaultSubject(MailTypes.Unpaid1st), tokens);
                    var body = template.LoadAndRender(TemplateFiles.Unpaid1st, tokens);
                    var r = smtp.SendByBranch(testBranch.BranchCode, TestEmail, subject, body);
                    AppLog.Info("[테스트] [2/4] 미납1차 결과: " + r.Status + (r.ErrorMessage != null ? " - " + r.ErrorMessage : ""));
                }
                catch (Exception ex) { AppLog.Error("[테스트] [2/4] 미납1차 오류", ex); }

                // ── 3. 계약 갱신 ──
                AppLog.Info("[테스트] [3/4] 계약 갱신 발송 시도...");
                try
                {
                    var tokens = new Dictionary<string, string>
                    {
                        ["회사명"] = "테스트회사(주)",
                        ["계약종료일"] = "2026-05-01",
                        ["계약종료일 포함 3일전"] = "2026-04-29",
                        ["회신기한"] = "2026-04-29",
                        ["지점"] = testBranch.BranchName,
                        ["상품/분류"] = "법인사업자",
                        ["호실"] = "501호",
                        ["예치금"] = "300,000원",
                        ["임대료"] = "200,000원",
                        ["납부계좌"] = testBranch.BankAccount ?? "테스트계좌",
                        ["예금주"] = testBranch.BankHolder ?? "테스트예금주"
                    };
                    var subject = template.RenderSubject(
                        TemplateEngine.GetDefaultSubject(MailTypes.RenewalAuto), tokens);
                    var body = template.LoadAndRender(TemplateFiles.RenewalAuto, tokens);
                    var r = smtp.SendByBranch(testBranch.BranchCode, TestEmail, subject, body);
                    AppLog.Info("[테스트] [3/4] 갱신 결과: " + r.Status + (r.ErrorMessage != null ? " - " + r.ErrorMessage : ""));
                }
                catch (Exception ex) { AppLog.Error("[테스트] [3/4] 갱신 오류", ex); }

                // ── 4. 퇴실 안내 ──
                AppLog.Info("[테스트] [4/4] 퇴실 안내 발송 시도...");
                try
                {
                    var tokens = new Dictionary<string, string>
                    {
                        ["회사명"] = "테스트회사(주)",
                        ["퇴실예정일"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                        ["지점"] = testBranch.BranchName,
                        ["상품/분류"] = "2인실",
                        ["호실"] = "301호",
                        ["예치금"] = "500,000원",
                        ["계약종료일"] = "2026-12-31"
                    };
                    var subject = template.RenderSubject(
                        TemplateEngine.GetDefaultSubject(MailTypes.MoveOut), tokens);
                    var body = template.LoadAndRender(TemplateFiles.MoveOut, tokens);
                    var r = smtp.SendByBranch(testBranch.BranchCode, TestEmail, subject, body);
                    AppLog.Info("[테스트] [4/4] 퇴실 결과: " + r.Status + (r.ErrorMessage != null ? " - " + r.ErrorMessage : ""));
                }
                catch (Exception ex) { AppLog.Error("[테스트] [4/4] 퇴실 오류", ex); }

                AppLog.Info("[테스트] === 테스트 발송 완료 ===");
            }
            catch (Exception ex)
            {
                AppLog.Error("[테스트] 치명적 오류", ex);
            }
        }
    }
}
