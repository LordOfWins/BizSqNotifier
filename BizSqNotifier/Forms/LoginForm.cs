using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 로그인 폼.
    /// 기존 MOS 사용자 계정 DB(tb_accnt) 연동.
    /// 비밀번호는 평문 비교 (MOS 기존 방식 동일).
    /// 인증 통과 시 DialogResult.OK 반환 → Program.cs에서 MainForm 진입.
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
                    AppLog.Info($"로그인 성공 — 사용자: {userId} ({LoginSession.UserName})");
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
        /// tb_accnt 테이블에서 인증을 수행합니다.
        /// 비밀번호는 평문 비교 (기존 MOS 동일 방식).
        /// 인증 성공 시 LoginSession에 사용자 정보를 저장합니다.
        /// </summary>
        private bool Authenticate(string userId, string password)
        {
            const string sql = @"
SELECT uid, pwd, uname, accnt, br_code
FROM dbo.tb_accnt
WHERE uid = @uid;";

            var rows = DbManager.ExecuteReader(sql, reader =>
            {
                return new
                {
                    Uid     = DbManager.GetSafeString(reader, "uid"),
                    Pwd     = DbManager.GetSafeString(reader, "pwd"),
                    Uname   = DbManager.GetSafeString(reader, "uname"),
                    Accnt   = DbManager.GetSafeInt(reader, "accnt"),
                    BrCode  = DbManager.GetSafeString(reader, "br_code")
                };
            }, new SqlParameter("@uid", userId));

            if (rows.Count == 0)
                return false;

            var user = rows[0];

            // 평문 비밀번호 비교 (MOS 기존 방식)
            if (user.Pwd != password)
                return false;

            // 인증 성공 → 세션에 사용자 정보 저장
            LoginSession.UserId = user.Uid;
            LoginSession.UserName = user.Uname;
            LoginSession.AccountLevel = user.Accnt;
            LoginSession.BranchCode = user.BrCode;

            return true;
        }

        #endregion
    }
}
