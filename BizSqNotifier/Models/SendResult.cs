namespace BizSqNotifier.Models
{
    /// <summary>
    /// 메일 발송 결과를 나타내는 구조체.
    /// 발송 로그(tb_mail_log) 기록 시 status / error_msg 값으로 사용합니다.
    /// </summary>
    public sealed class SendResult
    {
        /// <summary>발송 성공 여부</summary>
        public bool Success { get; set; }

        /// <summary>상태 문자열 — "성공", "실패", "SKIP"</summary>
        public string Status { get; set; }

        /// <summary>실패 시 에러 메시지 (성공 시 null)</summary>
        public string ErrorMessage { get; set; }

        /// <summary>성공 결과 생성</summary>
        public static SendResult Ok()
        {
            return new SendResult { Success = true, Status = "성공", ErrorMessage = null };
        }

        /// <summary>실패 결과 생성</summary>
        public static SendResult Fail(string errorMessage)
        {
            return new SendResult { Success = false, Status = "실패", ErrorMessage = errorMessage };
        }

        /// <summary>SKIP 결과 생성 (이메일 미등록 등)</summary>
        public static SendResult Skip(string reason)
        {
            return new SendResult { Success = false, Status = "SKIP", ErrorMessage = reason };
        }
    }
}
