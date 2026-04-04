using System;

namespace BizSqNotifier.Models
{
    /// <summary>미납 안내 메일 발송 데이터 모델.</summary>
    public sealed class UnpaidInfo
    {
        public int MoveInId { get; set; }          // tb_invoice.mi_id
        public int InvoiceId { get; set; }         // tb_invoice.id
        public string BranchCode { get; set; }     // tb_invoice.br_code (varchar)
        public string CustName { get; set; }       // tb_invoice.cust
        public string Email { get; set; }          // tb_customer.email
        public string BranchName { get; set; }     // tb_branch.br_name
        public string BankAccount { get; set; }    // tb_branch.bank_accnt
        public string BankHolder { get; set; }     // tb_branch.bank_holder
        public string ProductName { get; set; }    // tb_invoice.prd_prd
        public string OfficeNum { get; set; }      // tb_invoice.off_num
        public int TotalAmount { get; set; }       // SUM(tb_inv_list.price + tax)
        public string DatePay { get; set; }        // tb_invoice.date_pay (varchar)
        public int DaysOverdue { get; set; }       // 계산값
    }
}
