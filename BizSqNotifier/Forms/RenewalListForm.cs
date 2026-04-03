using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 갱신 대상 목록 화면 (기존 MOS 음영 표시 대체).
    ///
    /// 탭 구성:
    ///   [오피스] 계약종료 33일 이내 목록 (prd_prd LIKE '%오피스%')
    ///   [adBox·회원제] 계약종료 15일 이내 목록 (prd_prd NOT LIKE '%오피스%')
    ///
    /// 기능:
    ///   - D-Day 컬럼 색상 (7일 이내 빨강 / 14일 이내 주황 / 그 외 기본)
    ///   - 이메일 미등록 행 회색 표시
    ///   - 오피스 탭: 더블클릭 → RenewalManualForm 열기
    ///   - 발송 완료 여부 아이콘/텍스트 표시
    ///   - 새로고침 / 건수 표시
    /// </summary>
    public partial class RenewalListForm : Form
    {
        private readonly RenewalManualService _manualService;
        private readonly RenewalAutoService _autoService;

        public RenewalListForm()
        {
            InitializeComponent();
            _manualService = new RenewalManualService();
            _autoService = new RenewalAutoService();

            WireEvents();
        }

        #region 이벤트 연결

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnRefresh.Click += OnRefreshClick;
            tabMain.SelectedIndexChanged += OnTabChanged;
            dgvOffice.CellDoubleClick += OnOfficeCellDoubleClick;
            dgvOffice.CellFormatting += OnCellFormatting;
            dgvAdBox.CellFormatting += OnCellFormatting;
        }

        #endregion

        #region 폼 로드

        private void OnFormLoad(object sender, EventArgs e)
        {
            SetupOfficeColumns();
            SetupAdBoxColumns();
            LoadAll();
        }

        #endregion

        #region DataGridView 컬럼 설정

        private void SetupOfficeColumns()
        {
            dgvOffice.Columns.Clear();
            dgvOffice.AutoGenerateColumns = false;

            AddColumn(dgvOffice, "colOCust", "회사명", "CustName", 140);
            AddColumn(dgvOffice, "colOBranch", "지점", "BranchName", 90);
            AddColumn(dgvOffice, "colOProduct", "상품", "ProductName", 90);
            AddColumn(dgvOffice, "colOOffice", "호실", "OfficeNum", 70);
            AddColumn(dgvOffice, "colODeposit", "예치금", "DepositDisplay", 100);
            AddColumn(dgvOffice, "colOPrice", "이용료", "PriceDisplay", 100);
            AddColumn(dgvOffice, "colODateTo", "계약종료일", "DateToDisplay", 100);
            AddColumn(dgvOffice, "colODays", "D-Day", "DaysUntilExpiry", 60);
            AddColumn(dgvOffice, "colOEmail", "이메일", "Email", 160);
            AddColumn(dgvOffice, "colOStatus", "발송", "SendStatus", 60);
        }

        private void SetupAdBoxColumns()
        {
            dgvAdBox.Columns.Clear();
            dgvAdBox.AutoGenerateColumns = false;

            AddColumn(dgvAdBox, "colACust", "회사명", "CustName", 140);
            AddColumn(dgvAdBox, "colABranch", "지점", "BranchName", 90);
            AddColumn(dgvAdBox, "colAProduct", "상품", "ProductName", 100);
            AddColumn(dgvAdBox, "colAOffice", "호실", "OfficeNum", 70);
            AddColumn(dgvAdBox, "colADateTo", "계약종료일", "DateToDisplay", 100);
            AddColumn(dgvAdBox, "colADays", "D-Day", "DaysUntilExpiry", 60);
            AddColumn(dgvAdBox, "colAEmail", "이메일", "Email", 180);
            AddColumn(dgvAdBox, "colAStatus", "발송", "SendStatus", 60);
        }

        private static void AddColumn(DataGridView dgv, string name, string header, string dataProp, int width)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = dataProp,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        #endregion

        #region 데이터 로드

        private void LoadAll()
        {
            LoadOffice();
            LoadAdBox();
        }

        private void LoadOffice()
        {
            lblOfficeStatus.Text = "조회 중...";
            lblOfficeStatus.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                var manualDays = UserSettings.Current.RenewalManualDays;
                var targets = _manualService.GetRenewalTargets(manualDays);
                var logRepo = new Data.MailLogRepository();

                var displayList = new List<RenewalListDisplayRow>();
                foreach (var info in targets)
                {
                    // 발송 여부 확인
                    bool sent = logRepo.HasSentByMoveIn(MailTypes.RenewalManual, info.MoveInId);
                    displayList.Add(new RenewalListDisplayRow(info, sent));
                }

                dgvOffice.DataSource = null;
                dgvOffice.DataSource = displayList;
                ColorDayColumn(dgvOffice, "colODays");

                lblOfficeStatus.Text = $"오피스 갱신 대상: {displayList.Count}건 (계약종료 {manualDays}일 이내)";
                lblOfficeStatus.ForeColor = Color.FromArgb(41, 128, 185);
            }
            catch (Exception ex)
            {
                lblOfficeStatus.Text = "조회 실패: " + ex.Message;
                lblOfficeStatus.ForeColor = Color.Red;
                AppLog.Error("[갱신목록] 오피스 조회 실패", ex);
            }
        }

        private void LoadAdBox()
        {
            lblAdBoxStatus.Text = "조회 중...";
            lblAdBoxStatus.ForeColor = Color.Gray;
            Application.DoEvents();

            try
            {
                // adBox/회원제: 15일 이내 (오피스 필터 반대)
                var autoDays = 15;
                var targets = _autoService.GetTargets(autoDays);

                // GetTargets는 = 조건이므로 범위 조회용 별도 SQL 필요
                // 여기서는 간이로 0~15일 범위 전체를 가져오기 위해
                // RenewalAutoService의 SQL을 직접 쓰는 대신
                // 15일 이내 범위 조회 전용 메서드를 호출
                var rangeTargets = GetAdBoxRangeTargets(autoDays);
                var logRepo = new Data.MailLogRepository();

                var displayList = new List<RenewalListDisplayRow>();
                foreach (var info in rangeTargets)
                {
                    bool sent = logRepo.HasSentByMoveIn(MailTypes.RenewalAuto, info.MoveInId);
                    displayList.Add(new RenewalListDisplayRow(info, sent));
                }

                dgvAdBox.DataSource = null;
                dgvAdBox.DataSource = displayList;
                ColorDayColumn(dgvAdBox, "colADays");

                lblAdBoxStatus.Text = $"adBox·회원제 갱신 대상: {displayList.Count}건 (계약종료 {autoDays}일 이내)";
                lblAdBoxStatus.ForeColor = Color.FromArgb(41, 128, 185);
            }
            catch (Exception ex)
            {
                lblAdBoxStatus.Text = "조회 실패: " + ex.Message;
                lblAdBoxStatus.ForeColor = Color.Red;
                AppLog.Error("[갱신목록] adBox 조회 실패", ex);
            }
        }

        /// <summary>
        /// adBox/회원제 상품의 갱신 대상을 범위(0~maxDays)로 조회합니다.
        /// RenewalAutoService.GetTargets()는 정확히 N일 전만 반환하므로
        /// 목록 화면용으로는 범위 조회가 필요합니다.
        /// </summary>
        private List<RenewalInfo> GetAdBoxRangeTargets(int maxDays)
        {
            // [ESTIMATED] RenewalAutoService의 SQL과 동일하되 = → BETWEEN 변경
            const string sql = @"
SELECT
    m.movein_id,
    m.cust                  AS cust_name,
    c.email                 AS email,
    m.br_id                 AS branch_id,
    b.br_name               AS branch_name,
    b.bank_accnt            AS bank_account,
    b.bank_holder           AS bank_holder,
    m.prd_prd               AS product_name,
    m.off_num               AS office_num,
    ISNULL(m.deposit, 0)    AS deposit,
    ISNULL(m.price, 0)      AS price,
    m.date_from,
    m.date_to,
    m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) AS days_until_expiry
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cust_id = c.cust_id
    LEFT JOIN dbo.tb_branch   b ON m.br_id   = b.br_id
WHERE m.date_to IS NOT NULL
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) BETWEEN 0 AND @maxDays
  AND m.prd_prd NOT LIKE @officeFilter
  AND (m.date_out IS NULL OR m.date_out >= m.date_to)
ORDER BY m.date_to ASC, m.cust;";

            try
            {
                return Data.DbManager.ExecuteReader(
                    sql,
                    reader => new RenewalInfo
                    {
                        MoveInId = Data.DbManager.GetSafeInt(reader, "movein_id"),
                        CustName = Data.DbManager.GetSafeString(reader, "cust_name"),
                        Email = Data.DbManager.GetSafeString(reader, "email"),
                        BranchId = Data.DbManager.GetSafeNullableInt(reader, "branch_id"),
                        BranchName = Data.DbManager.GetSafeString(reader, "branch_name"),
                        BankAccount = Data.DbManager.GetSafeString(reader, "bank_account"),
                        BankHolder = Data.DbManager.GetSafeString(reader, "bank_holder"),
                        ProductName = Data.DbManager.GetSafeString(reader, "product_name"),
                        OfficeNum = Data.DbManager.GetSafeString(reader, "office_num"),
                        Deposit = Data.DbManager.GetSafeDecimal(reader, "deposit"),
                        Price = Data.DbManager.GetSafeDecimal(reader, "price"),
                        DateFrom = Data.DbManager.GetSafeDateTime(reader, "date_from"),
                        DateTo = Data.DbManager.GetSafeDateTime(reader, "date_to"),
                        DateOut = Data.DbManager.GetSafeDateTime(reader, "date_out"),
                        DaysUntilExpiry = Data.DbManager.GetSafeInt(reader, "days_until_expiry")
                    },
                    new System.Data.SqlClient.SqlParameter("@maxDays", maxDays),
                    new System.Data.SqlClient.SqlParameter("@officeFilter", "%오피스%"));
            }
            catch (Exception ex)
            {
                AppLog.Error("[갱신목록] adBox 범위 조회 실패", ex);
                return new List<RenewalInfo>();
            }
        }

        #endregion

        #region D-Day 컬럼 색상

        private static void ColorDayColumn(DataGridView dgv, string colName)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.DataBoundItem is RenewalListDisplayRow dr)
                {
                    var cell = row.Cells[colName];
                    if (dr.DaysUntilExpiry <= 7)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 220, 220);
                        cell.Style.ForeColor = Color.FromArgb(192, 57, 43);
                        cell.Style.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
                    }
                    else if (dr.DaysUntilExpiry <= 14)
                    {
                        cell.Style.BackColor = Color.FromArgb(255, 243, 205);
                        cell.Style.ForeColor = Color.FromArgb(211, 84, 0);
                    }

                    // 이메일 미등록 행 회색 처리
                    if (string.IsNullOrWhiteSpace(dr.Email))
                    {
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
                    }
                }
            }
        }

        #endregion

        #region 셀 포맷팅 — 발송 상태 색상

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            var colHeader = dgv.Columns[e.ColumnIndex].HeaderText;
            if (colHeader != "발송") return;

            var value = e.Value?.ToString();
            if (value == "완료")
            {
                e.CellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                e.CellStyle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold);
            }
            else if (value == "미발송")
            {
                e.CellStyle.ForeColor = Color.FromArgb(149, 165, 166);
            }
        }

        #endregion

        #region 오피스 더블클릭 → 수동 발송 폼

        private void OnOfficeCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var displayRow = dgvOffice.Rows[e.RowIndex].DataBoundItem as RenewalListDisplayRow;
            if (displayRow == null) return;

            using (var form = new RenewalManualForm())
            {
                form.ShowDialog(this);
            }

            // 폼 닫힌 후 목록 갱신
            LoadOffice();
        }

        #endregion

        #region 새로고침

        private void OnRefreshClick(object sender, EventArgs e)
        {
            LoadAll();
        }

        #endregion

        #region 탭 전환

        private void OnTabChanged(object sender, EventArgs e)
        {
            // 탭 전환 시 자동 새로고침 (선택적)
        }

        #endregion
    }

    #region 표시용 래퍼

    /// <summary>
    /// RenewalInfo + 발송 상태를 DataGridView에 표시하기 위한 래퍼.
    /// </summary>
    internal sealed class RenewalListDisplayRow
    {
        private readonly RenewalInfo _src;
        private readonly bool _sent;

        public RenewalListDisplayRow(RenewalInfo source, bool sent)
        {
            _src = source ?? throw new ArgumentNullException(nameof(source));
            _sent = sent;
        }

        public RenewalInfo Source => _src;

        public string CustName => _src.CustName;
        public string BranchName => _src.BranchName;
        public string ProductName => _src.ProductName;
        public string OfficeNum => _src.OfficeNum;
        public string DepositDisplay => _src.Deposit.ToString("#,0") + "원";
        public string PriceDisplay => _src.Price.ToString("#,0") + "원";
        public string DateToDisplay => _src.DateTo?.ToString("yyyy-MM-dd") ?? "";
        public int DaysUntilExpiry => _src.DaysUntilExpiry;
        public string Email => _src.Email;
        public string SendStatus => _sent ? "완료" : "미발송";
    }

    #endregion
}
