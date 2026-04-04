using System;

namespace BizSqNotifier.Models
{
    /// <summary>계약 갱신 안내 데이터 모델. 오피스(수동)/adBox·회원제(자동) 공용.</summary>
    public sealed class RenewalInfo
    {
        public int MoveInId { get; set; }
        public string BranchCode { get; set; }
        public string CustName { get; set; }
        public string Email { get; set; }
        public string BranchName { get; set; }
        public string BankAccount { get; set; }
        public string BankHolder { get; set; }
        public string ProductName { get; set; }
        public string OfficeNum { get; set; }
        public int Deposit { get; set; }
        public int Price { get; set; }
        public string DateFrom { get; set; }       // varchar
        public string DateTo { get; set; }         // varchar
        public string DateOut { get; set; }        // varchar
        public int DaysUntilExpiry { get; set; }

        // 수기 입력 (오피스 수동 전용)
        public string NewDeposit { get; set; }
        public string NewPrice { get; set; }
        public string ReplyDeadline { get; set; }
        public string ReferenceNote { get; set; }
    }
}
