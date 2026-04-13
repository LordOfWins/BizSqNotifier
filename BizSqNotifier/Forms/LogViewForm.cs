using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using BizSqNotifier.Data;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 발송 현황 조회 화면.
    ///
    /// 기능:
    ///   - 기간별 / 유형별 / 상태별 / 회사명 / 지점별 검색
    ///   - DataGridView에 결과 표시 (최대 500건)
    ///   - 상태별 셀 색상 (성공=녹색, 실패=빨강, SKIP=주황)
    ///   - CSV 내보내기
    ///   - Enter키 검색 / 더블클릭 에러 상세 보기
    ///
    /// 데이터 소스: MailLogRepository.GetLogTable()
    ///   → DataTable 반환 → DataGridView 직접 바인딩
    /// </summary>
    public partial class LogViewForm : Form
    {
        private readonly MailLogRepository _logRepo;
        private readonly BranchRepository _branchRepo;

        public LogViewForm()
        {
            InitializeComponent();
            _logRepo = new MailLogRepository();
            _branchRepo = new BranchRepository();

            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnSearch.Click += OnSearchClick;
            btnReset.Click += OnResetClick;
            btnExportCsv.Click += OnExportCsvClick;
            dgvLogs.CellDoubleClick += OnCellDoubleClick;
            dgvLogs.CellFormatting += OnCellFormatting;

            // Enter키로 검색
            txtCustName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { OnSearchClick(s, e); e.SuppressKeyPress = true; }
            };
        }

        #endregion

        #region 폼 로드

        private void OnFormLoad(object sender, EventArgs e)
        {
            InitFilters();
            ExecuteSearch();
        }

        #endregion

        #region 필터 초기화

        private void InitFilters()
        {
            // 기간: 최근 30일
            dtpDateFrom.Value = DateTime.Today.AddDays(-30);
            dtpDateTo.Value = DateTime.Today;

            // 유형 콤보
            cmbMailType.Items.Clear();
            cmbMailType.Items.Add("(전체)");
            cmbMailType.Items.Add(MailTypes.MoveIn);
            cmbMailType.Items.Add(MailTypes.Unpaid1st);
            cmbMailType.Items.Add(MailTypes.Unpaid2nd);
            cmbMailType.Items.Add(MailTypes.UnpaidFinal);
            cmbMailType.Items.Add(MailTypes.RenewalManual);
            cmbMailType.Items.Add(MailTypes.RenewalAuto);
            cmbMailType.Items.Add(MailTypes.MoveOut);
            cmbMailType.SelectedIndex = 0;

            // 상태 콤보
            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("(전체)");
            cmbStatus.Items.Add("성공");
            cmbStatus.Items.Add("실패");
            cmbStatus.Items.Add("SKIP");
            cmbStatus.SelectedIndex = 0;

            // 지점 콤보
            cmbBranch.Items.Clear();
            cmbBranch.Items.Add("(전체)");
            try
            {
                var branches = _branchRepo.GetAll();
                foreach (var br in branches)
                    cmbBranch.Items.Add(br.BranchCode + "|" + br.BranchName);
            }
            catch (Exception ex)
            {
                AppLog.Error("지점 목록 로드 실패 (LogViewForm)", ex);
            }
            cmbBranch.SelectedIndex = 0;

            txtCustName.Text = string.Empty;
        }

        #endregion

        #region 검색 실행

        private void OnSearchClick(object sender, EventArgs e)
        {
            ExecuteSearch();
        }

        private void ExecuteSearch()
        {
            lblResultCount.Text = "조회 중...";
            lblResultCount.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                // 필터 값 추출
                var dateFrom = dtpDateFrom.Value.Date;
                var dateTo = dtpDateTo.Value.Date;

                string mailType = null;
                if (cmbMailType.SelectedIndex > 0)
                    mailType = cmbMailType.SelectedItem.ToString();

                string status = null;
                if (cmbStatus.SelectedIndex > 0)
                    status = cmbStatus.SelectedItem.ToString();

                string custName = null;
                if (!string.IsNullOrWhiteSpace(txtCustName.Text))
                    custName = txtCustName.Text.Trim();

                string branchCode = null;
                if (cmbBranch.SelectedIndex > 0)
                {
                    var selected = cmbBranch.SelectedItem.ToString();
                    var pipeIdx = selected.IndexOf('|');
                    if (pipeIdx > 0)
                        branchCode = selected.Substring(0, pipeIdx);
                }

                // DB 조회 (DataTable → DataGridView 바인딩)
                var dt = _logRepo.GetLogTable(dateFrom, dateTo, mailType, custName, status, branchCode);

                dgvLogs.DataSource = null;
                dgvLogs.DataSource = dt;

                // 컬럼 너비 조정
                AdjustColumnWidths();

                lblResultCount.Text = $"총 {dt.Rows.Count}건 조회됨";
                lblResultCount.ForeColor = Color.FromArgb(52, 73, 94);

                if (dt.Rows.Count >= 500)
                {
                    lblResultCount.Text += " (최대 500건 표시)";
                    lblResultCount.ForeColor = Color.FromArgb(211, 84, 0);
                }
            }
            catch (Exception ex)
            {
                lblResultCount.Text = "조회 실패: " + ex.Message;
                lblResultCount.ForeColor = Color.Red;
                AppLog.Error("발송 로그 조회 실패", ex);
            }
        }

        #endregion

        #region 컬럼 너비 조정

        private void AdjustColumnWidths()
        {
            if (dgvLogs.Columns.Count == 0) return;

            // 컬럼별 적정 너비 설정
            var widthMap = new System.Collections.Generic.Dictionary<string, int>
            {
                ["No"] = 55,
                ["유형"] = 70,
                ["회사명"] = 160,
                ["이메일"] = 200,
                ["발송일"] = 100,
                ["발송시각"] = 75,
                ["상태"] = 55,
                ["에러메시지"] = 200,
                ["MoveInID"] = 65,
                ["InvoiceID"] = 65,
                ["기록일시"] = 130
            };

            foreach (DataGridViewColumn col in dgvLogs.Columns)
            {
                if (widthMap.ContainsKey(col.HeaderText))
                {
                    col.Width = widthMap[col.HeaderText];
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }

            // 에러메시지 컬럼은 나머지 공간 채우기
            if (dgvLogs.Columns.Contains("에러메시지"))
            {
                dgvLogs.Columns["에러메시지"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        #endregion

        #region 셀 포맷팅 — 상태별 색상

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // "상태" 컬럼 찾기
            var colName = dgvLogs.Columns[e.ColumnIndex].HeaderText;
            if (colName != "상태") return;

            var value = e.Value?.ToString();
            switch (value)
            {
                case "성공":
                    e.CellStyle.BackColor = Color.FromArgb(212, 239, 223);
                    e.CellStyle.ForeColor = Color.FromArgb(30, 130, 76);
                    e.CellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
                    break;
                case "실패":
                    e.CellStyle.BackColor = Color.FromArgb(250, 219, 216);
                    e.CellStyle.ForeColor = Color.FromArgb(192, 57, 43);
                    e.CellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
                    break;
                case "SKIP":
                    e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
                    e.CellStyle.ForeColor = Color.FromArgb(211, 84, 0);
                    e.CellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
                    break;
            }
        }

        #endregion

        #region 더블클릭 — 에러 상세 보기

        private void OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 에러메시지 컬럼의 값을 팝업으로 표시
            var row = dgvLogs.Rows[e.RowIndex];
            var errorMsg = string.Empty;
            var custName = string.Empty;
            var mailType = string.Empty;

            if (row.Cells["에러메시지"] != null)
                errorMsg = row.Cells["에러메시지"].Value?.ToString() ?? string.Empty;

            if (row.Cells["회사명"] != null)
                custName = row.Cells["회사명"].Value?.ToString() ?? string.Empty;

            if (row.Cells["유형"] != null)
                mailType = row.Cells["유형"].Value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                MessageBox.Show($"[{mailType}] {custName}\n\n에러 메시지 없음 (정상 발송)",
                    "상세 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"[{mailType}] {custName}\n\n{errorMsg}",
                    "에러 상세", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region 초기화

        private void OnResetClick(object sender, EventArgs e)
        {
            InitFilters();
            ExecuteSearch();
        }

        #endregion

        #region CSV 내보내기

        private void OnExportCsvClick(object sender, EventArgs e)
        {
            if (dgvLogs.Rows.Count == 0)
            {
                MessageBox.Show("내보낼 데이터가 없습니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV 파일 (*.csv)|*.csv";
                dialog.FileName = $"발송현황_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                dialog.Title = "CSV 내보내기";

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    ExportToCsv(dialog.FileName);
                    MessageBox.Show($"CSV 내보내기 완료\n{dialog.FileName}",
                        "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppLog.Error("CSV 내보내기 실패", ex);
                    MessageBox.Show("CSV 내보내기 실패: " + ex.Message,
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// DataGridView 내용을 CSV 파일로 저장합니다.
        /// BOM 포함 UTF-8 (엑셀 한글 호환).
        /// </summary>
        private void ExportToCsv(string filePath)
        {
            var sb = new StringBuilder();

            // 헤더
            for (int i = 0; i < dgvLogs.Columns.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(EscapeCsv(dgvLogs.Columns[i].HeaderText));
            }
            sb.AppendLine();

            // 데이터
            foreach (DataGridViewRow row in dgvLogs.Rows)
            {
                for (int i = 0; i < dgvLogs.Columns.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    var value = row.Cells[i].Value?.ToString() ?? string.Empty;
                    sb.Append(EscapeCsv(value));
                }
                sb.AppendLine();
            }

            // BOM 포함 UTF-8 저장 (엑셀에서 한글 정상 표시)
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>
        /// CSV 필드 값에 쉼표/따옴표/줄바꿈이 포함된 경우 이스케이프합니다.
        /// </summary>
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        #endregion
    }
}
