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
    /// <summary>
    /// 관리자 설정 화면.
    ///
    /// 탭 구성:
    ///   [복합기 설정] 복합기 ID/PW 입력 → 입주 안내 메일에 치환
    ///   [발송 스케줄] 발송 시각(2종) + 기준일(6종) 설정
    ///   [연결 테스트] DB 연결 테스트 + 지점별 SMTP 테스트
    ///
    /// 설정 저장: settings.json (UserSettings 클래스 기반)
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly BranchRepository _branchRepo;
        private readonly SmtpService _smtp;
        private List<BranchSmtpInfo> _branches;

        public SettingsForm()
        {
            InitializeComponent();
            _branchRepo = new BranchRepository();
            _smtp = new SmtpService();
            _branches = new List<BranchSmtpInfo>();

            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnSave.Click += OnSaveClick;
            btnCancel.Click += OnCancelClick;
            btnTestDb.Click += OnTestDbClick;
            btnTestSmtp.Click += OnTestSmtpClick;
        }

        #endregion

        #region 폼 로드 — 설정값 바인딩

        private void OnFormLoad(object sender, EventArgs e)
        {
            var s = UserSettings.Current;

            // 복합기 탭
            txtPrinterId.Text = s.PrinterLoginId;
            txtPrinterPw.Text = s.PrinterLoginPw;

            // 발송 시각
            dtpGeneralTime.Value = DateTime.Today.Add(s.GeneralSendTimeSpan);
            dtpUnpaidTime.Value = DateTime.Today.Add(s.UnpaidSendTimeSpan);

            // 기준일
            nudUnpaid1st.Value = Clamp(s.Unpaid1stDays, nudUnpaid1st);
            nudUnpaid2nd.Value = Clamp(s.Unpaid2ndDays, nudUnpaid2nd);
            nudUnpaidFinal.Value = Clamp(s.UnpaidFinalDays, nudUnpaidFinal);
            nudRenewalAuto.Value = Clamp(s.RenewalAutoDays, nudRenewalAuto);
            nudRenewalManual.Value = Clamp(s.RenewalManualDays, nudRenewalManual);
            nudMoveOut.Value = Clamp(s.MoveOutDays, nudMoveOut);

            // 지점 콤보박스 로드
            LoadBranches();
        }

        #endregion

        #region 지점 콤보박스

        private void LoadBranches()
        {
            try
            {
                _branches = _branchRepo.GetAll();
                cmbBranch.Items.Clear();

                foreach (var br in _branches)
                {
                    cmbBranch.Items.Add($"{br.BranchName} (ID: {br.BranchId})");
                }

                if (cmbBranch.Items.Count > 0)
                    cmbBranch.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLog.Error("지점 목록 로드 실패", ex);
                cmbBranch.Items.Clear();
                cmbBranch.Items.Add("(로드 실패)");
            }
        }

        #endregion

        #region 저장

        private void OnSaveClick(object sender, EventArgs e)
        {
            // 기준일 유효성 검증: 미납 1차 < 2차 < 최종
            if (nudUnpaid1st.Value >= nudUnpaid2nd.Value || nudUnpaid2nd.Value >= nudUnpaidFinal.Value)
            {
                MessageBox.Show("미납 기준일은 1차 < 2차 < 최종 순서로 설정해야 합니다.",
                    "검증 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabSchedule;
                return;
            }

            try
            {
                var s = UserSettings.Current;

                // 복합기
                s.PrinterLoginId = txtPrinterId.Text.Trim();
                s.PrinterLoginPw = txtPrinterPw.Text.Trim();

                // 발송 시각
                s.GeneralSendTime = dtpGeneralTime.Value.ToString("HH:mm");
                s.UnpaidSendTime = dtpUnpaidTime.Value.ToString("HH:mm");

                // 기준일
                s.Unpaid1stDays = (int)nudUnpaid1st.Value;
                s.Unpaid2ndDays = (int)nudUnpaid2nd.Value;
                s.UnpaidFinalDays = (int)nudUnpaidFinal.Value;
                s.RenewalAutoDays = (int)nudRenewalAuto.Value;
                s.RenewalManualDays = (int)nudRenewalManual.Value;
                s.MoveOutDays = (int)nudMoveOut.Value;

                s.Save();

                lblSaveStatus.Text = "저장 완료 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                lblSaveStatus.ForeColor = Color.FromArgb(39, 174, 96);

                MessageBox.Show("설정이 저장되었습니다.", "저장 완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblSaveStatus.Text = "저장 실패";
                lblSaveStatus.ForeColor = Color.Red;
                AppLog.Error("설정 저장 실패", ex);
                MessageBox.Show("설정 저장 중 오류가 발생했습니다.\n" + ex.Message,
                    "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        #region DB 연결 테스트

        private void OnTestDbClick(object sender, EventArgs e)
        {
            btnTestDb.Enabled = false;
            lblDbResult.Text = "테스트 중...";
            lblDbResult.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                string errorMsg;
                bool ok = DbManager.TestConnection(out errorMsg);

                if (ok)
                {
                    lblDbResult.Text = "연결 성공";
                    lblDbResult.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else
                {
                    lblDbResult.Text = "연결 실패: " + (errorMsg ?? "알 수 없는 오류");
                    lblDbResult.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblDbResult.Text = "오류: " + ex.Message;
                lblDbResult.ForeColor = Color.Red;
            }
            finally
            {
                btnTestDb.Enabled = true;
            }
        }

        #endregion

        #region SMTP 연결 테스트

        private void OnTestSmtpClick(object sender, EventArgs e)
        {
            if (cmbBranch.SelectedIndex < 0 || cmbBranch.SelectedIndex >= _branches.Count)
            {
                lblSmtpResult.Text = "지점을 선택해 주세요.";
                lblSmtpResult.ForeColor = Color.FromArgb(211, 84, 0);
                return;
            }

            var branch = _branches[cmbBranch.SelectedIndex];

            btnTestSmtp.Enabled = false;
            lblSmtpResult.Text = $"'{branch.BranchName}' SMTP 테스트 중...";
            lblSmtpResult.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                var result = _smtp.TestConnection(branch.BranchId);

                if (result.Success)
                {
                    lblSmtpResult.Text = $"'{branch.BranchName}' SMTP 연결 성공 (테스트 메일 발송됨)";
                    lblSmtpResult.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else
                {
                    lblSmtpResult.Text = $"실패: {result.ErrorMessage}";
                    lblSmtpResult.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblSmtpResult.Text = "오류: " + ex.Message;
                lblSmtpResult.ForeColor = Color.Red;
            }
            finally
            {
                btnTestSmtp.Enabled = true;
            }
        }

        private void BtnRegisterTasks_Click(object sender, EventArgs e)
        {
            var generalTime = UserSettings.Current.GeneralSendTime;
            var unpaidTime = UserSettings.Current.UnpaidSendTime;

            var confirm = MessageBox.Show(
                $"다음 작업을 Windows 작업 스케줄러에 등록합니다:\n\n" +
                $"  ① 매일 {generalTime} → 입주/퇴실/갱신자동 발송\n" +
                $"  ② 매일 {unpaidTime} → 미납 1차/2차/최종 발송\n" +
                $"  ③ 로그온 시 → 트레이 아이콘 자동 시작\n\n" +
                "관리자 권한이 필요할 수 있습니다. 계속하시겠습니까?",
                "작업 스케줄러 등록",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var result = TaskSchedulerHelper.RegisterAll(generalTime, unpaidTime);

            lblSchedulerStatus.Text = result.Success ? "등록 완료" : "일부 실패";
            lblSchedulerStatus.ForeColor = result.Success
                ? Color.FromArgb(39, 174, 96)
                : Color.FromArgb(231, 76, 60);

            MessageBox.Show(
                result.Message,
                "작업 스케줄러 등록 결과",
                MessageBoxButtons.OK,
                result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnRemoveTasks_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "등록된 모든 BizSqNotifier 작업 스케줄러를 해제합니다.\n계속하시겠습니까?",
                "작업 스케줄러 해제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            TaskSchedulerHelper.RemoveAll();

            lblSchedulerStatus.Text = "해제 완료";
            lblSchedulerStatus.ForeColor = Color.FromArgb(127, 140, 141);

            MessageBox.Show("모든 작업이 해제되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshSchedulerStatus()
        {
            bool has09 = TaskSchedulerHelper.IsTaskRegistered("BizSqNotifier_09");
            bool has13 = TaskSchedulerHelper.IsTaskRegistered("BizSqNotifier_13");
            bool hasStartup = TaskSchedulerHelper.IsTaskRegistered("BizSqNotifier_Startup");

            if (has09 && has13 && hasStartup)
            {
                lblSchedulerStatus.Text = "전체 등록됨";
                lblSchedulerStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else if (has09 || has13 || hasStartup)
            {
                lblSchedulerStatus.Text = "일부 등록됨";
                lblSchedulerStatus.ForeColor = Color.FromArgb(211, 84, 0);
            }
            else
            {
                lblSchedulerStatus.Text = "미등록";
                lblSchedulerStatus.ForeColor = Color.FromArgb(231, 76, 60);
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// NumericUpDown의 Min/Max 범위 내로 값을 제한합니다.
        /// </summary>
        private static decimal Clamp(int value, NumericUpDown nud)
        {
            if (value < (int)nud.Minimum) return nud.Minimum;
            if (value > (int)nud.Maximum) return nud.Maximum;
            return value;
        }

        #endregion
    }
}
