using System;
using System.Net;
using System.Net.Mail;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>SMTP 메일 발송.</summary>
    public sealed class SmtpService
    {
        public void SendHtml(SmtpSettings settings, string to, string subject, string htmlBody)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("수신 주소가 비어 있습니다.", nameof(to));

            using (var client = new SmtpClient(settings.Host, settings.Port))
            {
                client.EnableSsl = settings.EnableSsl;
                if (!string.IsNullOrEmpty(settings.UserName))
                    client.Credentials = new NetworkCredential(settings.UserName, settings.Password);

                var addr = settings.FromAddress ?? settings.UserName;
                if (string.IsNullOrWhiteSpace(addr))
                    throw new InvalidOperationException("FromAddress 또는 UserName을 설정하세요.");
                var from = new MailAddress(addr, settings.FromDisplayName ?? string.Empty);
                using (var msg = new MailMessage(from, new MailAddress(to))
                {
                    Subject = subject ?? string.Empty,
                    Body = htmlBody ?? string.Empty,
                    IsBodyHtml = true
                })
                {
                    AppLog.Info("SMTP 발송 시도: " + to);
                    client.Send(msg);
                    AppLog.Info("SMTP 발송 완료: " + to);
                }
            }
        }
    }
}
