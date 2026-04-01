using System;

namespace BizSqNotifier.Models
{
    /// <summary>tb_mail_log 행에 대응하는 모델.</summary>
    public sealed class MailLogEntry
    {
        // ── 조회 시 사용 (INSERT 시에는 DB 자동 생성) ──
        public int LogId { get; set; }

        // ── INSERT 필수 필드 ──
        public string MailType { get; set; }
        public int MoveInId { get; set; }
        public int? InvoiceId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public int? BranchId { get; set; }
        public DateTime SendDate { get; set; }
        public TimeSpan SendTime { get; set; }
        public string Status { get; set; }       // 성공 / 실패 / SKIP
        public string ErrorMessage { get; set; }

        // ── 조회 시 사용 ──
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 발송 결과(SendResult)와 기본 정보를 조합하여 로그 엔트리를 빠르게 생성합니다.
        /// </summary>
        public static MailLogEntry Create(
            string mailType,
            int moveInId,
            string custName,
            string email,
            int? branchId,
            SendResult result,
            int? invoiceId = null)
        {
            var now = DateTime.Now;
            return new MailLogEntry
            {
                MailType = mailType,
                MoveInId = moveInId,
                InvoiceId = invoiceId,
                CustName = custName ?? string.Empty,
                Email = email,
                BranchId = branchId,
                SendDate = now.Date,
                SendTime = now.TimeOfDay,
                Status = result?.Status ?? "실패",
                ErrorMessage = result?.ErrorMessage
            };
        }
    }
}
