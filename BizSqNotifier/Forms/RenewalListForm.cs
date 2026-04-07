using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using BizSqNotifier.Config;
using BizSqNotifier.Data;
using BizSqNotifier.Models;
using BizSqNotifier.Services;

namespace BizSqNotifier
{
    /// <summary>
    /// 갱신 대상 목록 화면.
    /// 탭1: 오피스(N인실) 33일 이내 / 탭2: adBox·회원제(나머지) 15일 이내
    /// </summary>
    public partial class RenewalListForm : Form
    {
        public RenewalListForm()
        {
            InitializeComponent();
            WireEvents();
        }

        private void WireEvents()
        {
            this.Load += OnFormLoad;
            btnRefresh.Click += (s, e) => LoadAll();
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            LoadAll();
        }

        private void LoadAll()
        {
            try
            {
                // 오피스 (N인실) — 33일 이내
                var officeTargets = new RenewalManualService().GetRenewalTargets(
                    UserSettings.Current.RenewalManualDays
                );

                // adBox/회원제 — 15일 이내
                var autoTargets = GetAutoRenewalTargetsRange(15);

                // ── 오피스 탭 ──
                dgvOffice.Rows.Clear();
                foreach (var t in officeTargets)
                {
                    var ri = dgvOffice.Rows.Add(
                        t.CustName,
                        t.BranchName,
                        t.ProductName,
                        t.OfficeNum,
                        t.DateTo,
                        t.DaysUntilExpiry.ToString() + "일",
                        t.Deposit.ToString("#,0"),
                        t.Price.ToString("#,0"),
                        t.Email ?? "(미등록)"
                    );

                    // 7일 이내 → 노란색 강조
                    if (t.DaysUntilExpiry <= 7)
                        dgvOffice.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(
                            255,
                            253,
                            231
                        );
                }
                lblOfficeStatus.Text = $"오피스 갱신 대상: {officeTargets.Count}건";

                // ── adBox/회원제 탭 ──
                dgvAdBox.Rows.Clear();
                foreach (var t in autoTargets)
                {
                    var ri = dgvAdBox.Rows.Add(
                        t.CustName,
                        t.BranchName,
                        t.ProductName,
                        t.OfficeNum,
                        t.DateTo,
                        t.DaysUntilExpiry.ToString() + "일",
                        t.Email ?? "(미등록)"
                    );

                    if (t.DaysUntilExpiry <= 3)
                        dgvAdBox.Rows[ri].DefaultCellStyle.BackColor = Color.FromArgb(
                            255,
                            235,
                            238
                        );
                }
                lblAdBoxStatus.Text = $"adBox/회원제 갱신 대상: {autoTargets.Count}건";
            }
            catch (Exception ex)
            {
                AppLog.Error("갱신 대상 목록 로드 실패", ex);
                MessageBox.Show(
                    "목록 로드 실패:\n" + ex.Message,
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>adBox/회원제 갱신 대상 범위 조회 (0~maxDays일).</summary>
        private List<RenewalInfo> GetAutoRenewalTargetsRange(int maxDays)
        {
            const string sql =
                @"
SELECT
    m.id AS movein_id, m.br_code, m.cust AS cust_name, c.email,
    b.br_name AS branch_name, b.bank_accnt AS bank_account, b.bank_holder,
    m.prd_prd AS product_name, m.off_num AS office_num,
    ISNULL(m.deposit, 0) AS deposit, ISNULL(m.price, 0) AS price,
    m.date_from, m.date_to, m.date_out,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) AS days_until_expiry
FROM dbo.tb_movein m
    LEFT JOIN dbo.tb_customer c ON m.cu_id = c.id
    LEFT JOIN dbo.tb_branch b ON m.br_code = b.br_code
WHERE m.date_to IS NOT NULL AND m.date_to <> ''
  AND ISDATE(m.date_to) = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(m.date_to AS DATE)) BETWEEN 0 AND @maxDays
  AND (
    ISNULL(m.prd_prd, '') IN ('개인사업자', '법인사업자')
    OR ISNULL(m.prd_prd, '') LIKE '스마트데스크%'
)
  AND (m.date_out IS NULL OR m.date_out = ''
       OR (ISDATE(m.date_out) = 1 AND CAST(m.date_out AS DATE) >= CAST(m.date_to AS DATE)))
ORDER BY m.date_to ASC, m.cust;";

            return DbManager.ExecuteReader(
                sql,
                r => new RenewalInfo
                {
                    MoveInId = DbManager.GetSafeInt(r, "movein_id"),
                    BranchCode = DbManager.GetSafeString(r, "br_code"),
                    CustName = DbManager.GetSafeString(r, "cust_name"),
                    Email = DbManager.GetSafeString(r, "email"),
                    BranchName = DbManager.GetSafeString(r, "branch_name"),
                    BankAccount = DbManager.GetSafeString(r, "bank_account"),
                    BankHolder = DbManager.GetSafeString(r, "bank_holder"),
                    ProductName = DbManager.GetSafeString(r, "product_name"),
                    OfficeNum = DbManager.GetSafeString(r, "office_num"),
                    Deposit = DbManager.GetSafeInt(r, "deposit"),
                    Price = DbManager.GetSafeInt(r, "price"),
                    DateFrom = DbManager.GetSafeString(r, "date_from"),
                    DateTo = DbManager.GetSafeString(r, "date_to"),
                    DateOut = DbManager.GetSafeString(r, "date_out"),
                    DaysUntilExpiry = DbManager.GetSafeInt(r, "days_until_expiry"),
                },
                new SqlParameter("@maxDays", maxDays)
            );
        }
    }
}
