using System;

namespace BizSqNotifier.Models
{
    /// <summary>tb_mail_log 행 모델.</summary>
    public sealed class MailLogEntry
    {
        public int LogId { get; set; }
        public string MailType { get; set; }
        public int MoveInId { get; set; }
        public int? InvoiceId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public string BranchCode { get; set; }     // varchar FK
        public DateTime SendDate { get; set; }
        public TimeSpan SendTime { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? CreatedAt { get; set; }

        public static MailLogEntry Create(
            string mailType, int moveInId, string custName,
            string email, string branchCode, SendResult result,
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
                BranchCode = branchCode,
                SendDate = now.Date,
                SendTime = now.TimeOfDay,
                Status = result?.Status ?? "실패",
                ErrorMessage = result?.ErrorMessage
            };
        }
    }
}
