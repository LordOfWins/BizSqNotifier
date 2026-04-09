using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>SMTP 메일 발송 엔진. tb_branch 기반 지점별 SMTP.</summary>
    public sealed class SmtpService
    {
        private readonly BranchRepository _branchRepo;
        private static readonly char[] EmailSeparators = { ',', ';' };

        public SmtpService()
        {
            _branchRepo = new BranchRepository();
        }

        /// <summary>지점 br_code 기반 HTML 메일 발송. 쉼표/세미콜론 구분 복수 이메일 지원.</summary>
        public SendResult SendByBranch(string branchCode, string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return SendResult.Skip("이메일 미등록");

            var addresses = toEmail.Split(EmailSeparators, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(e => e.Trim())
                                   .Where(e => !string.IsNullOrWhiteSpace(e))
                                   .ToList();

            if (addresses.Count == 0)
                return SendResult.Skip("이메일 미등록");

            if (addresses.Count == 1)
                return SendSingle(branchCode, addresses[0], subject, htmlBody);

            return SendMultiple(branchCode, addresses, subject, htmlBody);
        }

        private SendResult SendSingle(string branchCode, string email, string subject, string htmlBody)
        {
            if (!IsValidEmail(email))
                return SendResult.Skip($"이메일 형식 오류: {email}");

            try
            {
                var branch = _branchRepo.GetByBranchCode(branchCode);
                if (branch == null)
                    return SendResult.Fail($"지점 '{branchCode}' SMTP 설정 없음");
                if (string.IsNullOrWhiteSpace(branch.SmtpHost))
                    return SendResult.Fail($"지점 '{branch.BranchName}' SMTP 호스트 미설정");
                if (string.IsNullOrWhiteSpace(branch.SmtpEmail))
                    return SendResult.Fail($"지점 '{branch.BranchName}' SMTP 이메일 미설정");

                return ExecuteSend(
                    branch.SmtpHost, branch.SmtpPort, branch.EnableSsl,
                    branch.SmtpEmail, branch.SmtpPassword,
                    branch.FromAddress, branch.FromDisplayName,
                    email, subject, htmlBody);
            }
            catch (Exception ex)
            {
                AppLog.Error($"SendByBranch 실패 — {branchCode}, {email}", ex);
                return SendResult.Fail(Truncate(ex.Message));
            }
        }

        private SendResult SendMultiple(string branchCode, List<string> addresses, string subject, string htmlBody)
        {
            var sent = new List<string>();
            var skipped = new List<string>();
            var failed = new List<string>();

            foreach (var addr in addresses)
            {
                var result = SendSingle(branchCode, addr, subject, htmlBody);
                if (result.Success)
                    sent.Add(addr);
                else if (result.Status == "SKIP")
                    skipped.Add($"{addr}({result.ErrorMessage})");
                else
                    failed.Add($"{addr}({result.ErrorMessage})");
            }

            if (sent.Count == addresses.Count)
                return SendResult.Ok();

            if (sent.Count > 0)
            {
                var detail = $"발송 {sent.Count}건 성공";
                if (skipped.Count > 0) detail += $" / SKIP {skipped.Count}건: {string.Join(", ", skipped)}";
                if (failed.Count > 0) detail += $" / 실패 {failed.Count}건: {string.Join(", ", failed)}";
                AppLog.Info($"복수 이메일 발송 결과 — {detail}");
                return SendResult.Ok();
            }

            var allErrors = new List<string>();
            allErrors.AddRange(skipped);
            allErrors.AddRange(failed);
            return SendResult.Fail($"복수 이메일 전체 실패: {string.Join(", ", allErrors)}");
        }

        public SendResult TestConnection(string branchCode)
        {
            try
            {
                var branch = _branchRepo.GetByBranchCode(branchCode);
                if (branch == null) return SendResult.Fail($"지점 '{branchCode}' 없음");
                if (string.IsNullOrWhiteSpace(branch.SmtpEmail))
                    return SendResult.Fail("SMTP 이메일 미설정");

                using (var client = CreateClient(branch.SmtpHost, branch.SmtpPort, branch.EnableSsl,
                    branch.SmtpEmail, branch.SmtpPassword))
                using (var msg = new MailMessage(
                    new MailAddress(branch.SmtpEmail, "테스트"),
                    new MailAddress(branch.SmtpEmail))
                { Subject = "[BizSqNotifier] SMTP 테스트", Body = "정상 수신되면 설정 OK", IsBodyHtml = false })
                {
                    client.Send(msg);
                }
                return SendResult.Ok();
            }
            catch (Exception ex)
            {
                AppLog.Error($"SMTP 테스트 실패 — {branchCode}", ex);
                return SendResult.Fail(Truncate(ex.Message));
            }
        }

        private SendResult ExecuteSend(string host, int port, bool ssl,
            string userName, string password, string fromAddr, string fromName,
            string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(fromAddr))
                return SendResult.Fail("발신 이메일 미설정");

            using (var client = CreateClient(host, port, ssl, userName, password))
            using (var msg = new MailMessage(
                new MailAddress(fromAddr, fromName, Encoding.UTF8),
                new MailAddress(toEmail)))
            {
                msg.SubjectEncoding = Encoding.UTF8;
                msg.Subject = subject ?? "";
                msg.BodyEncoding = Encoding.UTF8;
                msg.IsBodyHtml = true;
                msg.Body = htmlBody ?? "";
                msg.HeadersEncoding = Encoding.UTF8;

                AppLog.Info($"SMTP 발송 → {toEmail}");
                client.Send(msg);
                AppLog.Info($"SMTP 완료 → {toEmail}");
            }
            return SendResult.Ok();
        }

        private SmtpClient CreateClient(string host, int port, bool ssl, string user, string pwd)
        {
            var c = new SmtpClient(host, port)
            { EnableSsl = ssl, DeliveryMethod = SmtpDeliveryMethod.Network, Timeout = 30000 };
            if (!string.IsNullOrEmpty(user))
                c.Credentials = new NetworkCredential(user, pwd);
            return c;
        }

        private static bool IsValidEmail(string email)
        {
            try { var a = new MailAddress(email.Trim()); return a.Address == email.Trim(); }
            catch { return false; }
        }

        private static string Truncate(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return null;
            return msg.Length > 490 ? msg.Substring(0, 490) + "..." : msg;
        }
    }
}
