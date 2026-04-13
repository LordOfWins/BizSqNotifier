using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>메인 대시보드 + 트레이 아이콘 + 스케줄러.</summary>
    public partial class MainForm : Form
    {
        private readonly MailLogRepository _logRepo;
        private readonly Timer _refreshTimer;
        private readonly SchedulerService _scheduler;
        private readonly bool _silentMode;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private bool _realClose = false;

        public MainForm(bool silentMode = false)
        {
            _silentMode = silentMode;
            InitializeComponent();
            _logRepo = new MailLogRepository();
            _scheduler = new SchedulerService();
            _refreshTimer = new Timer { Interval = 300_000 };
            _refreshTimer.Tick += (s, e) => SafeRefreshDashboard();
            SetupTrayIcon();
            WireEvents();
        }

        #region 트레이

        private void SetupTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("대시보드 열기", null, (s, e) => ShowDashboard());
            _trayMenu.Items.Add("즉시 전체 실행", null, (s, e) => RunAllNow());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("프로그램 종료", null, (s, e) => ExitApplication());

            _trayIcon = new NotifyIcon
            {
                Icon = GetAppIcon(),
                Text = "BizSqNotifier — 자동 메일 발송",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowDashboard();
        }

        private Icon GetAppIcon()
        {
            try
            {
                var p = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BizSqNotifier.ico");
                if (System.IO.File.Exists(p)) return new Icon(p);
            }
            catch { }
            return this.Icon ?? SystemIcons.Application;
        }

        private void ShowDashboard()
        {
            this.Show(); this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.BringToFront(); this.Activate();
            SafeRefreshDashboard();
        }

        private void RunAllNow()
        {
            _trayIcon.ShowBalloonTip(3000, "BizSqNotifier", "전체 발송 즉시 실행...", ToolTipIcon.Info);
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _scheduler.RunAllNow();
                    ShowBalloon("전체 발송 완료", ToolTipIcon.Info);
                    SafeInvoke(() => SafeRefreshDashboard());
                }
                catch (Exception ex)
                {
                    AppLog.Error("즉시 실행 오류", ex);
                    ShowBalloon("오류 발생. 로그 확인.", ToolTipIcon.Error);
                }
            });
        }

        private void ExitApplication()
        {
            if (MessageBox.Show("종료하면 자동 발송이 중단됩니다.\n정말 종료?",
                "종료 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { _realClose = true; Application.Exit(); }
        }

        private void ShowBalloon(string text, ToolTipIcon icon)
        { try { _trayIcon?.ShowBalloonTip(3000, "BizSqNotifier", text, icon); } catch { } }

        private void SafeInvoke(Action a)
        { if (this.IsHandleCreated && !this.IsDisposed) this.BeginInvoke(a); }

        #endregion

        #region 이벤트

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
            this.Resize += OnFormResize;
            btnRenewalList.Click += (s, e) => OpenForm<RenewalListForm>();
            btnManualSend.Click += (s, e) => OpenForm<RenewalManualForm>();
            btnLogView.Click += (s, e) => OpenForm<LogViewForm>();
            btnSettings.Click += (s, e) => OpenSettings();
            btnRefresh.Click += (s, e) => SafeRefreshDashboard();

            // 카드 클릭 → 대상자 리스트 표시
            WireCardClick(cardSend, OnCardSendClick);
            WireCardClick(cardUnpaid, OnCardUnpaidClick);
            WireCardClick(cardRenewal, OnCardRenewalClick);
            WireCardClick(cardMoveOut, OnCardMoveOutClick);
            WireCardClick(cardMoveIn, OnCardMoveInClick);
        }

        /// <summary>카드 Panel + 자식 Label 전체에 클릭 이벤트 연결.</summary>
        private static void WireCardClick(Panel card, EventHandler handler)
        {
            card.Click += handler;
            foreach (Control c in card.Controls)
                c.Click += handler;
        }

        #endregion

        #region 생명주기

        private void OnFormLoad(object sender, EventArgs e)
        {
            lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

            // 관리자(1) 외 일반 사원은 설정 버튼 비활성화
            if (LoginSession.IsLoggedIn && LoginSession.AccountLevel != 1)
            {
                btnSettings.Enabled = false;
                btnSettings.BackColor = Color.FromArgb(189, 195, 199);
            }

            if (_silentMode)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false; this.Hide();
                ShowBalloon("백그라운드 자동 메일 발송 실행 중", ToolTipIcon.Info);
            }
            else SafeRefreshDashboard();

            _refreshTimer.Start();
            _scheduler.OnLog += msg => AppLog.Info(msg);
            _scheduler.Start();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_realClose && e.CloseReason == CloseReason.UserClosing)
            { e.Cancel = true; this.Hide(); this.ShowInTaskbar = false; ShowBalloon("트레이에서 실행 중", ToolTipIcon.Info); return; }
            _refreshTimer.Stop(); _refreshTimer.Dispose();
            _scheduler.Stop(); _scheduler.Dispose();
            _trayIcon.Visible = false; _trayIcon.Dispose();
        }

        private void OnFormResize(object sender, EventArgs e)
        { if (this.WindowState == FormWindowState.Minimized) { this.Hide(); this.ShowInTaskbar = false; } }

        #endregion

        #region 대시보드

        private void SafeRefreshDashboard()
        {
            lblLastUpdate.Text = "갱신 중...";
            lblLastUpdate.ForeColor = Color.Gray;
            Application.DoEvents();
            try
            {
                RefreshSummaryCards();
                RefreshTodayGrid();
                lblLastUpdate.Text = "마지막 갱신: " + DateTime.Now.ToString("HH:mm:ss");
                lblLastUpdate.ForeColor = Color.FromArgb(127, 140, 141);
            }
            catch (Exception ex)
            {
                lblLastUpdate.Text = "갱신 실패";
                lblLastUpdate.ForeColor = Color.Red;
                AppLog.Error("[대시보드] 갱신 실패", ex);
            }
        }

        private void RefreshSummaryCards()
        {
            var summary = _logRepo.GetTodaySummary();
            int tS = 0, tF = 0;
            foreach (var kv in summary) { tS += kv.Value.Success; tF += kv.Value.Fail; }
            lblCardSendCount.Text = tS.ToString();
            lblCardSendSub.Text = tF > 0 ? $"실패 {tF}건" : "정상";
            lblCardSendSub.ForeColor = tF > 0 ? Color.FromArgb(231, 76, 60) : Color.FromArgb(39, 174, 96);

            try
            {
                var list = new UnpaidService().GetUnpaidTargets(UserSettings.Current.Unpaid1stDays);
                lblCardUnpaidCount.Text = list.Count.ToString();
                lblCardUnpaidSub.Text = list.Count > 0 ? "조속히 처리 필요" : "미납 없음";
                lblCardUnpaidSub.ForeColor = list.Count > 0 ? Color.FromArgb(211, 84, 0) : Color.FromArgb(39, 174, 96);
            }
            catch { lblCardUnpaidCount.Text = "-"; lblCardUnpaidSub.Text = "조회 실패"; lblCardUnpaidSub.ForeColor = Color.Red; }

            try
            {
                var list = new RenewalManualService().GetRenewalTargets(UserSettings.Current.RenewalManualDays);
                lblCardRenewalCount.Text = list.Count.ToString();
                lblCardRenewalSub.Text = list.Count > 0 ? "갱신 안내 필요" : "예정 없음";
                lblCardRenewalSub.ForeColor = list.Count > 0 ? Color.FromArgb(41, 128, 185) : Color.FromArgb(127, 140, 141);
            }
            catch { lblCardRenewalCount.Text = "-"; lblCardRenewalSub.Text = "조회 실패"; lblCardRenewalSub.ForeColor = Color.Red; }

            try
            {
                // 7일 이내 퇴실 건 — 범위 조회로 개선
                var cnt = GetMoveOutCountWithin7Days();
                lblCardMoveOutCount.Text = cnt.ToString();
                lblCardMoveOutSub.Text = cnt > 0 ? "7일 이내" : "예정 없음";
                lblCardMoveOutSub.ForeColor = cnt > 0 ? Color.FromArgb(142, 68, 173) : Color.FromArgb(127, 140, 141);
            }
            catch { lblCardMoveOutCount.Text = "-"; lblCardMoveOutSub.Text = "조회 실패"; lblCardMoveOutSub.ForeColor = Color.Red; }

            try
            {
                var list = new MoveInService().GetTodayTargets();
                lblCardMoveInCount.Text = list.Count.ToString();
                lblCardMoveInSub.Text = list.Count > 0 ? "오늘 입주" : "대상 없음";
                lblCardMoveInSub.ForeColor = list.Count > 0 ? Color.FromArgb(243, 156, 18) : Color.FromArgb(127, 140, 141);
            }
            catch { lblCardMoveInCount.Text = "-"; lblCardMoveInSub.Text = "조회 실패"; lblCardMoveInSub.ForeColor = Color.Red; }
        }

        /// <summary>7일 이내 퇴실 건수 — 단일 쿼리로 조회 (성능 개선).</summary>
        private int GetMoveOutCountWithin7Days()
        {
            const string sql = @"
SELECT COUNT(*) FROM dbo.tb_movein
WHERE date_out IS NOT NULL AND date_out <> ''
  AND ISDATE(date_out) = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(date_out AS DATE)) BETWEEN 0 AND 7;";
            var r = DbManager.ExecuteScalar(sql);
            return Convert.ToInt32(r);
        }

        private void RefreshTodayGrid()
        {
            var summary = _logRepo.GetTodaySummary();
            dgvToday.Rows.Clear();

            var types = new[] {
                MailTypes.MoveIn, MailTypes.Unpaid1st, MailTypes.Unpaid2nd,
                MailTypes.UnpaidFinal, MailTypes.RenewalManual,
                MailTypes.RenewalAuto, MailTypes.MoveOut };
            var labels = new Dictionary<string, string>
            {
                [MailTypes.MoveIn] = "입주 안내", [MailTypes.Unpaid1st] = "미납 1차",
                [MailTypes.Unpaid2nd] = "미납 2차", [MailTypes.UnpaidFinal] = "미납 최종",
                [MailTypes.RenewalManual] = "갱신 수동", [MailTypes.RenewalAuto] = "갱신 자동",
                [MailTypes.MoveOut] = "퇴실 안내"
            };

            foreach (var t in types)
            {
                int s = 0, f = 0, sk = 0;
                if (summary.ContainsKey(t)) { s = summary[t].Success; f = summary[t].Fail; sk = summary[t].Skip; }
                var ri = dgvToday.Rows.Add(labels.ContainsKey(t) ? labels[t] : t, s, f, sk, s + f + sk);
                if (f > 0)
                {
                    dgvToday.Rows[ri].Cells["colFail"].Style.ForeColor = Color.FromArgb(231, 76, 60);
                    dgvToday.Rows[ri].Cells["colFail"].Style.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
                }
            }
        }

        #endregion

        #region 카드 클릭 — 대상자 리스트 표시

        private bool _showingDetail = false;

        private void ShowDetailGrid(string title, System.Data.DataTable dt)
        {
            _showingDetail = true;
            lblGridTitle.Text = title + "  (클릭: 요약으로 돌아가기)";
            lblGridTitle.ForeColor = Color.FromArgb(41, 128, 185);
            lblGridTitle.Cursor = Cursors.Hand;
            lblGridTitle.Click -= OnGridTitleClick;
            lblGridTitle.Click += OnGridTitleClick;

            dgvToday.DataSource = null;
            dgvToday.Columns.Clear();
            dgvToday.DataSource = dt;
            dgvToday.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 더블클릭 이벤트 제거 (입주안내 전용은 별도 처리)
            dgvToday.CellDoubleClick -= OnMoveInDoubleClick;
        }

        private void OnGridTitleClick(object sender, EventArgs e)
        {
            if (!_showingDetail) return;
            _showingDetail = false;
            lblGridTitle.Click -= OnGridTitleClick;
            lblGridTitle.Cursor = Cursors.Default;
            dgvToday.CellDoubleClick -= OnMoveInDoubleClick;

            // 원래 요약 그리드로 복원
            dgvToday.DataSource = null;
            dgvToday.Columns.Clear();
            dgvToday.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "유형", Name = "colType", Width = 150 },
                new DataGridViewTextBoxColumn { HeaderText = "성공", Name = "colSuccess", Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(39, 174, 96) } },
                new DataGridViewTextBoxColumn { HeaderText = "실패", Name = "colFail", Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { HeaderText = "SKIP", Name = "colSkip", Width = 100,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(211, 84, 0) } },
                new DataGridViewTextBoxColumn { HeaderText = "합계", Name = "colTotal", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("맑은 고딕", 9F, FontStyle.Bold) } }
            });
            dgvToday.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SafeRefreshDashboard();
        }

        // ── 오늘 발송 카드 ──
        private void OnCardSendClick(object sender, EventArgs e)
        {
            try
            {
                var dt = _logRepo.GetLogTable(DateTime.Today, DateTime.Today);
                ShowDetailGrid("오늘 발송 내역", dt);
            }
            catch (Exception ex) { AppLog.Error("카드:오늘발송 클릭 오류", ex); }
        }

        // ── 미납 대상 카드 ──
        private void OnCardUnpaidClick(object sender, EventArgs e)
        {
            try
            {
                var list = new UnpaidService().GetUnpaidTargets(UserSettings.Current.Unpaid1stDays);
                var dt = new System.Data.DataTable();
                dt.Columns.Add("회사명"); dt.Columns.Add("지점"); dt.Columns.Add("상품");
                dt.Columns.Add("호실"); dt.Columns.Add("청구금액"); dt.Columns.Add("연체일수");
                dt.Columns.Add("이메일");
                foreach (var u in list)
                    dt.Rows.Add(u.CustName, u.BranchName, u.ProductName, u.OfficeNum,
                        u.TotalAmount.ToString("#,0") + "원", u.DaysOverdue + "일", u.Email ?? "(미등록)");
                ShowDetailGrid($"미납 대상 ({list.Count}건)", dt);
            }
            catch (Exception ex) { AppLog.Error("카드:미납 클릭 오류", ex); }
        }

        // ── 갱신 예정 카드 ──
        private void OnCardRenewalClick(object sender, EventArgs e)
        {
            try
            {
                var list = new RenewalManualService().GetRenewalTargets(UserSettings.Current.RenewalManualDays);
                var dt = new System.Data.DataTable();
                dt.Columns.Add("회사명"); dt.Columns.Add("지점"); dt.Columns.Add("상품");
                dt.Columns.Add("호실"); dt.Columns.Add("계약종료일"); dt.Columns.Add("남은일수");
                dt.Columns.Add("이메일");
                foreach (var r in list)
                    dt.Rows.Add(r.CustName, r.BranchName, r.ProductName, r.OfficeNum,
                        r.DateTo, r.DaysUntilExpiry + "일", r.Email ?? "(미등록)");
                ShowDetailGrid($"갱신 예정 ({list.Count}건)", dt);
            }
            catch (Exception ex) { AppLog.Error("카드:갱신 클릭 오류", ex); }
        }

        // ── 퇴실 예정 카드 ──
        private void OnCardMoveOutClick(object sender, EventArgs e)
        {
            try
            {
                var list = new MoveOutService().GetTargetsWithinDays(7);
                var dt = new System.Data.DataTable();
                dt.Columns.Add("회사명"); dt.Columns.Add("지점"); dt.Columns.Add("상품");
                dt.Columns.Add("호실"); dt.Columns.Add("퇴실일"); dt.Columns.Add("이메일");
                foreach (var m in list)
                    dt.Rows.Add(m.CustName, m.BranchName, m.ProductName, m.OfficeNum,
                        m.DateOut, m.Email ?? "(미등록)");
                ShowDetailGrid($"퇴실 예정 ({list.Count}건)", dt);
            }
            catch (Exception ex) { AppLog.Error("카드:퇴실 클릭 오류", ex); }
        }

        // ── 입주 안내 카드 ──
        private List<MoveInInfo> _moveInTargets;

        private void OnCardMoveInClick(object sender, EventArgs e)
        {
            try
            {
                _moveInTargets = new MoveInService().GetTodayTargets();
                var dt = new System.Data.DataTable();
                dt.Columns.Add("회사명"); dt.Columns.Add("지점"); dt.Columns.Add("상품");
                dt.Columns.Add("호실"); dt.Columns.Add("입주일"); dt.Columns.Add("이메일");
                foreach (var m in _moveInTargets)
                    dt.Rows.Add(m.CustName, m.BranchName, m.ProductName, m.OfficeNum,
                        m.DateFrom, m.Email ?? "(미등록)");
                ShowDetailGrid($"입주 안내 대상 ({_moveInTargets.Count}건) — 더블클릭: 복합기 ID/PW 입력 후 발송", dt);
                dgvToday.CellDoubleClick += OnMoveInDoubleClick;
            }
            catch (Exception ex) { AppLog.Error("카드:입주 클릭 오류", ex); }
        }

        private void OnMoveInDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _moveInTargets == null || e.RowIndex >= _moveInTargets.Count) return;
            var info = _moveInTargets[e.RowIndex];

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                MessageBox.Show("이메일이 등록되지 않은 고객입니다.", "발송 불가",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 복합기 ID/PW 입력 폼
            using (var dlg = new Form
            {
                Text = $"입주 발송 — {info.CustName}",
                Size = new Size(380, 220),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false
            })
            {
                var lblId = new Label { Text = "복합기 ID:", Location = new Point(20, 24), AutoSize = true };
                var txtId = new TextBox { Location = new Point(120, 20), Size = new Size(220, 23),
                    Text = UserSettings.Current.PrinterLoginId };
                var lblPw = new Label { Text = "복합기 PW:", Location = new Point(20, 60), AutoSize = true };
                var txtPw = new TextBox { Location = new Point(120, 56), Size = new Size(220, 23),
                    Text = UserSettings.Current.PrinterLoginPw };
                var lblInfo = new Label { Text = $"{info.CustName} / {info.BranchName} / {info.OfficeNum}\n{info.Email}",
                    Location = new Point(20, 96), AutoSize = true, ForeColor = Color.FromArgb(127, 140, 141) };
                var btnOk = new Button { Text = "발송", Location = new Point(140, 140), Size = new Size(90, 32),
                    BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "취소", Location = new Point(240, 140), Size = new Size(90, 32),
                    FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[] { lblId, txtId, lblPw, txtPw, lblInfo, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var pid = txtId.Text.Trim();
                var ppw = txtPw.Text.Trim();

                try
                {
                    var svc = new MoveInService();
                    var result = svc.ProcessOne(info, pid, ppw);
                    if (result.Success)
                    {
                        MessageBox.Show($"'{info.CustName}' 발송 완료!", "성공",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        OnCardMoveInClick(null, null); // 리스트 갱신
                        SafeRefreshDashboard();
                    }
                    else
                    {
                        MessageBox.Show($"결과: {result.Status}\n{result.ErrorMessage}", "결과",
                            MessageBoxButtons.OK, result.Status == "SKIP" ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Error("입주 수동 발송 오류", ex);
                    MessageBox.Show("발송 오류:\n" + ex.Message, "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region 폼 헬퍼

        private void OpenForm<T>() where T : Form, new()
        { using (var f = new T()) f.ShowDialog(this); SafeRefreshDashboard(); }

        private void OpenSettings()
        {
            if (LoginSession.IsLoggedIn && LoginSession.AccountLevel != 1)
            {
                MessageBox.Show("관리자 권한이 필요합니다.\nMOS 관리자 계정으로 로그인해 주세요.",
                    "접근 제한", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var f = new SettingsForm()) f.ShowDialog(this); UserSettings.Reload(); SafeRefreshDashboard();
        }

        #endregion
    }
}
