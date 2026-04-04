namespace BizSqNotifier.Models
{
    /// <summary>tb_branch 지점 정보 + SMTP 설정.</summary>
    public sealed class BranchSmtpInfo
    {
        public string BranchCode { get; set; }     // tb_branch.br_code (PK, varchar)
        public string BranchName { get; set; }     // tb_branch.br_name
        public string SmtpHost { get; set; }       // tb_branch.smtp (예: smtp.naver.com)
        public string SmtpEmail { get; set; }      // tb_branch.smtp_email (발송 계정)
        public string SmtpPassword { get; set; }   // tb_branch.smtp_pwd
        public string BankAccount { get; set; }    // tb_branch.bank_accnt
        public string BankHolder { get; set; }     // tb_branch.bank_holder

        // SMTP 포트/SSL은 smtp 호스트에서 자동 판별
        public int SmtpPort
        {
            get
            {
                if (string.IsNullOrEmpty(SmtpHost)) return 587;
                // cafe24 = 587, naver = 587
                return 587;
            }
        }

        public bool EnableSsl => true;

        // 발신 주소 = smtp_email
        public string FromAddress => SmtpEmail;
        public string FromDisplayName => BranchName ?? "모아";
    }
}
