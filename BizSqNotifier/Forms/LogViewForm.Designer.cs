namespace BizSqNotifier
{
    partial class LogViewForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // ── 상단 필터 패널 ──
            this.panelFilter = new System.Windows.Forms.Panel();
            this.lblDateFrom = new System.Windows.Forms.Label();
            this.dtpDateFrom = new System.Windows.Forms.DateTimePicker();
            this.lblDateTo = new System.Windows.Forms.Label();
            this.dtpDateTo = new System.Windows.Forms.DateTimePicker();
            this.lblMailType = new System.Windows.Forms.Label();
            this.cmbMailType = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.lblCustName = new System.Windows.Forms.Label();
            this.txtCustName = new System.Windows.Forms.TextBox();
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();

            // ── DataGridView ──
            this.dgvLogs = new System.Windows.Forms.DataGridView();

            // ── 하단 상태 패널 ──
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblResultCount = new System.Windows.Forms.Label();
            this.btnExportCsv = new System.Windows.Forms.Button();

            this.panelFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // panelFilter
            // ════════════════════════════════════════
            this.panelFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilter.Height = 88;
            this.panelFilter.Padding = new System.Windows.Forms.Padding(12, 8, 12, 4);
            this.panelFilter.BackColor = System.Drawing.Color.FromArgb(245, 248, 250);
            this.panelFilter.Name = "panelFilter";

            // ── Row 1: 기간 + 유형 + 상태 ──
            int r1Y = 10;

            this.lblDateFrom.AutoSize = true;
            this.lblDateFrom.Location = new System.Drawing.Point(12, r1Y + 3);
            this.lblDateFrom.Text = "기간:";
            this.lblDateFrom.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDateFrom.Name = "lblDateFrom";

            this.dtpDateFrom.Location = new System.Drawing.Point(48, r1Y);
            this.dtpDateFrom.Size = new System.Drawing.Size(130, 23);
            this.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateFrom.Name = "dtpDateFrom";

            this.lblDateTo.AutoSize = true;
            this.lblDateTo.Location = new System.Drawing.Point(184, r1Y + 3);
            this.lblDateTo.Text = "~";
            this.lblDateTo.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDateTo.Name = "lblDateTo";

            this.dtpDateTo.Location = new System.Drawing.Point(198, r1Y);
            this.dtpDateTo.Size = new System.Drawing.Size(130, 23);
            this.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDateTo.Name = "dtpDateTo";

            this.lblMailType.AutoSize = true;
            this.lblMailType.Location = new System.Drawing.Point(348, r1Y + 3);
            this.lblMailType.Text = "유형:";
            this.lblMailType.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMailType.Name = "lblMailType";

            this.cmbMailType.Location = new System.Drawing.Point(384, r1Y);
            this.cmbMailType.Size = new System.Drawing.Size(110, 23);
            this.cmbMailType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMailType.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.cmbMailType.Name = "cmbMailType";

            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(510, r1Y + 3);
            this.lblStatus.Text = "상태:";
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblStatus.Name = "lblStatus";

            this.cmbStatus.Location = new System.Drawing.Point(546, r1Y);
            this.cmbStatus.Size = new System.Drawing.Size(90, 23);
            this.cmbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.cmbStatus.Name = "cmbStatus";

            // ── Row 2: 회사명 + 검색/초기화 버튼 ──
            int r2Y = r1Y + 32;

            this.lblCustName.AutoSize = true;
            this.lblCustName.Location = new System.Drawing.Point(12, r2Y + 3);
            this.lblCustName.Text = "회사명:";
            this.lblCustName.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblCustName.Name = "lblCustName";

            this.txtCustName.Location = new System.Drawing.Point(66, r2Y);
            this.txtCustName.Size = new System.Drawing.Size(200, 23);
            this.txtCustName.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.txtCustName.Name = "txtCustName";

            this.btnSearch.Location = new System.Drawing.Point(500, r2Y - 2);
            this.btnSearch.Size = new System.Drawing.Size(80, 28);
            this.btnSearch.Text = "검색";
            this.btnSearch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSearch.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnSearch.ForeColor = System.Drawing.Color.White;
            this.btnSearch.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSearch.Name = "btnSearch";

            this.btnReset.Location = new System.Drawing.Point(588, r2Y - 2);
            this.btnReset.Size = new System.Drawing.Size(60, 28);
            this.btnReset.Text = "초기화";
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnReset.Name = "btnReset";

            this.panelFilter.Controls.Add(this.lblDateFrom);
            this.panelFilter.Controls.Add(this.dtpDateFrom);
            this.panelFilter.Controls.Add(this.lblDateTo);
            this.panelFilter.Controls.Add(this.dtpDateTo);
            this.panelFilter.Controls.Add(this.lblMailType);
            this.panelFilter.Controls.Add(this.cmbMailType);
            this.panelFilter.Controls.Add(this.lblStatus);
            this.panelFilter.Controls.Add(this.cmbStatus);
            this.panelFilter.Controls.Add(this.lblCustName);
            this.panelFilter.Controls.Add(this.txtCustName);
            this.panelFilter.Controls.Add(this.btnSearch);
            this.panelFilter.Controls.Add(this.btnReset);

            // ════════════════════════════════════════
            // dgvLogs
            // ════════════════════════════════════════
            this.dgvLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLogs.AllowUserToAddRows = false;
            this.dgvLogs.AllowUserToDeleteRows = false;
            this.dgvLogs.ReadOnly = true;
            this.dgvLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLogs.MultiSelect = false;
            this.dgvLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvLogs.RowHeadersVisible = false;
            this.dgvLogs.BackgroundColor = System.Drawing.Color.White;
            this.dgvLogs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvLogs.AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(245, 248, 250)
            };
            this.dgvLogs.ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
            };
            this.dgvLogs.EnableHeadersVisualStyles = false;
            this.dgvLogs.Name = "dgvLogs";

            // ════════════════════════════════════════
            // panelBottom
            // ════════════════════════════════════════
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Height = 40;
            this.panelBottom.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.panelBottom.Name = "panelBottom";

            this.lblResultCount.AutoSize = true;
            this.lblResultCount.Location = new System.Drawing.Point(12, 12);
            this.lblResultCount.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblResultCount.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblResultCount.Text = "";
            this.lblResultCount.Name = "lblResultCount";

            this.btnExportCsv.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnExportCsv.Location = new System.Drawing.Point(830, 6);
            this.btnExportCsv.Size = new System.Drawing.Size(100, 28);
            this.btnExportCsv.Text = "CSV 내보내기";
            this.btnExportCsv.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportCsv.BackColor = System.Drawing.Color.FromArgb(44, 62, 80);
            this.btnExportCsv.ForeColor = System.Drawing.Color.White;
            this.btnExportCsv.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.btnExportCsv.Name = "btnExportCsv";

            this.panelBottom.Controls.Add(this.lblResultCount);
            this.panelBottom.Controls.Add(this.btnExportCsv);

            // ════════════════════════════════════════
            // LogViewForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(950, 560);
            this.Controls.Add(this.dgvLogs);
            this.Controls.Add(this.panelFilter);
            this.Controls.Add(this.panelBottom);
            this.MinimumSize = new System.Drawing.Size(800, 450);
            this.Name = "LogViewForm";
            this.Text = "발송 현황 조회";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.panelFilter.ResumeLayout(false);
            this.panelFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLogs)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelFilter;
        private System.Windows.Forms.Label lblDateFrom;
        private System.Windows.Forms.DateTimePicker dtpDateFrom;
        private System.Windows.Forms.Label lblDateTo;
        private System.Windows.Forms.DateTimePicker dtpDateTo;
        private System.Windows.Forms.Label lblMailType;
        private System.Windows.Forms.ComboBox cmbMailType;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Label lblCustName;
        private System.Windows.Forms.TextBox txtCustName;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnReset;

        private System.Windows.Forms.DataGridView dgvLogs;

        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblResultCount;
        private System.Windows.Forms.Button btnExportCsv;
    }
}
