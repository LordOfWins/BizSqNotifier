using System;

namespace BizSqNotifier.Models
{
    /// <summary>
    /// 입주 안내 메일 발송에 필요한 데이터 모델.
    /// tb_movein + tb_customer + tb_branch JOIN 결과를 매핑합니다.
    /// </summary>
    public sealed class MoveInInfo
    {
        // ── PK / FK ──
        public int MoveInId { get; set; }
        public int? BranchId { get; set; }

        // ── 회사 / 수신자 ──
        public string CustName { get; set; }          // tb_movein.cust → {회사명}
        public string Email { get; set; }              // tb_customer.email → {청구서수신이메일}

        // ── 지점 정보 (tb_branch) ──
        public string BranchName { get; set; }         // tb_branch.br_name → {지점}

        // ── 상품 / 호실 (tb_movein) ──
        public string ProductName { get; set; }        // tb_movein.prd_prd → {상품/분류}
        public string OfficeNum { get; set; }          // tb_movein.off_num → {호실}

        // ── 금액 (tb_movein) ──
        public decimal Deposit { get; set; }           // tb_movein.deposit → {예치금}
        public decimal Price { get; set; }             // tb_movein.price → {임대료}

        // ── 날짜 (tb_movein) ──
        public DateTime? DateFrom { get; set; }        // tb_movein.date_from (입주일)
        public DateTime? DateTo { get; set; }          // tb_movein.date_to → {계약종료일}
    }
}
