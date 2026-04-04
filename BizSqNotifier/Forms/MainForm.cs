using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
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
                var p = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
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
        }

        #endregion

        #region 생명주기

        private void OnFormLoad(object sender, EventArgs e)
        {
            lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

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
        }

        /// <summary>7일 이내 퇴실 건수 — 단일 쿼리로 조회 (성능 개선).</summary>
        private int GetMoveOutCountWithin7Days()
        {
            const string sql = @"
SELECT COUNT(*) FROM dbo.tb_movein
WHERE date_out IS NOT NULL AND date_out <> ''
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), TRY_CAST(date_out AS DATE)) BETWEEN 0 AND 7;";
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

        #region 폼 헬퍼

        private void OpenForm<T>() where T : Form, new()
        { using (var f = new T()) f.ShowDialog(this); SafeRefreshDashboard(); }

        private void OpenSettings()
        { using (var f = new SettingsForm()) f.ShowDialog(this); UserSettings.Reload(); SafeRefreshDashboard(); }

        #endregion
    }
}
