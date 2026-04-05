namespace BizSqNotifier
{
    partial class RenewalManualForm
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
            this.splitMain = new System.Windows.Forms.SplitContainer();

            // ── 상단 패널: 대상 목록 ──
            this.panelTopHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.dgvTargets = new System.Windows.Forms.DataGridView();

            // dgvTargets 컬럼 정의
            this.dgvTargets.Columns.Add("colCust", "회사명");
            this.dgvTargets.Columns.Add("colBranch", "지점");
            this.dgvTargets.Columns.Add("colProduct", "상품/분류");
            this.dgvTargets.Columns.Add("colRoom", "호실");
            this.dgvTargets.Columns.Add("colDateTo", "계약종료일");
            this.dgvTargets.Columns.Add("colDays", "남은일수");
            this.dgvTargets.Columns.Add("colDeposit", "예치금");
            this.dgvTargets.Columns.Add("colPrice", "이용료");
            this.dgvTargets.Columns.Add("colEmail", "이메일");

            // ── 하단 패널: 수기 입력 + 발송 ──
            this.panelDetail = new System.Windows.Forms.Panel();
            this.lblSelected = new System.Windows.Forms.Label();
            this.lblSelectedInfo = new System.Windows.Forms.Label();

            this.lblCurrentDeposit = new System.Windows.Forms.Label();
            this.txtCurrentDeposit = new System.Windows.Forms.TextBox();
            this.lblArrow1 = new System.Windows.Forms.Label();
            this.lblNewDeposit = new System.Windows.Forms.Label();
            this.txtNewDeposit = new System.Windows.Forms.TextBox();

            this.lblCurrentPrice = new System.Windows.Forms.Label();
            this.txtCurrentPrice = new System.Windows.Forms.TextBox();
            this.lblArrow2 = new System.Windows.Forms.Label();
            this.lblNewPrice = new System.Windows.Forms.Label();
            this.txtNewPrice = new System.Windows.Forms.TextBox();

            this.lblReplyDeadline = new System.Windows.Forms.Label();
            this.dtpReplyDeadline = new System.Windows.Forms.DateTimePicker();

            this.lblReferenceNote = new System.Windows.Forms.Label();
            this.txtReferenceNote = new System.Windows.Forms.TextBox();

            this.btnPreview = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.panelTopHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTargets)).BeginInit();
            this.panelDetail.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // splitMain
            // ════════════════════════════════════════
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitMain.SplitterDistance = 320;
            this.splitMain.Name = "splitMain";

            // ── Panel1 (상단): 헤더 + DataGridView ──
            this.splitMain.Panel1.Controls.Add(this.dgvTargets);
            this.splitMain.Panel1.Controls.Add(this.panelTopHeader);

            // ── Panel2 (하단): 수기 입력 폼 ──
            this.splitMain.Panel2.Controls.Add(this.panelDetail);

            // ════════════════════════════════════════
            // panelTopHeader
            // ════════════════════════════════════════
            this.panelTopHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTopHeader.Height = 44;
            this.panelTopHeader.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.panelTopHeader.Controls.Add(this.btnRefresh);
            this.panelTopHeader.Controls.Add(this.lblTitle);
            this.panelTopHeader.Name = "panelTopHeader";

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Text = "계약 갱신 대상 (오피스 / 33일 이내)";
            this.lblTitle.Name = "lblTitle";

            // btnRefresh
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.Location = new System.Drawing.Point(830, 8);
            this.btnRefresh.Size = new System.Drawing.Size(80, 28);
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.btnRefresh.Name = "btnRefresh";

            // ════════════════════════════════════════
            // dgvTargets
            // ════════════════════════════════════════
            this.dgvTargets.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTargets.AllowUserToAddRows = false;
            this.dgvTargets.AllowUserToDeleteRows = false;
            this.dgvTargets.ReadOnly = true;
            this.dgvTargets.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTargets.MultiSelect = false;
            this.dgvTargets.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvTargets.RowHeadersVisible = false;
            this.dgvTargets.BackgroundColor = System.Drawing.Color.White;
            this.dgvTargets.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvTargets.AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(245, 248, 250)
            };
            this.dgvTargets.ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(52, 73, 94),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold),
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
            };
            this.dgvTargets.EnableHeadersVisualStyles = false;
            this.dgvTargets.Name = "dgvTargets";

            // 컬럼 정의
            this.dgvTargets.Columns.Add("colCust", "회사명");
            this.dgvTargets.Columns.Add("colBranch", "지점");
            this.dgvTargets.Columns.Add("colProduct", "상품/분류");
            this.dgvTargets.Columns.Add("colRoom", "호실");
            this.dgvTargets.Columns.Add("colDateTo", "계약종료일");
            this.dgvTargets.Columns.Add("colDays", "남은일수");
            this.dgvTargets.Columns.Add("colDeposit", "예치금");
            this.dgvTargets.Columns.Add("colPrice", "이용료");
            this.dgvTargets.Columns.Add("colEmail", "이메일");

            // ════════════════════════════════════════
            // panelDetail (하단 수기 입력 영역)
            // ════════════════════════════════════════
            this.panelDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDetail.AutoScroll = true;
            this.panelDetail.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
            this.panelDetail.Name = "panelDetail";

            // ── 선택 정보 ──
            // lblSelected
            this.lblSelected.AutoSize = true;
            this.lblSelected.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.lblSelected.Location = new System.Drawing.Point(16, 12);
            this.lblSelected.Text = "선택된 계약:";
            this.lblSelected.Name = "lblSelected";

            // lblSelectedInfo
            this.lblSelectedInfo.AutoSize = true;
            this.lblSelectedInfo.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSelectedInfo.ForeColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.lblSelectedInfo.Location = new System.Drawing.Point(100, 12);
            this.lblSelectedInfo.Text = "(목록에서 항목을 선택하세요)";
            this.lblSelectedInfo.Name = "lblSelectedInfo";

            // ── 예치금 행 ──
            int row1Y = 42;

            this.lblCurrentDeposit.AutoSize = true;
            this.lblCurrentDeposit.Location = new System.Drawing.Point(16, row1Y + 4);
            this.lblCurrentDeposit.Text = "예치금:";
            this.lblCurrentDeposit.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblCurrentDeposit.Name = "lblCurrentDeposit";

            this.txtCurrentDeposit.Location = new System.Drawing.Point(80, row1Y);
            this.txtCurrentDeposit.Size = new System.Drawing.Size(140, 23);
            this.txtCurrentDeposit.ReadOnly = true;
            this.txtCurrentDeposit.BackColor = System.Drawing.Color.FromArgb(236, 240, 241);
            this.txtCurrentDeposit.Name = "txtCurrentDeposit";

            this.lblArrow1.AutoSize = true;
            this.lblArrow1.Location = new System.Drawing.Point(228, row1Y + 4);
            this.lblArrow1.Text = "→";
            this.lblArrow1.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblArrow1.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            this.lblArrow1.Name = "lblArrow1";

            this.lblNewDeposit.AutoSize = true;
            this.lblNewDeposit.Location = new System.Drawing.Point(256, row1Y + 4);
            this.lblNewDeposit.Text = "변경:";
            this.lblNewDeposit.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblNewDeposit.Name = "lblNewDeposit";

            this.txtNewDeposit.Location = new System.Drawing.Point(300, row1Y);
            this.txtNewDeposit.Size = new System.Drawing.Size(160, 23);
            this.txtNewDeposit.Name = "txtNewDeposit";

            // ── 이용료 행 ──
            int row2Y = row1Y + 34;

            this.lblCurrentPrice.AutoSize = true;
            this.lblCurrentPrice.Location = new System.Drawing.Point(16, row2Y + 4);
            this.lblCurrentPrice.Text = "이용료:";
            this.lblCurrentPrice.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblCurrentPrice.Name = "lblCurrentPrice";

            this.txtCurrentPrice.Location = new System.Drawing.Point(80, row2Y);
            this.txtCurrentPrice.Size = new System.Drawing.Size(140, 23);
            this.txtCurrentPrice.ReadOnly = true;
            this.txtCurrentPrice.BackColor = System.Drawing.Color.FromArgb(236, 240, 241);
            this.txtCurrentPrice.Name = "txtCurrentPrice";

            this.lblArrow2.AutoSize = true;
            this.lblArrow2.Location = new System.Drawing.Point(228, row2Y + 4);
            this.lblArrow2.Text = "→";
            this.lblArrow2.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblArrow2.ForeColor = System.Drawing.Color.FromArgb(231, 76, 60);
            this.lblArrow2.Name = "lblArrow2";

            this.lblNewPrice.AutoSize = true;
            this.lblNewPrice.Location = new System.Drawing.Point(256, row2Y + 4);
            this.lblNewPrice.Text = "변경:";
            this.lblNewPrice.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblNewPrice.Name = "lblNewPrice";

            this.txtNewPrice.Location = new System.Drawing.Point(300, row2Y);
            this.txtNewPrice.Size = new System.Drawing.Size(160, 23);
            this.txtNewPrice.Name = "txtNewPrice";

            // ── 회신요청일 행 ──
            int row3Y = row2Y + 34;

            this.lblReplyDeadline.AutoSize = true;
            this.lblReplyDeadline.Location = new System.Drawing.Point(16, row3Y + 4);
            this.lblReplyDeadline.Text = "회신요청일:";
            this.lblReplyDeadline.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblReplyDeadline.Name = "lblReplyDeadline";

            this.dtpReplyDeadline.Location = new System.Drawing.Point(100, row3Y);
            this.dtpReplyDeadline.Size = new System.Drawing.Size(200, 23);
            this.dtpReplyDeadline.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpReplyDeadline.Name = "dtpReplyDeadline";

            // ── 참조안내사항 행 ──
            int row4Y = row3Y + 34;

            this.lblReferenceNote.AutoSize = true;
            this.lblReferenceNote.Location = new System.Drawing.Point(16, row4Y + 4);
            this.lblReferenceNote.Text = "참조 안내사항:";
            this.lblReferenceNote.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblReferenceNote.Name = "lblReferenceNote";

            this.txtReferenceNote.Location = new System.Drawing.Point(120, row4Y);
            this.txtReferenceNote.Size = new System.Drawing.Size(560, 70);
            this.txtReferenceNote.Multiline = true;
            this.txtReferenceNote.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtReferenceNote.Name = "txtReferenceNote";

            // ── 버튼 행 ──
            int btnY = row4Y + 80;

            this.btnPreview.Location = new System.Drawing.Point(480, btnY);
            this.btnPreview.Size = new System.Drawing.Size(96, 32);
            this.btnPreview.Text = "미리보기";
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Name = "btnPreview";

            this.btnSend.Location = new System.Drawing.Point(584, btnY);
            this.btnSend.Size = new System.Drawing.Size(96, 32);
            this.btnSend.Text = "발송";
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnSend.Name = "btnSend";

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(16, btnY + 8);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Text = "";
            this.lblStatus.Name = "lblStatus";

            // ── panelDetail에 컨트롤 추가 ──
            this.panelDetail.Controls.Add(this.lblSelected);
            this.panelDetail.Controls.Add(this.lblSelectedInfo);
            this.panelDetail.Controls.Add(this.lblCurrentDeposit);
            this.panelDetail.Controls.Add(this.txtCurrentDeposit);
            this.panelDetail.Controls.Add(this.lblArrow1);
            this.panelDetail.Controls.Add(this.lblNewDeposit);
            this.panelDetail.Controls.Add(this.txtNewDeposit);
            this.panelDetail.Controls.Add(this.lblCurrentPrice);
            this.panelDetail.Controls.Add(this.txtCurrentPrice);
            this.panelDetail.Controls.Add(this.lblArrow2);
            this.panelDetail.Controls.Add(this.lblNewPrice);
            this.panelDetail.Controls.Add(this.txtNewPrice);
            this.panelDetail.Controls.Add(this.lblReplyDeadline);
            this.panelDetail.Controls.Add(this.dtpReplyDeadline);
            this.panelDetail.Controls.Add(this.lblReferenceNote);
            this.panelDetail.Controls.Add(this.txtReferenceNote);
            this.panelDetail.Controls.Add(this.btnPreview);
            this.panelDetail.Controls.Add(this.btnSend);
            this.panelDetail.Controls.Add(this.lblStatus);

            // ════════════════════════════════════════
            // RenewalManualForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 620);
            this.Controls.Add(this.splitMain);
            this.MinimumSize = new System.Drawing.Size(800, 550);
            this.Name = "RenewalManualForm";
            this.Text = "계약 갱신 수동 발송 (오피스)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.ResumeLayout(false);
            this.panelTopHeader.ResumeLayout(false);
            this.panelTopHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTargets)).EndInit();
            this.panelDetail.ResumeLayout(false);
            this.panelDetail.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel panelTopHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.DataGridView dgvTargets;
        private System.Windows.Forms.Panel panelDetail;
        private System.Windows.Forms.Label lblSelected;
        private System.Windows.Forms.Label lblSelectedInfo;
        private System.Windows.Forms.Label lblCurrentDeposit;
        private System.Windows.Forms.TextBox txtCurrentDeposit;
        private System.Windows.Forms.Label lblArrow1;
        private System.Windows.Forms.Label lblNewDeposit;
        private System.Windows.Forms.TextBox txtNewDeposit;
        private System.Windows.Forms.Label lblCurrentPrice;
        private System.Windows.Forms.TextBox txtCurrentPrice;
        private System.Windows.Forms.Label lblArrow2;
        private System.Windows.Forms.Label lblNewPrice;
        private System.Windows.Forms.TextBox txtNewPrice;
        private System.Windows.Forms.Label lblReplyDeadline;
        private System.Windows.Forms.DateTimePicker dtpReplyDeadline;
        private System.Windows.Forms.Label lblReferenceNote;
        private System.Windows.Forms.TextBox txtReferenceNote;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblStatus;
    }
}
