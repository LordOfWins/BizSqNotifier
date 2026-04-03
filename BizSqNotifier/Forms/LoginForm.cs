using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using BizSqNotifier.Data;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 로그인 폼.
    ///
    /// 기존 MOS 사용자 계정 DB 연동.
    /// [ESTIMATED] 사용자 테이블: tb_user (또는 tb_admin / tb_member)
    /// [ESTIMATED] 비밀번호 저장 방식: 평문 / MD5 / SHA256 중 하나
    ///   → 아래 코드에서 3가지 방식 모두 시도하는 폴백 로직 적용
    ///   → 실제 MOS DB 확인 후 불필요한 방식 제거 가능
    ///
    /// 인증 통과 시 DialogResult.OK 반환 → Program.cs에서 MainForm 진입
    /// </summary>
    public partial class LoginForm : Form
    {
        private int _loginAttempts = 0;
        private const int MaxAttempts = 5;

        public LoginForm()
        {
            InitializeComponent();
            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnLogin.Click += OnLoginClick;
            btnCancel.Click += OnCancelClick;

            // Enter키로 로그인
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { OnLoginClick(s, e); e.SuppressKeyPress = true; }
            };
            txtUserId.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { txtPassword.Focus(); e.SuppressKeyPress = true; }
            };
        }

        #endregion

        #region 폼 로드

        private void OnFormLoad(object sender, EventArgs e)
        {
            txtUserId.Focus();
            lblMessage.Text = string.Empty;

            // DB 연결 상태 체크 (연결 불가 시 경고)
            string errMsg;
            if (!DbManager.TestConnection(out errMsg))
            {
                lblMessage.Text = "DB 연결 실패 — App.config 연결 문자열을 확인하세요";
                lblMessage.ForeColor = Color.Red;
                AppLog.Error("로그인 화면 DB 연결 실패: " + errMsg);
            }
        }

        #endregion

        #region 로그인

        private void OnLoginClick(object sender, EventArgs e)
        {
            var userId = txtUserId.Text.Trim();
            var password = txtPassword.Text;

            // 입력 검증
            if (string.IsNullOrEmpty(userId))
            {
                lblMessage.Text = "아이디를 입력하세요";
                lblMessage.ForeColor = Color.FromArgb(211, 84, 0);
                txtUserId.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblMessage.Text = "비밀번호를 입력하세요";
                lblMessage.ForeColor = Color.FromArgb(211, 84, 0);
                txtPassword.Focus();
                return;
            }

            // 시도 횟수 체크
            if (_loginAttempts >= MaxAttempts)
            {
                lblMessage.Text = $"로그인 시도 {MaxAttempts}회 초과. 프로그램을 재시작하세요.";
                lblMessage.ForeColor = Color.Red;
                btnLogin.Enabled = false;
                return;
            }

            btnLogin.Enabled = false;
            lblMessage.Text = "인증 중...";
            lblMessage.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                bool authenticated = Authenticate(userId, password);

                if (authenticated)
                {
                    AppLog.Info($"로그인 성공 — 사용자: {userId}");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    _loginAttempts++;
                    int remaining = MaxAttempts - _loginAttempts;
                    lblMessage.Text = $"아이디 또는 비밀번호가 일치하지 않습니다 (남은 시도: {remaining}회)";
                    lblMessage.ForeColor = Color.FromArgb(192, 57, 43);
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    AppLog.Warn($"로그인 실패 — 사용자: {userId} (시도 {_loginAttempts}/{MaxAttempts})");
                    btnLogin.Enabled = _loginAttempts < MaxAttempts;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "인증 중 오류 발생: " + ex.Message;
                lblMessage.ForeColor = Color.Red;
                AppLog.Error("로그인 인증 오류", ex);
                btnLogin.Enabled = true;
            }
        }

        #endregion

        #region 취소

        private void OnCancelClick(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion

        #region 인증 로직

        /// <summary>
        /// 기존 MOS DB의 사용자 테이블에서 인증을 수행합니다.
        ///
        /// [ESTIMATED] 테이블/컬럼 추정:
        ///   테이블: tb_user
        ///   아이디: user_id
        ///   비밀번호: user_pwd
        ///
        /// [ESTIMATED] 비밀번호 검증 전략 (MOS 방식 불명 → 폴백 순서):
        ///   1) 평문 비교 (VB.NET 레거시에서 흔함)
        ///   2) MD5 해시 비교
        ///   3) SHA256 해시 비교
        ///
        /// → 실제 MOS DB 확인 후 해당 방식만 남기면 됩니다.
        /// </summary>
        private bool Authenticate(string userId, string password)
        {
            // [ESTIMATED] 테이블명/컬럼명 — 실제 DB 확인 필요
            const string sql = @"
SELECT user_pwd
FROM dbo.tb_user
WHERE user_id = @userId;";

            var storedPwd = DbManager.ExecuteScalar(
                sql,
                new SqlParameter("@userId", userId));

            if (storedPwd == null || storedPwd == DBNull.Value)
                return false;

            var storedPwdStr = storedPwd.ToString();

            if (string.IsNullOrEmpty(storedPwdStr))
                return false;

            // ── 1) 평문 비교 ──
            if (storedPwdStr == password)
                return true;

            // ── 2) MD5 비교 (대소문자 무시) ──
            var md5Hash = ComputeHash(password, MD5.Create());
            if (string.Equals(storedPwdStr, md5Hash, StringComparison.OrdinalIgnoreCase))
                return true;

            // ── 3) SHA256 비교 (대소문자 무시) ──
            var sha256Hash = ComputeHash(password, SHA256.Create());
            if (string.Equals(storedPwdStr, sha256Hash, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// 문자열의 해시값을 16진수 문자열로 반환합니다.
        /// </summary>
        private static string ComputeHash(string input, HashAlgorithm algorithm)
        {
            using (algorithm)
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = algorithm.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        #endregion
    }
}
