namespace BizSqNotifier.Models
{
    /// <summary>미납 안내 메일용 데이터.</summary>
    public sealed class UnpaidInfo
    {
        public int MoveInId { get; set; }
        public int? InvoiceId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public int? BranchId { get; set; }
    }
}
