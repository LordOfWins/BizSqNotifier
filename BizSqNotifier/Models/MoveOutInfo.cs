using System;

namespace BizSqNotifier.Models
{
    /// <summary>
    /// 퇴실 안내 메일 발송에 필요한 데이터 모델.
    /// tb_movein + tb_customer + tb_branch JOIN 결과를 매핑합니다.
    ///
    /// 발송 조건 (Step1 확정):
    ///   DATEDIFF(DAY, GETDATE(), 퇴실예정일) = 1
    ///   AND 동일 movein_id에 '퇴실' 로그 없음
    /// </summary>
    public sealed class MoveOutInfo
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

        // ── 상품 / 호실 ──
        /// <summary>상품/분류 (tb_movein.prd_prd)</summary>
        public string ProductName { get; set; }

        /// <summary>호실 (tb_movein.off_num)</summary>
        public string OfficeNum { get; set; }

        // ── 금액 ──
        /// <summary>예치금 (tb_movein.deposit)</summary>
        public decimal Deposit { get; set; }

        // ── 날짜 ──
        /// <summary>계약 종료일 (tb_movein.date_to)</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>퇴실 예정일 (tb_movein.date_out)</summary>
        public DateTime? DateOut { get; set; }

        // ── 계산 필드 ──
        /// <summary>퇴실까지 남은 일수 (쿼리에서 계산)</summary>
        public int DaysUntilMoveOut { get; set; }
    }
}
