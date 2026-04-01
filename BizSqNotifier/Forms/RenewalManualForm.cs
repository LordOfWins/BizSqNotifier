using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 계약 갱신 수동 발송 폼 (오피스).
    ///
    /// 화면 구성:
    ///   상단: 33일 이내 갱신 대상 목록 (DataGridView)
    ///   하단: 수기 입력 폼 + 미리보기 + 발송 버튼
    ///
    /// 워크플로:
    ///   1) 폼 로드 시 자동 조회
    ///   2) 행 선택 → 현재 예치금/이용료 자동 채움
    ///   3) 수기 입력 (변경예치금/변경이용료/회신요청일/참조안내사항)
    ///   4) 미리보기 → HTML 브라우저 팝업
    ///   5) 발송 → 확인 다이얼로그 → SMTP 발송 → 결과 표시
    /// </summary>
    public partial class RenewalManualForm : Form
    {
        private readonly RenewalManualService _service;
        private readonly TemplateEngine _template;
        private List<RenewalInfo> _targets;
        private RenewalInfo _selected;

        public RenewalManualForm()
        {
            InitializeComponent();
            _service = new RenewalManualService();
            _template = new TemplateEngine();
            _targets = new List<RenewalInfo>();

            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnRefresh.Click += OnRefreshClick;
            dgvTargets.SelectionChanged += OnGridSelectionChanged;
            btnPreview.Click += OnPreviewClick;
            btnSend.Click += OnSendClick;
        }

        #endregion

        #region 폼 로드

        private void OnFormLoad(object sender, EventArgs e)
        {
            SetupColumns();
            LoadTargets();
            SetInputEnabled(false);

            // 회신요청일 기본값: 오늘 + 7일
            dtpReplyDeadline.Value = DateTime.Today.AddDays(7);
        }

        #endregion

        #region DataGridView 컬럼 설정

        private void SetupColumns()
        {
            dgvTargets.Columns.Clear();
            dgvTargets.AutoGenerateColumns = false;

            var columns = new[]
            {
                new { Name = "colCustName",    Header = "회사명",       DataProp = "CustName",        Width = 140 },
                new { Name = "colBranch",      Header = "지점",        DataProp = "BranchName",      Width = 90  },
                new { Name = "colProduct",     Header = "상품",        DataProp = "ProductName",     Width = 90  },
                new { Name = "colOffice",      Header = "호실",        DataProp = "OfficeNum",       Width = 70  },
                new { Name = "colDeposit",     Header = "예치금",      DataProp = "DepositDisplay",  Width = 100 },
                new { Name = "colPrice",       Header = "이용료",      DataProp = "PriceDisplay",    Width = 100 },
                new { Name = "colDateTo",      Header = "계약종료일",   DataProp = "DateToDisplay",   Width = 100 },
                new { Name = "colDays",        Header = "D-Day",      DataProp = "DaysUntilExpiry", Width = 60  },
                new { Name = "colEmail",       Header = "이메일",      DataProp = "Email",           Width = 160 }
            };

            foreach (var col in columns)
            {
                dgvTargets.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.Header,
                    DataPropertyName = col.DataProp,
                    Width = col.Width,
                    SortMode = DataGridViewColumnSortMode.Automatic
                });
            }
        }

        #endregion

        #region 데이터 로드

        private void LoadTargets()
        {
            lblStatus.Text = "조회 중...";
            lblStatus.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                _targets = _service.GetRenewalTargets();
                BindGrid();

                lblStatus.Text = $"총 {_targets.Count}건 조회됨";
                lblStatus.ForeColor = Color.FromArgb(41, 128, 185);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "조회 실패: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                AppLog.Error("갱신 대상 조회 실패", ex);
            }
        }

        private void BindGrid()
        {
            // BindingSource에 표시용 래퍼 바인딩
            var displayList = new List<RenewalDisplayRow>();
            foreach (var info in _targets)
            {
                displayList.Add(new RenewalDisplayRow(info));
            }

            dgvTargets.DataSource = null;
            dgvTargets.DataSource = displayList;

            // D-Day 컬럼 색상: 7일 이내 빨강 / 14일 이내 주황
            ColorDayColumn();
        }

        /// <summary>
        /// D-Day 값에 따라 셀 배경색을 지정합니다.
        /// </summary>
        private void ColorDayColumn()
        {
            foreach (DataGridViewRow row in dgvTargets.Rows)
            {
                if (row.DataBoundItem is RenewalDisplayRow dr)
                {
                    var cell = row.Cells["colDays"];
                    if (dr.DaysUntilExpiry <= 7)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 220, 220);
                        cell.Style.ForeColor = Color.FromArgb(192, 57, 43);
                    }
                    else if (dr.DaysUntilExpiry <= 14)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 243, 205);
                        cell.Style.ForeColor = Color.FromArgb(211, 84, 0);
                    }
                }
            }
        }

        #endregion

        #region 행 선택 → 하단 패널 채움

        private void OnGridSelectionChanged(object sender, EventArgs e)
        {
            if (dgvTargets.SelectedRows.Count == 0)
            {
                _selected = null;
                SetInputEnabled(false);
                ClearInputs();
                return;
            }

            var displayRow = dgvTargets.SelectedRows[0].DataBoundItem as RenewalDisplayRow;
            if (displayRow == null) return;

            _selected = displayRow.Source;
            FillCurrentValues(_selected);
            SetInputEnabled(true);
        }

        private void FillCurrentValues(RenewalInfo info)
        {
            lblSelectedInfo.Text = $"{info.CustName} / {info.BranchName} / {info.OfficeNum} / 계약종료: {info.DateTo:yyyy-MM-dd}";
            txtCurrentDeposit.Text = info.Deposit.ToString("#,0") + "원";
            txtCurrentPrice.Text = info.Price.ToString("#,0") + "원";

            // 변경 필드 초기값: 현재 값과 동일하게 채움 (수정 편의)
            txtNewDeposit.Text = info.Deposit.ToString("#,0") + "원";
            txtNewPrice.Text = info.Price.ToString("#,0") + "원";

            // 회신요청일 기본값: 계약종료일 -5일 또는 오늘+7일 중 나중 날짜
            var defaultReply = info.DateTo?.AddDays(-5) ?? DateTime.Today.AddDays(7);
            if (defaultReply < DateTime.Today) defaultReply = DateTime.Today.AddDays(7);
            dtpReplyDeadline.Value = defaultReply;

            txtReferenceNote.Text = string.Empty;
        }

        private void ClearInputs()
        {
            lblSelectedInfo.Text = "(목록에서 항목을 선택하세요)";
            txtCurrentDeposit.Text = string.Empty;
            txtCurrentPrice.Text = string.Empty;
            txtNewDeposit.Text = string.Empty;
            txtNewPrice.Text = string.Empty;
            dtpReplyDeadline.Value = DateTime.Today.AddDays(7);
            txtReferenceNote.Text = string.Empty;
        }

        private void SetInputEnabled(bool enabled)
        {
            txtNewDeposit.Enabled = enabled;
            txtNewPrice.Enabled = enabled;
            dtpReplyDeadline.Enabled = enabled;
            txtReferenceNote.Enabled = enabled;
            btnPreview.Enabled = enabled;
            btnSend.Enabled = enabled;
        }

        #endregion

        #region 미리보기

        private void OnPreviewClick(object sender, EventArgs e)
        {
            if (_selected == null) return;

            var info = BuildSendInfo();
            var tokens = BuildPreviewTokens(info);

            var subjectTemplate = TemplateEngine.GetDefaultSubject(MailTypes.RenewalManual);
            var subject = _template.RenderSubject(subjectTemplate, tokens);
            var body = _template.LoadAndRender(TemplateFiles.RenewalOffice, tokens);

            if (string.IsNullOrEmpty(body))
            {
                MessageBox.Show("템플릿 파일(RenewalOffice.html)을 찾을 수 없습니다.",
                    "템플릿 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 미리보기 팝업
            using (var preview = new Form())
            {
                preview.Text = "미리보기 — " + subject;
                preview.Size = new Size(720, 600);
                preview.StartPosition = FormStartPosition.CenterParent;

                var browser = new WebBrowser
                {
                    Dock = DockStyle.Fill,
                    DocumentText = body,
                    ScriptErrorsSuppressed = true
                };
                preview.Controls.Add(browser);
                preview.ShowDialog(this);
            }
        }

        #endregion

        #region 발송

        private void OnSendClick(object sender, EventArgs e)
        {
            if (_selected == null) return;

            var info = BuildSendInfo();

            // 확인 다이얼로그
            var confirmMsg = $"다음 내용으로 갱신 안내 메일을 발송합니다.\n\n" +
                             $"수신: {info.CustName} ({info.Email})\n" +
                             $"변경 예치금: {info.NewDeposit}\n" +
                             $"변경 이용료: {info.NewPrice}\n" +
                             $"회신 요청일: {info.ReplyDeadline}\n\n" +
                             $"발송하시겠습니까?";

            var confirm = MessageBox.Show(confirmMsg, "발송 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnSend.Enabled = false;
            lblStatus.Text = "발송 중...";
            lblStatus.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                var result = _service.SendOne(info);

                if (result.Success)
                {
                    lblStatus.Text = $"발송 완료 — {info.CustName}";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                    MessageBox.Show($"{info.CustName}에게 갱신 안내 메일을 발송했습니다.",
                        "발송 성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (result.Status == "SKIP")
                {
                    lblStatus.Text = $"SKIP — {result.ErrorMessage}";
                    lblStatus.ForeColor = Color.FromArgb(211, 84, 0);
                    MessageBox.Show($"발송 건너뜀: {result.ErrorMessage}",
                        "SKIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    lblStatus.Text = $"발송 실패 — {result.ErrorMessage}";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show($"발송 실패: {result.ErrorMessage}",
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "발송 오류: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                AppLog.Error("[갱신수동] 발송 오류", ex);
                MessageBox.Show("발송 중 오류가 발생했습니다.\n" + ex.Message,
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

        #endregion

        #region 새로고침

        private void OnRefreshClick(object sender, EventArgs e)
        {
            LoadTargets();
            ClearInputs();
            SetInputEnabled(false);
            _selected = null;
        }

        #endregion

        #region 헬퍼

        /// <summary>
        /// 현재 선택된 행 + 수기 입력값을 합쳐 발송용 RenewalInfo를 구성합니다.
        /// </summary>
        private RenewalInfo BuildSendInfo()
        {
            // _selected의 DB 값은 유지하고 수기 입력값만 추가
            _selected.NewDeposit = txtNewDeposit.Text.Trim();
            _selected.NewPrice = txtNewPrice.Text.Trim();
            _selected.ReplyDeadline = dtpReplyDeadline.Value.ToString("yyyy-MM-dd");
            _selected.ReferenceNote = txtReferenceNote.Text.Trim();
            return _selected;
        }

        /// <summary>
        /// 미리보기 전용 토큰 (BuildSendInfo 결과 기반).
        /// </summary>
        private static Dictionary<string, string> BuildPreviewTokens(RenewalInfo info)
        {
            return new Dictionary<string, string>
            {
                ["회사명"]       = info.CustName ?? string.Empty,
                ["지점"]         = info.BranchName ?? string.Empty,
                ["상품/분류"]    = info.ProductName ?? string.Empty,
                ["호실"]         = info.OfficeNum ?? string.Empty,
                ["예치금"]       = info.Deposit.ToString("#,0") + "원",
                ["임대료"]       = info.Price.ToString("#,0") + "원",
                ["계약종료일"]   = info.DateTo?.ToString("yyyy-MM-dd") ?? string.Empty,
                ["납부계좌"]     = info.BankAccount ?? string.Empty,
                ["예금주"]       = info.BankHolder ?? string.Empty,
                ["변경예치금"]   = info.NewDeposit ?? string.Empty,
                ["변경이용료"]   = info.NewPrice ?? string.Empty,
                ["회신요청일"]   = info.ReplyDeadline ?? string.Empty,
                ["참조안내사항"] = info.ReferenceNote ?? string.Empty
            };
        }

        #endregion
    }

    #region DataGridView 표시용 래퍼 클래스

    /// <summary>
    /// RenewalInfo를 DataGridView에 표시하기 위한 래퍼.
    /// 금액 포맷 / 날짜 포맷 등 표시용 속성을 제공합니다.
    /// </summary>
    internal sealed class RenewalDisplayRow
    {
        private readonly RenewalInfo _src;

        public RenewalDisplayRow(RenewalInfo source)
        {
            _src = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>원본 데이터 접근용</summary>
        public RenewalInfo Source => _src;

        // DataGridView DataPropertyName 매핑
        public string CustName => _src.CustName;
        public string BranchName => _src.BranchName;
        public string ProductName => _src.ProductName;
        public string OfficeNum => _src.OfficeNum;
        public string DepositDisplay => _src.Deposit.ToString("#,0") + "원";
        public string PriceDisplay => _src.Price.ToString("#,0") + "원";
        public string DateToDisplay => _src.DateTo?.ToString("yyyy-MM-dd") ?? "";
        public int DaysUntilExpiry => _src.DaysUntilExpiry;
        public string Email => _src.Email;
    }

    #endregion
}
