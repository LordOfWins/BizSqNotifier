namespace BizSqNotifier
{
    partial class RenewalListForm
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
            // ── 객체 생성 (모두 먼저) ──
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblFormTitle = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();

            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabOffice = new System.Windows.Forms.TabPage();
            this.tabAdBox = new System.Windows.Forms.TabPage();

            this.dgvOffice = new System.Windows.Forms.DataGridView();
            this.panelOfficeBottom = new System.Windows.Forms.Panel();
            this.lblOfficeStatus = new System.Windows.Forms.Label();
            this.lblOfficeHint = new System.Windows.Forms.Label();

            this.dgvAdBox = new System.Windows.Forms.DataGridView();
            this.panelAdBoxBottom = new System.Windows.Forms.Panel();
            this.lblAdBoxStatus = new System.Windows.Forms.Label();

            // ── SuspendLayout ──
            this.tabMain.SuspendLayout();
            this.tabOffice.SuspendLayout();
            this.tabAdBox.SuspendLayout();
            this.panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOffice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAdBox)).BeginInit();
            this.panelOfficeBottom.SuspendLayout();
            this.panelAdBoxBottom.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // panelHeader
            // ════════════════════════════════════════
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 48;
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(44, 62, 80);
            this.panelHeader.Name = "panelHeader";

            this.lblFormTitle.AutoSize = true;
            this.lblFormTitle.Font = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.lblFormTitle.ForeColor = System.Drawing.Color.White;
            this.lblFormTitle.Location = new System.Drawing.Point(16, 13);
            this.lblFormTitle.Text = "갱신 대상 목록";
            this.lblFormTitle.Name = "lblFormTitle";

            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.Location = new System.Drawing.Point(820, 10);
            this.btnRefresh.Size = new System.Drawing.Size(80, 28);
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnRefresh.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnRefresh.Name = "btnRefresh";

            this.panelHeader.Controls.Add(this.lblFormTitle);
            this.panelHeader.Controls.Add(this.btnRefresh);

            // ════════════════════════════════════════
            // tabMain
            // ════════════════════════════════════════
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.TabPages.Add(this.tabOffice);
            this.tabMain.TabPages.Add(this.tabAdBox);
            this.tabMain.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.tabMain.Padding = new System.Drawing.Point(16, 4);
            this.tabMain.Name = "tabMain";

            // ════════════════════════════════════════
            // tabOffice — 오피스 (33일 이내)
            // ════════════════════════════════════════
            this.tabOffice.Text = "오피스 (33일 이내)";
            this.tabOffice.Name = "tabOffice";

            this.dgvOffice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOffice.AllowUserToAddRows = false;
            this.dgvOffice.AllowUserToDeleteRows = false;
            this.dgvOffice.ReadOnly = true;
            this.dgvOffice.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOffice.MultiSelect = false;
            this.dgvOffice.RowHeadersVisible = false;
            this.dgvOffice.BackgroundColor = System.Drawing.Color.White;
            this.dgvOffice.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvOffice.AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(245, 248, 250)
            };
            this.dgvOffice.ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
            };
            this.dgvOffice.EnableHeadersVisualStyles = false;
            this.dgvOffice.Name = "dgvOffice";

            this.dgvOffice.Columns.Add("colCust", "회사명");
            this.dgvOffice.Columns.Add("colBranch", "지점");
            this.dgvOffice.Columns.Add("colProduct", "상품/분류");
            this.dgvOffice.Columns.Add("colRoom", "호실");
            this.dgvOffice.Columns.Add("colDateTo", "계약종료일");
            this.dgvOffice.Columns.Add("colDays", "남은일수");
            this.dgvOffice.Columns.Add("colDeposit", "예치금");
            this.dgvOffice.Columns.Add("colPrice", "이용료");
            this.dgvOffice.Columns.Add("colEmail", "이메일");

            this.dgvOffice.Columns["colCust"].Width = 130;
            this.dgvOffice.Columns["colBranch"].Width = 90;
            this.dgvOffice.Columns["colProduct"].Width = 80;
            this.dgvOffice.Columns["colRoom"].Width = 60;
            this.dgvOffice.Columns["colDateTo"].Width = 95;
            this.dgvOffice.Columns["colDays"].Width = 65;
            this.dgvOffice.Columns["colDeposit"].Width = 80;
            this.dgvOffice.Columns["colPrice"].Width = 80;
            this.dgvOffice.Columns["colEmail"].AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;

            this.panelOfficeBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelOfficeBottom.Height = 36;
            this.panelOfficeBottom.Name = "panelOfficeBottom";

            this.lblOfficeStatus.AutoSize = true;
            this.lblOfficeStatus.Location = new System.Drawing.Point(8, 10);
            this.lblOfficeStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblOfficeStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblOfficeStatus.Text = "";
            this.lblOfficeStatus.Name = "lblOfficeStatus";

            this.lblOfficeHint.AutoSize = true;
            this.lblOfficeHint.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.lblOfficeHint.Location = new System.Drawing.Point(620, 10);
            this.lblOfficeHint.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.lblOfficeHint.ForeColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.lblOfficeHint.Text = "* 수동 발송은 [수동 발송] 화면에서 진행하세요";
            this.lblOfficeHint.Name = "lblOfficeHint";

            this.panelOfficeBottom.Controls.Add(this.lblOfficeStatus);
            this.panelOfficeBottom.Controls.Add(this.lblOfficeHint);

            this.tabOffice.Controls.Add(this.dgvOffice);
            this.tabOffice.Controls.Add(this.panelOfficeBottom);

            // ════════════════════════════════════════
            // tabAdBox — adBox/회원제 (15일 이내)
            // ════════════════════════════════════════
            this.tabAdBox.Text = "adBox·회원제 (15일 이내)";
            this.tabAdBox.Name = "tabAdBox";

            this.dgvAdBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAdBox.AllowUserToAddRows = false;
            this.dgvAdBox.AllowUserToDeleteRows = false;
            this.dgvAdBox.ReadOnly = true;
            this.dgvAdBox.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAdBox.MultiSelect = false;
            this.dgvAdBox.RowHeadersVisible = false;
            this.dgvAdBox.BackgroundColor = System.Drawing.Color.White;
            this.dgvAdBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvAdBox.AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(245, 248, 250)
            };
            this.dgvAdBox.ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
            };
            this.dgvAdBox.EnableHeadersVisualStyles = false;
            this.dgvAdBox.Name = "dgvAdBox";

            this.dgvAdBox.Columns.Add("colCust", "회사명");
            this.dgvAdBox.Columns.Add("colBranch", "지점");
            this.dgvAdBox.Columns.Add("colProduct", "상품/분류");
            this.dgvAdBox.Columns.Add("colRoom", "호실");
            this.dgvAdBox.Columns.Add("colDateTo", "계약종료일");
            this.dgvAdBox.Columns.Add("colDays", "남은일수");
            this.dgvAdBox.Columns.Add("colEmail", "이메일");

            this.dgvAdBox.Columns["colCust"].Width = 130;
            this.dgvAdBox.Columns["colBranch"].Width = 90;
            this.dgvAdBox.Columns["colProduct"].Width = 100;
            this.dgvAdBox.Columns["colRoom"].Width = 60;
            this.dgvAdBox.Columns["colDateTo"].Width = 95;
            this.dgvAdBox.Columns["colDays"].Width = 65;
            this.dgvAdBox.Columns["colEmail"].AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;

            this.panelAdBoxBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelAdBoxBottom.Height = 36;
            this.panelAdBoxBottom.Name = "panelAdBoxBottom";

            this.lblAdBoxStatus.AutoSize = true;
            this.lblAdBoxStatus.Location = new System.Drawing.Point(8, 10);
            this.lblAdBoxStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblAdBoxStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblAdBoxStatus.Text = "";
            this.lblAdBoxStatus.Name = "lblAdBoxStatus";

            this.panelAdBoxBottom.Controls.Add(this.lblAdBoxStatus);

            this.tabAdBox.Controls.Add(this.dgvAdBox);
            this.tabAdBox.Controls.Add(this.panelAdBoxBottom);

            // ════════════════════════════════════════
            // RenewalListForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 540);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.panelHeader);
            this.MinimumSize = new System.Drawing.Size(750, 400);
            this.Name = "RenewalListForm";
            this.Text = "갱신 대상 목록";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.tabMain.ResumeLayout(false);
            this.tabOffice.ResumeLayout(false);
            this.tabAdBox.ResumeLayout(false);
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOffice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAdBox)).EndInit();
            this.panelOfficeBottom.ResumeLayout(false);
            this.panelOfficeBottom.PerformLayout();
            this.panelAdBoxBottom.ResumeLayout(false);
            this.panelAdBoxBottom.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblFormTitle;
        private System.Windows.Forms.Button btnRefresh;

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabOffice;
        private System.Windows.Forms.TabPage tabAdBox;

        private System.Windows.Forms.DataGridView dgvOffice;
        private System.Windows.Forms.Panel panelOfficeBottom;
        private System.Windows.Forms.Label lblOfficeStatus;
        private System.Windows.Forms.Label lblOfficeHint;

        private System.Windows.Forms.DataGridView dgvAdBox;
        private System.Windows.Forms.Panel panelAdBoxBottom;
        private System.Windows.Forms.Label lblAdBoxStatus;
    }
}
