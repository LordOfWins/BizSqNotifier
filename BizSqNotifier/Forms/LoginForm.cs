using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 로그인 폼. 기존 MOS 계정 DB 연동.
    /// [ESTIMATED] tb_accnt 테이블/컬럼명 추정 — 자동 감지로 대응.
    /// </summary>
    public partial class LoginForm : Form
    {
        private int _loginAttempts = 0;
        private const int MaxAttempts = 5;

        private static bool _schemaChecked = false;
        private static bool _schemaValid = false;
        private static string _schemaError = null;
        private static string _actualTableName = "tb_accnt";
        private static string _colUid = "uid";
        private static string _colPwd = "pwd";
        private static string _colUname = "uname";
        private static string _colAccnt = "accnt";
        private static string _colBrCode = "br_code";

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

            string errMsg;
            if (!DbManager.TestConnection(out errMsg))
            {
                lblMessage.Text = "DB 연결 실패 — App.config 연결 문자열을 확인하세요";
                lblMessage.ForeColor = Color.Red;
                AppLog.Error("로그인 화면 DB 연결 실패: " + errMsg);
                return;
            }

            if (!_schemaChecked) DetectAccountTableSchema();

            if (!_schemaValid && _schemaError != null)
            {
                lblMessage.Text = _schemaError;
                lblMessage.ForeColor = Color.FromArgb(211, 84, 0);
            }
        }

        #endregion

        #region 스키마 자동 감지

        private static void DetectAccountTableSchema()
        {
            try
            {
                if (!CheckTableExists("tb_accnt"))
                {
                    var candidates = FindCandidateTables(
                        new[] { "accnt", "account", "user", "login", "admin" });

                    if (candidates.Count > 0)
                    {
                        _schemaError = $"tb_accnt 없음. 유사: {string.Join(", ", candidates)}";
                        AppLog.Warn(_schemaError);
                        _actualTableName = candidates[0];
                    }
                    else
                    {
                        _schemaError = "계정 테이블(tb_accnt)을 찾을 수 없습니다";
                        _schemaChecked = true;
                        _schemaValid = false;
                        return;
                    }
                }

                var columns = GetTableColumns(_actualTableName);
                var colSet = new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
                AppLog.Info($"[Login] {_actualTableName} 컬럼: {string.Join(", ", columns)}");

                _colUid = FindColumn(colSet, new[] { "uid", "user_id", "userid", "login_id", "id" });
                _colPwd = FindColumn(colSet, new[] { "pwd", "password", "passwd", "pw", "user_pwd" });
                _colUname = FindColumn(colSet, new[] { "uname", "user_name", "username", "name" });
                _colAccnt = FindColumn(colSet, new[] { "accnt", "account", "acc_level", "level", "auth" });
                _colBrCode = FindColumn(colSet, new[] { "br_code", "branch_code", "branch_id", "br_id" });

                if (_colUid == null || _colPwd == null)
                {
                    _schemaError = $"{_actualTableName}에서 ID/PW 컬럼 없음. 컬럼: {string.Join(", ", columns)}";
                    _schemaValid = false;
                }
                else
                {
                    _schemaValid = true;
                    _schemaError = null;
                    AppLog.Info($"[Login] 매핑 완료 — uid={_colUid} pwd={_colPwd} uname={_colUname} accnt={_colAccnt} br_code={_colBrCode}");
                }
            }
            catch (Exception ex)
            {
                _schemaError = "스키마 감지 오류: " + ex.Message;
                _schemaValid = false;
                AppLog.Error("계정 테이블 스키마 감지 실패", ex);
            }
            finally { _schemaChecked = true; }
        }

        private static bool CheckTableExists(string tableName)
        {
            var r = DbManager.ExecuteScalar(
                "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @t",
                new SqlParameter("@t", tableName));
            return Convert.ToInt32(r) > 0;
        }

        private static List<string> FindCandidateTables(string[] keywords)
        {
            var list = new List<string>();
            foreach (var kw in keywords)
            {
                var rows = DbManager.ExecuteReader(
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE @p",
                    reader => DbManager.GetSafeString(reader, "TABLE_NAME"),
                    new SqlParameter("@p", "%" + kw + "%"));
                foreach (var r in rows)
                    if (r != null && !list.Contains(r)) list.Add(r);
            }
            return list;
        }

        private static List<string> GetTableColumns(string tableName)
        {
            return DbManager.ExecuteReader(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION",
                reader => DbManager.GetSafeString(reader, "COLUMN_NAME"),
                new SqlParameter("@t", tableName));
        }

        private static string FindColumn(HashSet<string> existing, string[] candidates)
        {
            foreach (var c in candidates)
                if (existing.Contains(c)) return c;
            return null;
        }

        #endregion

        #region 로그인

        private void OnLoginClick(object sender, EventArgs e)
        {
            var userId = txtUserId.Text.Trim();
            var password = txtPassword.Text;

            if (string.IsNullOrEmpty(userId))
            { ShowMsg("아이디를 입력하세요", true); txtUserId.Focus(); return; }
            if (string.IsNullOrEmpty(password))
            { ShowMsg("비밀번호를 입력하세요", true); txtPassword.Focus(); return; }
            if (!_schemaValid)
            { ShowMsg(_schemaError ?? "계정 테이블 구조를 확인할 수 없습니다", false, true); return; }
            if (_loginAttempts >= MaxAttempts)
            { ShowMsg($"로그인 시도 {MaxAttempts}회 초과. 재시작하세요.", false, true); btnLogin.Enabled = false; return; }

            btnLogin.Enabled = false;
            lblMessage.Text = "인증 중...";
            lblMessage.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                if (Authenticate(userId, password))
                {
                    AppLog.Info($"로그인 성공 — {userId} ({LoginSession.UserName})");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    _loginAttempts++;
                    int remaining = MaxAttempts - _loginAttempts;
                    ShowMsg($"아이디 또는 비밀번호 불일치 (남은 시도: {remaining}회)", false, true);
                    txtPassword.SelectAll(); txtPassword.Focus();
                    AppLog.Warn($"로그인 실패 — {userId} ({_loginAttempts}/{MaxAttempts})");
                    btnLogin.Enabled = _loginAttempts < MaxAttempts;
                }
            }
            catch (Exception ex)
            {
                ShowMsg("인증 오류: " + ex.Message, false, true);
                AppLog.Error("로그인 인증 오류", ex);
                btnLogin.Enabled = true;
            }
        }

        private void ShowMsg(string text, bool isWarning, bool isError = false)
        {
            lblMessage.Text = text;
            lblMessage.ForeColor = isError ? Color.Red
                : isWarning ? Color.FromArgb(211, 84, 0)
                : Color.Gray;
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

        private bool Authenticate(string userId, string password)
        {
            var selectCols = new List<string> { _colUid, _colPwd };
            if (_colUname != null) selectCols.Add(_colUname);
            if (_colAccnt != null) selectCols.Add(_colAccnt);
            if (_colBrCode != null) selectCols.Add(_colBrCode);

            var sql = $"SELECT {string.Join(", ", selectCols)} FROM dbo.{_actualTableName} WHERE {_colUid} = @uid;";

            var rows = DbManager.ExecuteReader(sql, reader => new
            {
                Uid    = SafeRead(reader, _colUid),
                Pwd    = SafeRead(reader, _colPwd),
                Uname  = _colUname != null ? SafeRead(reader, _colUname) : null,
                Accnt  = _colAccnt != null ? SafeReadInt(reader, _colAccnt) : 0,
                BrCode = _colBrCode != null ? SafeRead(reader, _colBrCode) : null
            }, new SqlParameter("@uid", userId));

            if (rows.Count == 0) return false;
            var user = rows[0];
            if (user.Pwd != password) return false;

            LoginSession.UserId = user.Uid;
            LoginSession.UserName = user.Uname ?? user.Uid;
            LoginSession.AccountLevel = user.Accnt;
            LoginSession.BranchCode = user.BrCode;
            return true;
        }

        private static string SafeRead(SqlDataReader r, string col)
        { try { return DbManager.GetSafeString(r, col); } catch { return null; } }

        private static int SafeReadInt(SqlDataReader r, string col)
        { try { return DbManager.GetSafeInt(r, col); } catch { return 0; } }

        #endregion
    }
}
