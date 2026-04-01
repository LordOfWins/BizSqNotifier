namespace BizSqNotifier.Models
{
    /// <summary>퇴실 안내 메일용 데이터.</summary>
    public sealed class MoveOutInfo
    {
        public int MoveInId { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public int? BranchId { get; set; }
    }
}
