using System;

namespace BizSqNotifier.Models
{
    /// <summary>퇴실 안내 메일 발송 데이터 모델.</summary>
    public sealed class MoveOutInfo
    {
        public int MoveInId { get; set; }
        public string BranchCode { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public string BranchName { get; set; }
        public string ProductName { get; set; }
        public string OfficeNum { get; set; }
        public int Deposit { get; set; }
        public string DateTo { get; set; }         // varchar
        public string DateOut { get; set; }        // varchar
        public int DaysUntilMoveOut { get; set; }
    }
}
