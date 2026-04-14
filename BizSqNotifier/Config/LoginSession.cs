namespace BizSqNotifier.Config
{
    /// <summary>
    /// 로그인 성공 후 세션 정보를 보관하는 정적 클래스.
    /// 프로그램 전역에서 현재 로그인 사용자 정보에 접근할 때 사용합니다.
    /// </summary>
    public static class LoginSession
    {
        /// <summary>로그인한 사용자 ID (tb_accnt.uid)</summary>
        public static string UserId { get; set; }

        /// <summary>로그인한 사용자 이름 (tb_accnt.uname)</summary>
        public static string UserName { get; set; }

        /// <summary>권한 레벨 (tb_accnt.accnt) — 1=최고관리자, 10=운영관리자, 200=일반, 500=투자대표</summary>
        public static int AccountLevel { get; set; }

        /// <summary>지점 코드 (tb_accnt.br_code)</summary>
        public static string BranchCode { get; set; }

        /// <summary>로그인 여부</summary>
        public static bool IsLoggedIn => !string.IsNullOrEmpty(UserId);

        /// <summary>
        /// 관리자 여부 판별.
        /// accnt 1=최고관리자, 10=운영관리자 → 설정 접근 가능
        /// accnt 200=일반, 500=투자대표 → 설정 접근 불가
        /// </summary>
        public static bool IsAdmin => AccountLevel <= 10;

        /// <summary>세션 초기화 (로그아웃 시)</summary>
        public static void Clear()
        {
            UserId = null;
            UserName = null;
            AccountLevel = 0;
            BranchCode = null;
        }
    }
}
