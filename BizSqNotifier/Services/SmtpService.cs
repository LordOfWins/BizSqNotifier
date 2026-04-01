using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using BizSqNotifier.Data;
using BizSqNotifier.Models;

namespace BizSqNotifier.Services
{
    /// <summary>
    /// SMTP 메일 발송 엔진.
    /// 1순위: tb_branch 기반 지점별 SMTP 설정
    /// 2순위: SmtpSettings (settings.json) 폴백
    /// </summary>
    public sealed class SmtpService
    {
        private readonly BranchRepository _branchRepo;

        public SmtpService()
        {
            _branchRepo = new BranchRepository();
        }

        #region 지점별 SMTP 발송 (주 사용 메서드)

        /// <summary>
        /// 지점 ID 기반으로 SMTP 설정을 DB에서 로드하여 HTML 메일을 발송합니다.
        /// </summary>
        /// <param name="branchId">지점 ID (tb_branch.br_id)</param>
        /// <param name="toEmail">수신자 이메일</param>
        /// <param name="subject">메일 제목</param>
        /// <param name="htmlBody">HTML 본문</param>
        /// <returns>발송 결과</returns>
        public SendResult SendByBranch(int branchId, string toEmail, string subject, string htmlBody)
        {
            // 이메일 미등록 체크
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                AppLog.Warn($"이메일 미등록 — 지점ID={branchId}, SKIP 처리");
                return SendResult.Skip("이메일 미등록");
            }

            // 이메일 형식 검증
            if (!IsValidEmail(toEmail))
            {
                AppLog.Warn($"이메일 형식 오류 — {toEmail}, SKIP 처리");
                return SendResult.Skip($"이메일 형식 오류: {toEmail}");
            }

            try
            {
                // DB에서 지점 SMTP 설정 로드
                var branch = _branchRepo.GetByBranchId(branchId);
                if (branch == null)
                {
                    AppLog.Error($"지점 SMTP 설정 없음 — 지점ID={branchId}");
                    return SendResult.Fail($"지점ID={branchId}의 SMTP 설정을 찾을 수 없습니다.");
                }

                if (string.IsNullOrWhiteSpace(branch.SmtpHost))
                {
                    AppLog.Error($"지점 SMTP 호스트 미설정 — 지점={branch.BranchName}");
                    return SendResult.Fail($"지점 '{branch.BranchName}'의 SMTP 호스트가 설정되지 않았습니다.");
                }

                return ExecuteSend(
                    host: branch.SmtpHost,
                    port: branch.SmtpPort,
                    enableSsl: branch.EnableSsl,
                    userName: branch.SmtpUser,
                    password: branch.SmtpPassword,
                    fromAddress: branch.FromAddress ?? branch.SmtpUser,
                    fromDisplayName: branch.FromDisplayName ?? branch.BranchName ?? "모아",
                    toEmail: toEmail,
                    subject: subject,
                    htmlBody: htmlBody);
            }
            catch (Exception ex)
            {
                AppLog.Error($"SendByBranch 실패 — 지점ID={branchId}, 수신={toEmail}", ex);
                return SendResult.Fail(TruncateError(ex.Message));
            }
        }

        #endregion

        #region SmtpSettings 폴백 발송

        /// <summary>
        /// SmtpSettings(settings.json 기반) 발송 — DB 설정이 없을 경우 폴백용.
        /// </summary>
        public SendResult SendBySettings(SmtpSettings settings, string toEmail, string subject, string htmlBody)
        {
            if (settings == null)
                return SendResult.Fail("SmtpSettings가 null입니다.");

            if (string.IsNullOrWhiteSpace(toEmail))
                return SendResult.Skip("이메일 미등록");

            if (!IsValidEmail(toEmail))
                return SendResult.Skip($"이메일 형식 오류: {toEmail}");

            try
            {
                return ExecuteSend(
                    host: settings.Host,
                    port: settings.Port,
                    enableSsl: settings.EnableSsl,
                    userName: settings.UserName,
                    password: settings.Password,
                    fromAddress: settings.FromAddress ?? settings.UserName,
                    fromDisplayName: settings.FromDisplayName ?? "모아",
                    toEmail: toEmail,
                    subject: subject,
                    htmlBody: htmlBody);
            }
            catch (Exception ex)
            {
                AppLog.Error($"SendBySettings 실패 — 수신={toEmail}", ex);
                return SendResult.Fail(TruncateError(ex.Message));
            }
        }

        #endregion

        #region SMTP 연결 테스트

        /// <summary>
        /// 지점 ID 기반 SMTP 연결 테스트. 설정 화면에서 "테스트" 버튼에 사용합니다.
        /// 실제 메일은 발송하지 않고, SMTP 서버 연결(HELO)만 확인합니다.
        /// </summary>
        public SendResult TestConnection(int branchId)
        {
            try
            {
                var branch = _branchRepo.GetByBranchId(branchId);
                if (branch == null)
                    return SendResult.Fail($"지점ID={branchId} 설정을 찾을 수 없습니다.");

                using (var client = CreateSmtpClient(branch.SmtpHost, branch.SmtpPort, branch.EnableSsl,
                                                      branch.SmtpUser, branch.SmtpPassword))
                {
                    // SmtpClient에는 연결만 테스트하는 API가 없으므로
                    // 자신에게 빈 메일을 보내는 대신, 단순 Dispose로 연결 검증
                    // 실제로는 SMTP 서버 접속 가능 여부는 Send 시에만 알 수 있으므로
                    // 테스트 메일을 자신에게 발송합니다.
                    var fromAddr = branch.FromAddress ?? branch.SmtpUser;
                    if (string.IsNullOrWhiteSpace(fromAddr))
                        return SendResult.Fail("발신 이메일이 설정되지 않았습니다.");

                    using (var msg = new MailMessage(
                        new MailAddress(fromAddr, branch.FromDisplayName ?? "테스트"),
                        new MailAddress(fromAddr))
                    {
                        Subject = "[BizSqNotifier] SMTP 연결 테스트",
                        Body = "이 메일은 SMTP 연결 테스트용입니다. 정상 수신되었다면 설정이 올바릅니다.",
                        IsBodyHtml = false
                    })
                    {
                        client.Send(msg);
                    }
                }

                AppLog.Info($"SMTP 테스트 성공 — 지점ID={branchId}");
                return SendResult.Ok();
            }
            catch (Exception ex)
            {
                AppLog.Error($"SMTP 테스트 실패 — 지점ID={branchId}", ex);
                return SendResult.Fail(TruncateError(ex.Message));
            }
        }

        #endregion

        #region 내부 발송 실행

        /// <summary>
        /// 실제 SMTP 발송을 수행하는 공통 메서드.
        /// </summary>
        private SendResult ExecuteSend(
            string host, int port, bool enableSsl,
            string userName, string password,
            string fromAddress, string fromDisplayName,
            string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(fromAddress))
                return SendResult.Fail("발신 이메일(FromAddress)이 설정되지 않았습니다.");

            using (var client = CreateSmtpClient(host, port, enableSsl, userName, password))
            {
                // 발신자 주소 — UTF-8 DisplayName 지원
                var from = new MailAddress(fromAddress, fromDisplayName, Encoding.UTF8);
                var to = new MailAddress(toEmail);

                using (var msg = new MailMessage(from, to))
                {
                    // 제목 — UTF-8 인코딩 (한국어 깨짐 방지)
                    msg.SubjectEncoding = Encoding.UTF8;
                    msg.Subject = subject ?? string.Empty;

                    // 본문 — HTML / UTF-8
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.IsBodyHtml = true;
                    msg.Body = htmlBody ?? string.Empty;

                    // 헤더 인코딩
                    msg.HeadersEncoding = Encoding.UTF8;

                    AppLog.Info($"SMTP 발송 시도 — 수신={toEmail}, 제목={subject}");
                    client.Send(msg);
                    AppLog.Info($"SMTP 발송 완료 — 수신={toEmail}");
                }
            }

            return SendResult.Ok();
        }

        /// <summary>
        /// SmtpClient 인스턴스를 생성합니다.
        /// </summary>
        private SmtpClient CreateSmtpClient(string host, int port, bool enableSsl,
                                             string userName, string password)
        {
            var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30초 타임아웃
            };

            if (!string.IsNullOrEmpty(userName))
            {
                client.Credentials = new NetworkCredential(userName, password);
            }

            return client;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 기본적인 이메일 형식 검증.
        /// 정규식 대신 System.Net.Mail.MailAddress 파서를 활용합니다.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email.Trim());
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 에러 메시지를 tb_mail_log.error_msg 컬럼 크기(500자)에 맞춰 잘라냅니다.
        /// </summary>
        private static string TruncateError(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            return message.Length > 490 ? message.Substring(0, 490) + "..." : message;
        }

        #endregion
    }
}
