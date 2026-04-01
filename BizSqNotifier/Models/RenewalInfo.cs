namespace BizSqNotifier.Models
{
    /// <summary>갱신(자동/수동) 메일용 데이터.</summary>
    public sealed class RenewalInfo
    {
        public int MoveInId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public int? BranchId { get; set; }
    }
}
