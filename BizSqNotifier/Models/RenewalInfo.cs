using System;

namespace BizSqNotifier.Models
{
    /// <summary>
    /// 계약 갱신 안내 메일 발송에 필요한 데이터 모델.
    /// 오피스(수동) / adBox·회원제(자동) 공용.
    /// tb_movein + tb_customer + tb_branch JOIN 결과를 매핑합니다.
    /// </summary>
    public sealed class RenewalInfo
    {
        // ── PK / FK ──
        public int MoveInId { get; set; }
        public int? BranchId { get; set; }

        // ── 회사 / 수신자 ──
        /// <summary>회사명 (tb_movein.cust)</summary>
        public string CustName { get; set; }

        /// <summary>청구서 수신 이메일 (tb_customer.email)</summary>
        public string Email { get; set; }

        // ── 지점 정보 ──
        /// <summary>지점명 (tb_branch.br_name)</summary>
        public string BranchName { get; set; }

        /// <summary>납부 계좌 (tb_branch.bank_accnt)</summary>
        public string BankAccount { get; set; }

        /// <summary>예금주 (tb_branch.bank_holder)</summary>
        public string BankHolder { get; set; }

        // ── 상품 / 호실 ──
        /// <summary>상품/분류 (tb_movein.prd_prd)</summary>
        public string ProductName { get; set; }

        /// <summary>호실 (tb_movein.off_num)</summary>
        public string OfficeNum { get; set; }

        // ── 금액 ──
        /// <summary>예치금 (tb_movein.deposit)</summary>
        public decimal Deposit { get; set; }

        /// <summary>임대료 (tb_movein.price)</summary>
        public decimal Price { get; set; }

        // ── 날짜 ──
        /// <summary>계약 시작일 (tb_movein.date_from)</summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>계약 종료일 (tb_movein.date_to)</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>퇴실 예정일 (tb_movein.date_out)</summary>
        public DateTime? DateOut { get; set; }

        // ── 계산 필드 ──
        /// <summary>갱신까지 남은 일수 (쿼리에서 계산)</summary>
        public int DaysUntilExpiry { get; set; }

        // ── 수기 입력 필드 (오피스 수동 전용) ──
        /// <summary>변경 예치금 (관리자 수기 입력)</summary>
        public string NewDeposit { get; set; }

        /// <summary>변경 이용료 (관리자 수기 입력)</summary>
        public string NewPrice { get; set; }

        /// <summary>회신 요청일 (관리자 수기 입력)</summary>
        public string ReplyDeadline { get; set; }

        /// <summary>참조 안내 사항 (관리자 수기 입력 / 2~4줄)</summary>
        public string ReferenceNote { get; set; }
    }
}
