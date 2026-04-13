using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Services;
using BizSqNotifier.Models;

namespace BizSqNotifier
{
    /// <summary>관리자 설정 화면.</summary>
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

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnSave.Click += OnSaveClick;
            btnCancel.Click += OnCancelClick;
            btnTestDb.Click += OnTestDbClick;
            btnTestSmtp.Click += OnTestSmtpClick;
            btnRegisterTasks.Click += BtnRegisterTasks_Click;
            btnRemoveTasks.Click += BtnRemoveTasks_Click;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            var s = UserSettings.Current;
            chkAutoSend.Checked = s.AutoSendEnabled;
            txtPrinterId.Text = s.PrinterLoginId;
            txtPrinterPw.Text = s.PrinterLoginPw;
            dtpGeneralTime.Value = DateTime.Today.Add(s.GeneralSendTimeSpan);
            dtpUnpaidTime.Value = DateTime.Today.Add(s.UnpaidSendTimeSpan);
            nudUnpaid1st.Value = Clamp(s.Unpaid1stDays, nudUnpaid1st);
            nudUnpaid2nd.Value = Clamp(s.Unpaid2ndDays, nudUnpaid2nd);
            nudUnpaidFinal.Value = Clamp(s.UnpaidFinalDays, nudUnpaidFinal);
            nudRenewalAuto.Value = Clamp(s.RenewalAutoDays, nudRenewalAuto);
            nudRenewalManual.Value = Clamp(s.RenewalManualDays, nudRenewalManual);
            nudMoveOut.Value = Clamp(s.MoveOutDays, nudMoveOut);
            LoadBranches();
        }

        private void LoadBranches()
        {
            try
            {
                _branches = _branchRepo.GetAll();
                cmbBranch.Items.Clear();
                foreach (var br in _branches)
                    cmbBranch.Items.Add($"{br.BranchName} ({br.BranchCode})");
                if (cmbBranch.Items.Count > 0) cmbBranch.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppLog.Error("지점 목록 로드 실패", ex);
                cmbBranch.Items.Clear();
                cmbBranch.Items.Add("(로드 실패)");
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
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
                s.AutoSendEnabled = chkAutoSend.Checked;
                s.PrinterLoginId = txtPrinterId.Text.Trim();
                s.PrinterLoginPw = txtPrinterPw.Text.Trim();
                s.GeneralSendTime = dtpGeneralTime.Value.ToString("HH:mm");
                s.UnpaidSendTime = dtpUnpaidTime.Value.ToString("HH:mm");
                s.Unpaid1stDays = (int)nudUnpaid1st.Value;
                s.Unpaid2ndDays = (int)nudUnpaid2nd.Value;
                s.UnpaidFinalDays = (int)nudUnpaidFinal.Value;
                s.RenewalAutoDays = (int)nudRenewalAuto.Value;
                s.RenewalManualDays = (int)nudRenewalManual.Value;
                s.MoveOutDays = (int)nudMoveOut.Value;
                s.Save();

                lblSaveStatus.Text = "저장 완료 (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                lblSaveStatus.ForeColor = Color.FromArgb(39, 174, 96);
                MessageBox.Show("설정이 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblSaveStatus.Text = "저장 실패";
                lblSaveStatus.ForeColor = Color.Red;
                AppLog.Error("설정 저장 실패", ex);
                MessageBox.Show("저장 실패:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object sender, EventArgs e)
        { this.DialogResult = DialogResult.Cancel; this.Close(); }

        private void OnTestDbClick(object sender, EventArgs e)
        {
            btnTestDb.Enabled = false;
            lblDbResult.Text = "테스트 중...";
            lblDbResult.ForeColor = Color.Gray;
            Application.DoEvents();
            try
            {
                string err;
                if (DbManager.TestConnection(out err))
                { lblDbResult.Text = "연결 성공"; lblDbResult.ForeColor = Color.FromArgb(39, 174, 96); }
                else
                { lblDbResult.Text = "실패: " + (err ?? "?"); lblDbResult.ForeColor = Color.Red; }
            }
            catch (Exception ex) { lblDbResult.Text = "오류: " + ex.Message; lblDbResult.ForeColor = Color.Red; }
            finally { btnTestDb.Enabled = true; }
        }

        private void OnTestSmtpClick(object sender, EventArgs e)
        {
            if (cmbBranch.SelectedIndex < 0 || cmbBranch.SelectedIndex >= _branches.Count)
            { lblSmtpResult.Text = "지점 선택 필요"; lblSmtpResult.ForeColor = Color.FromArgb(211, 84, 0); return; }

            var branch = _branches[cmbBranch.SelectedIndex];
            btnTestSmtp.Enabled = false;
            lblSmtpResult.Text = $"'{branch.BranchName}' SMTP 테스트 중...";
            lblSmtpResult.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                var result = _smtp.TestConnection(branch.BranchCode);
                if (result.Success)
                { lblSmtpResult.Text = $"'{branch.BranchName}' 성공"; lblSmtpResult.ForeColor = Color.FromArgb(39, 174, 96); }
                else
                { lblSmtpResult.Text = $"실패: {result.ErrorMessage}"; lblSmtpResult.ForeColor = Color.Red; }
            }
            catch (Exception ex) { lblSmtpResult.Text = "오류: " + ex.Message; lblSmtpResult.ForeColor = Color.Red; }
            finally { btnTestSmtp.Enabled = true; }
        }

        private void BtnRegisterTasks_Click(object sender, EventArgs e)
        {
            var gt = UserSettings.Current.GeneralSendTime;
            var ut = UserSettings.Current.UnpaidSendTime;
            if (MessageBox.Show($"등록:\n① 매일 {gt} 입주/퇴실/갱신\n② 매일 {ut} 미납\n③ 로그온 시 트레이\n\n계속?",
                "스케줄러 등록", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var r = TaskSchedulerHelper.RegisterAll(gt, ut);
            lblSchedulerStatus.Text = r.Success ? "등록 완료" : "일부 실패";
            lblSchedulerStatus.ForeColor = r.Success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
            MessageBox.Show(r.Message, "결과", MessageBoxButtons.OK,
                r.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnRemoveTasks_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("모든 작업 해제?", "해제", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            TaskSchedulerHelper.RemoveAll();
            lblSchedulerStatus.Text = "해제 완료";
            lblSchedulerStatus.ForeColor = Color.FromArgb(127, 140, 141);
            MessageBox.Show("해제 완료", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static decimal Clamp(int v, NumericUpDown n)
        { if (v < (int)n.Minimum) return n.Minimum; if (v > (int)n.Maximum) return n.Maximum; return v; }
    }
}
