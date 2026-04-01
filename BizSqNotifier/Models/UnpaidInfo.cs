using System;

namespace BizSqNotifier.Models
{
    /// <summary>
    /// 미납 안내 메일 발송에 필요한 데이터 모델.
    /// tb_invoice + tb_inv_list + tb_movein + tb_customer + tb_branch JOIN 결과.
    ///
    /// 미납 판별 기준:
    ///   - 청구서가 발송 완료 상태 (tb_invoice.status 등)
    ///   - 납부 미완료 (수납 내역 없음)
    ///   - 납부일(tb_invoice.date_pay) 기준 경과일 계산
    /// </summary>
    public sealed class UnpaidInfo
    {
        // ── PK / FK ──
        public int MoveInId { get; set; }
        public int InvoiceId { get; set; }             // tb_invoice PK → 중복 체크 키
        public int? BranchId { get; set; }

        // ── 회사 / 수신자 ──
        public string CustName { get; set; }           // tb_movein.cust → {회사명}
        public string Email { get; set; }              // tb_customer.email → {청구서수신이메일}

        // ── 지점 정보 (tb_branch) ──
        public string BranchName { get; set; }         // tb_branch.br_name → {지점}
        public string BankAccount { get; set; }        // tb_branch.bank_accnt → {납부계좌}
        public string BankHolder { get; set; }         // tb_branch.bank_holder → {예금주}

        // ── 상품 / 호실 (tb_movein) ──
        public string ProductName { get; set; }        // tb_movein.prd_prd → {상품/분류}
        public string OfficeNum { get; set; }          // tb_movein.off_num → {호실}

        // ── 청구 금액 ──
        public decimal TotalAmount { get; set; }       // SUM(tb_inv_list.price + tax) → {합계금액}

        // ── 납부일 ──
        public DateTime DatePay { get; set; }          // tb_invoice.date_pay → 납부기한 계산 기준

        // ── 경과일 (쿼리에서 계산) ──
        public int DaysOverdue { get; set; }           // DATEDIFF(DAY, date_pay, GETDATE())
    }
}
