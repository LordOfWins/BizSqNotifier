namespace BizSqNotifier.Models
{
    /// <summary>
    /// tb_branch 테이블에서 로드하는 지점별 SMTP 설정.
    /// 기존 MOS에 이미 저장된 SMTP 발송 계정 정보를 그대로 활용합니다.
    /// </summary>
    public sealed class BranchSmtpInfo
    {
        /// <summary>지점 ID (tb_branch PK)</summary>
        public int BranchId { get; set; }

        /// <summary>지점명</summary>
        public string BranchName { get; set; }

        /// <summary>SMTP 서버 호스트 (예: smtp.gmail.com)</summary>
        public string SmtpHost { get; set; }

        /// <summary>SMTP 포트 (예: 587)</summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>SSL 사용 여부</summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>SMTP 인증 계정 (이메일 주소)</summary>
        public string SmtpUser { get; set; }

        /// <summary>SMTP 인증 비밀번호</summary>
        public string SmtpPassword { get; set; }

        /// <summary>발신자 표시 이메일 (별도 설정 없으면 SmtpUser 사용)</summary>
        public string FromAddress { get; set; }

        /// <summary>발신자 표시 이름 (예: 모아 홍대점)</summary>
        public string FromDisplayName { get; set; }

        /// <summary>납부 계좌번호 (메일 본문 치환용)</summary>
        public string BankAccount { get; set; }

        /// <summary>예금주 (메일 본문 치환용)</summary>
        public string BankHolder { get; set; }
    }
}
