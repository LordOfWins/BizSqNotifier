using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 메인 대시보드.
    ///
    /// 상단: 요약 카드 4개 (오늘 발송 / 미납 건수 / 갱신 예정 / 퇴실 예정)
    /// 중앙: 오늘 발송 현황 DataGridView (유형별 성공/실패/SKIP)
    /// 하단 메뉴 버튼: 갱신목록 / 수동발송 / 발송현황 / 설정
    ///
    /// 자동 새로고침: 5분마다 대시보드 갱신
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly MailLogRepository _logRepo;
        private readonly Timer _refreshTimer;

        public MainForm()
        {
            InitializeComponent();
            _logRepo = new MailLogRepository();

            // 5분 자동 새로고침
            _refreshTimer = new Timer { Interval = 300_000 };
            _refreshTimer.Tick += (s, e) => RefreshDashboard();

            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;

            btnRenewalList.Click += (s, e) => OpenForm<RenewalListForm>();
            btnManualSend.Click += (s, e) => OpenForm<RenewalManualForm>();
            btnLogView.Click += (s, e) => OpenForm<LogViewForm>();
            btnSettings.Click += (s, e) => OpenSettings();
            btnRefresh.Click += (s, e) => RefreshDashboard();
        }

        #endregion

        #region 폼 로드

        private void OnFormLoad(object sender, EventArgs e)
        {
            lblVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly()
                .GetName().Version.ToString(3);

            RefreshDashboard();
            _refreshTimer.Start();
        }

        #endregion

        #region 폼 종료

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
        }

        #endregion

        #region 대시보드 갱신

        private void RefreshDashboard()
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

        #endregion

        #region 요약 카드 갱신

        private void RefreshSummaryCards()
        {
            // ① 오늘 발송 건수 (성공)
            var todaySummary = _logRepo.GetTodaySummary();
            int totalSuccess = 0;
            int totalFail = 0;
            foreach (var kv in todaySummary)
            {
                totalSuccess += kv.Value.Success;
                totalFail += kv.Value.Fail;
            }
            lblCardSendCount.Text = totalSuccess.ToString();
            lblCardSendSub.Text = totalFail > 0
                ? $"실패 {totalFail}건"
                : "정상";
            lblCardSendSub.ForeColor = totalFail > 0
                ? Color.FromArgb(231, 76, 60)
                : Color.FromArgb(39, 174, 96);

            // ② 미납 건수 (현재 미납 대상)
            try
            {
                var unpaidService = new UnpaidService();
                var unpaidList = unpaidService.GetUnpaidTargets(
                    UserSettings.Current.Unpaid1stDays);
                lblCardUnpaidCount.Text = unpaidList.Count.ToString();
                lblCardUnpaidSub.Text = unpaidList.Count > 0 ? "조속히 처리 필요" : "미납 없음";
                lblCardUnpaidSub.ForeColor = unpaidList.Count > 0
                    ? Color.FromArgb(211, 84, 0)
                    : Color.FromArgb(39, 174, 96);
            }
            catch
            {
                lblCardUnpaidCount.Text = "-";
                lblCardUnpaidSub.Text = "조회 실패";
                lblCardUnpaidSub.ForeColor = Color.Red;
            }

            // ③ 갱신 예정 건수 (오피스 33일 + adBox 15일)
            try
            {
                var manualService = new RenewalManualService();
                var officeList = manualService.GetRenewalTargets(
                    UserSettings.Current.RenewalManualDays);
                lblCardRenewalCount.Text = officeList.Count.ToString();
                lblCardRenewalSub.Text = officeList.Count > 0 ? "갱신 안내 필요" : "예정 없음";
                lblCardRenewalSub.ForeColor = officeList.Count > 0
                    ? Color.FromArgb(41, 128, 185)
                    : Color.FromArgb(127, 140, 141);
            }
            catch
            {
                lblCardRenewalCount.Text = "-";
                lblCardRenewalSub.Text = "조회 실패";
                lblCardRenewalSub.ForeColor = Color.Red;
            }

            // ④ 퇴실 예정 건수 (7일 이내)
            try
            {
                var moveOutService = new MoveOutService();
                var moveOutList = moveOutService.GetTargets(7);
                // GetTargets는 = 조건이므로 7일 이내 범위 조회 필요
                // 간이로 개별 조회
                int moveOutCount = 0;
                for (int d = 0; d <= 7; d++)
                {
                    moveOutCount += moveOutService.GetTargets(d).Count;
                }
                lblCardMoveOutCount.Text = moveOutCount.ToString();
                lblCardMoveOutSub.Text = moveOutCount > 0 ? "7일 이내" : "예정 없음";
                lblCardMoveOutSub.ForeColor = moveOutCount > 0
                    ? Color.FromArgb(142, 68, 173)
                    : Color.FromArgb(127, 140, 141);
            }
            catch
            {
                lblCardMoveOutCount.Text = "-";
                lblCardMoveOutSub.Text = "조회 실패";
                lblCardMoveOutSub.ForeColor = Color.Red;
            }
        }

        #endregion

        #region 오늘 발송 현황 그리드

        private void RefreshTodayGrid()
        {
            var todaySummary = _logRepo.GetTodaySummary();

            dgvToday.Rows.Clear();

            // 모든 유형 순서대로 표시
            var types = new[]
            {
                MailTypes.MoveIn, MailTypes.Unpaid1st, MailTypes.Unpaid2nd,
                MailTypes.UnpaidFinal, MailTypes.RenewalManual,
                MailTypes.RenewalAuto, MailTypes.MoveOut
            };

            var typeLabels = new Dictionary<string, string>
            {
                [MailTypes.MoveIn] = "입주 안내",
                [MailTypes.Unpaid1st] = "미납 1차",
                [MailTypes.Unpaid2nd] = "미납 2차",
                [MailTypes.UnpaidFinal] = "미납 최종",
                [MailTypes.RenewalManual] = "갱신 수동",
                [MailTypes.RenewalAuto] = "갱신 자동",
                [MailTypes.MoveOut] = "퇴실 안내"
            };

            foreach (var type in types)
            {
                int s = 0, f = 0, sk = 0;
                if (todaySummary.ContainsKey(type))
                {
                    var v = todaySummary[type];
                    s = v.Success;
                    f = v.Fail;
                    sk = v.Skip;
                }

                var rowIndex = dgvToday.Rows.Add(
                    typeLabels.ContainsKey(type) ? typeLabels[type] : type,
                    s, f, sk, s + f + sk);

                // 실패 건 빨간 표시
                if (f > 0)
                {
                    dgvToday.Rows[rowIndex].Cells["colFail"].Style.ForeColor =
                        Color.FromArgb(231, 76, 60);
                    dgvToday.Rows[rowIndex].Cells["colFail"].Style.Font =
                        new Font("맑은 고딕", 9F, FontStyle.Bold);
                }
            }
        }

        #endregion

        #region 폼 열기 헬퍼

        private void OpenForm<T>() where T : Form, new()
        {
            using (var form = new T())
            {
                form.ShowDialog(this);
            }
            // 폼 닫힌 후 대시보드 갱신
            RefreshDashboard();
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm())
            {
                form.ShowDialog(this);
            }
            // 설정 변경 반영을 위해 UserSettings 리로드
            UserSettings.Reload();
            RefreshDashboard();
        }

        #endregion
    }
}
