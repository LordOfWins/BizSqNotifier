namespace BizSqNotifier.Models
{
    /// <summary>SMTP 발송 설정 (settings.json 등에서 역직렬화).</summary>
    public sealed class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FromAddress { get; set; }
        public string FromDisplayName { get; set; }
    }
}
