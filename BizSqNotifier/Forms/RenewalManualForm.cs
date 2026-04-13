using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>오피스 수동 갱신 발송 폼.</summary>
    public partial class RenewalManualForm : Form
    {
        private readonly RenewalManualService _svc;
        private List<RenewalInfo> _targets;

        public RenewalManualForm()
        {
            InitializeComponent();
            _svc = new RenewalManualService();
            _targets = new List<RenewalInfo>();
            WireEvents();
        }

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnRefresh.Click += (s, e) => LoadTargets();
            dgvTargets.SelectionChanged += OnSelectionChanged;
            btnPreview.Click += OnPreviewClick;
            btnSend.Click += OnSendClick;
        }

        private void OnFormLoad(object sender, EventArgs e) { LoadTargets(); }

        private void LoadTargets()
        {
            try
            {
                _targets = _svc.GetRenewalTargets(UserSettings.Current.RenewalManualDays);
                dgvTargets.Rows.Clear();

                foreach (var t in _targets)
                {
                    dgvTargets.Rows.Add(
                        t.CustName, t.BranchName, t.ProductName,
                        t.OfficeNum, t.DateTo,
                        t.DaysUntilExpiry.ToString() + "일",
                        t.Deposit.ToString("#,0"), t.Price.ToString("#,0"),
                        t.Email ?? "(미등록)");
                }

                lblStatus.Text = $"총 {_targets.Count}건";
                lblStatus.ForeColor = Color.FromArgb(127, 140, 141);
            }
            catch (Exception ex)
            {
                AppLog.Error("갱신 목록 로드 실패", ex);
                MessageBox.Show("목록 로드 실패:\n" + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (dgvTargets.SelectedRows.Count == 0) return;
            var idx = dgvTargets.SelectedRows[0].Index;
            if (idx >= _targets.Count) return;

            var info = _targets[idx];

            // lblSelectedInfo: 선택 정보 한 줄로 표시
            lblSelectedInfo.Text = $"{info.CustName} / {info.BranchName} / {info.OfficeNum}";
            lblSelectedInfo.ForeColor = Color.FromArgb(41, 128, 185);

            // 현재 예치금/이용료 (읽기 전용)
            txtCurrentDeposit.Text = info.Deposit.ToString("#,0");
            txtCurrentPrice.Text = info.Price.ToString("#,0");

            // 변경 예치금/이용료 기본값
            txtNewDeposit.Text = info.Deposit.ToString("#,0");
            txtNewPrice.Text = info.Price.ToString("#,0");

            // 회신요청일 = 계약종료일 - 7일 (DateTimePicker)
            if (DateTime.TryParse(info.DateTo, out var dt))
                dtpReplyDeadline.Value = dt.AddDays(-7) < DateTime.Today ? DateTime.Today : dt.AddDays(-7);

            txtReferenceNote.Text = "";
        }

        private void OnPreviewClick(object sender, EventArgs e)
        {
            if (dgvTargets.SelectedRows.Count == 0)
            {
                MessageBox.Show("미리볼 건을 선택해 주세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var idx = dgvTargets.SelectedRows[0].Index;
            if (idx >= _targets.Count) return;

            var info = _targets[idx];

            // 수기 입력값 반영
            info.NewDeposit = txtNewDeposit.Text.Trim();
            info.NewPrice = txtNewPrice.Text.Trim();
            info.ReplyDeadline = dtpReplyDeadline.Value.ToString("yyyy-MM-dd");
            info.ReferenceNote = txtReferenceNote.Text.Trim();

            try
            {
                var template = new TemplateEngine();
                var tokens = new Dictionary<string, string>
                {
                    ["회사명"]       = info.CustName ?? "",
                    ["지점"]         = info.BranchName ?? "",
                    ["상품/분류"]    = info.ProductName ?? "",
                    ["호실"]         = info.OfficeNum ?? "",
                    ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                    ["임대료"]       = info.Price.ToString("#,0") + "원",
                    ["계약종료일"]   = info.DateTo ?? "",
                    ["납부계좌"]     = info.BankAccount ?? "",
                    ["예금주"]       = info.BankHolder ?? "",
                    ["변경예치금"]   = info.NewDeposit ?? "",
                    ["변경이용료"]   = info.NewPrice ?? "",
                    ["회신요청일"]   = info.ReplyDeadline ?? "",
                    ["참조안내사항"] = info.ReferenceNote ?? ""
                };

                var body = template.LoadAndRender(TemplateFiles.RenewalOffice, tokens);
                if (string.IsNullOrEmpty(body))
                {
                    MessageBox.Show("템플릿 로드 실패", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // WebBrowser 기반 미리보기 폼
                var previewForm = new Form
                {
                    Text = $"미리보기 — {info.CustName}",
                    Size = new System.Drawing.Size(750, 700),
                    StartPosition = FormStartPosition.CenterParent,
                    MinimumSize = new System.Drawing.Size(600, 400)
                };

                var browser = new WebBrowser
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    ScriptErrorsSuppressed = true
                };

                previewForm.Controls.Add(browser);
                previewForm.Show(this);
                browser.DocumentText = body;
            }
            catch (Exception ex)
            {
                AppLog.Error("미리보기 오류", ex);
                MessageBox.Show("미리보기 오류:\n" + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSendClick(object sender, EventArgs e)
        {
            if (dgvTargets.SelectedRows.Count == 0)
            {
                MessageBox.Show("발송할 건을 선택해 주세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var idx = dgvTargets.SelectedRows[0].Index;
            if (idx >= _targets.Count) return;

            var info = _targets[idx];

            // 수기 입력값 매핑
            info.NewDeposit = txtNewDeposit.Text.Trim();
            info.NewPrice = txtNewPrice.Text.Trim();
            info.ReplyDeadline = dtpReplyDeadline.Value.ToString("yyyy-MM-dd");
            info.ReferenceNote = txtReferenceNote.Text.Trim();

            if (string.IsNullOrWhiteSpace(info.Email))
            {
                MessageBox.Show("이메일이 등록되지 않은 고객입니다.", "발송 불가",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"[{info.CustName}] ({info.Email})\n갱신 안내 메일을 발송하시겠습니까?",
                "발송 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnSend.Enabled = false;
            lblStatus.Text = "발송 중...";
            lblStatus.ForeColor = Color.Gray;

            try
            {
                var result = _svc.SendOne(info);

                if (result.Success)
                {
                    lblStatus.Text = $"'{info.CustName}' 발송 완료!";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                    MessageBox.Show($"'{info.CustName}' 발송 완료!", "성공",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTargets();
                }
                else
                {
                    lblStatus.Text = $"결과: {result.Status}";
                    lblStatus.ForeColor = result.Status == "SKIP"
                        ? Color.FromArgb(211, 84, 0) : Color.Red;
                    MessageBox.Show($"결과: {result.Status}\n{result.ErrorMessage}",
                        "결과", MessageBoxButtons.OK,
                        result.Status == "SKIP" ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "발송 오류";
                lblStatus.ForeColor = Color.Red;
                AppLog.Error("수동 발송 오류", ex);
                MessageBox.Show("발송 오류:\n" + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btnSend.Enabled = true; }
        }
    }
}
