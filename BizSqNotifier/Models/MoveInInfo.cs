using System;

namespace BizSqNotifier.Models
{
    /// <summary>입주 안내 메일 발송 데이터 모델.</summary>
    public sealed class MoveInInfo
    {
        public int MoveInId { get; set; }          // tb_movein.id
        public string BranchCode { get; set; }     // tb_movein.br_code (varchar)
        public string CustName { get; set; }       // tb_movein.cust
        public string Email { get; set; }          // tb_customer.email
        public string BranchName { get; set; }     // tb_branch.br_name
        public string ProductName { get; set; }    // tb_movein.prd_prd
        public string OfficeNum { get; set; }      // tb_movein.off_num
        public int Deposit { get; set; }           // tb_movein.deposit (int)
        public int Price { get; set; }             // tb_movein.price (int)
        public string DateFrom { get; set; }       // tb_movein.date_from (varchar)
        public string DateTo { get; set; }         // tb_movein.date_to (varchar)
    }
}
