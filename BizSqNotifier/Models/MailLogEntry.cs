using System;

namespace BizSqNotifier.Models
{
    /// <summary>tb_mail_log 행에 대응.</summary>
    public sealed class MailLogEntry
    {
        public string MailType { get; set; }
        public int MoveInId { get; set; }
        public int? InvoiceId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public int? BranchId { get; set; }
        public DateTime SendDate { get; set; }
        public TimeSpan SendTime { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
