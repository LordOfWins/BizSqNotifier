using System.Drawing;

namespace BizSqNotifier
{
    partial class MainForm
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
            // ── 상단 헤더 ──
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblLastUpdate = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();

            // ── 요약 카드 패널 ──
            this.panelCards = new System.Windows.Forms.Panel();

            this.cardSend = new System.Windows.Forms.Panel();
            this.lblCardSendTitle = new System.Windows.Forms.Label();
            this.lblCardSendCount = new System.Windows.Forms.Label();
            this.lblCardSendSub = new System.Windows.Forms.Label();

            this.cardUnpaid = new System.Windows.Forms.Panel();
            this.lblCardUnpaidTitle = new System.Windows.Forms.Label();
            this.lblCardUnpaidCount = new System.Windows.Forms.Label();
            this.lblCardUnpaidSub = new System.Windows.Forms.Label();

            this.cardRenewal = new System.Windows.Forms.Panel();
            this.lblCardRenewalTitle = new System.Windows.Forms.Label();
            this.lblCardRenewalCount = new System.Windows.Forms.Label();
            this.lblCardRenewalSub = new System.Windows.Forms.Label();

            this.cardMoveOut = new System.Windows.Forms.Panel();
            this.lblCardMoveOutTitle = new System.Windows.Forms.Label();
            this.lblCardMoveOutCount = new System.Windows.Forms.Label();
            this.lblCardMoveOutSub = new System.Windows.Forms.Label();

            // ── 오늘 발송 현황 그리드 ──
            this.lblGridTitle = new System.Windows.Forms.Label();
            this.dgvToday = new System.Windows.Forms.DataGridView();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSuccess = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSkip = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTotal = new System.Windows.Forms.DataGridViewTextBoxColumn();

            // ── 하단 메뉴 버튼 ──
            this.panelMenu = new System.Windows.Forms.Panel();
            this.btnRenewalList = new System.Windows.Forms.Button();
            this.btnManualSend = new System.Windows.Forms.Button();
            this.btnLogView = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();

            this.panelHeader.SuspendLayout();
            this.panelCards.SuspendLayout();
            this.cardSend.SuspendLayout();
            this.cardUnpaid.SuspendLayout();
            this.cardRenewal.SuspendLayout();
            this.cardMoveOut.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvToday)).BeginInit();
            this.panelMenu.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // panelHeader
            // ════════════════════════════════════════
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 52;
            this.panelHeader.BackColor = Color.FromArgb(44, 62, 80);
            this.panelHeader.Name = "panelHeader";

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Location = new Point(16, 14);
            this.lblTitle.Text = "BizSqNotifier";
            this.lblTitle.Name = "lblTitle";

            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new Font("맑은 고딕", 9F);
            this.lblVersion.ForeColor = Color.FromArgb(149, 165, 166);
            this.lblVersion.Location = new Point(170, 20);
            this.lblVersion.Text = "";
            this.lblVersion.Name = "lblVersion";

            this.lblLastUpdate.AutoSize = true;
            this.lblLastUpdate.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lblLastUpdate.Font = new Font("맑은 고딕", 8.5F);
            this.lblLastUpdate.ForeColor = Color.FromArgb(149, 165, 166);
            this.lblLastUpdate.Location = new Point(640, 20);
            this.lblLastUpdate.Text = "";
            this.lblLastUpdate.Name = "lblLastUpdate";

            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.Location = new Point(820, 12);
            this.btnRefresh.Size = new Size(70, 28);
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(149, 165, 166);
            this.btnRefresh.Font = new Font("맑은 고딕", 8.5F);
            this.btnRefresh.Name = "btnRefresh";

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblVersion);
            this.panelHeader.Controls.Add(this.lblLastUpdate);
            this.panelHeader.Controls.Add(this.btnRefresh);

            // ════════════════════════════════════════
            // panelCards — 요약 카드 4개
            // ════════════════════════════════════════
            this.panelCards.Location = new Point(0, 52);
            this.panelCards.Size = new Size(910, 110);
            this.panelCards.Name = "panelCards";

            int cardW = 208;
            int cardH = 90;
            int cardGap = 12;
            int cardStartX = 16;
            int cardY = 10;

            // ── Card: 오늘 발송 ──
            BuildCard(this.cardSend, this.lblCardSendTitle, this.lblCardSendCount, this.lblCardSendSub,
                "오늘 발송", Color.FromArgb(39, 174, 96),
                cardStartX, cardY, cardW, cardH);

            // ── Card: 미납 ──
            BuildCard(this.cardUnpaid, this.lblCardUnpaidTitle, this.lblCardUnpaidCount, this.lblCardUnpaidSub,
                "미납 대상", Color.FromArgb(231, 76, 60),
                cardStartX + (cardW + cardGap) * 1, cardY, cardW, cardH);

            // ── Card: 갱신 예정 ──
            BuildCard(this.cardRenewal, this.lblCardRenewalTitle, this.lblCardRenewalCount, this.lblCardRenewalSub,
                "갱신 예정", Color.FromArgb(41, 128, 185),
                cardStartX + (cardW + cardGap) * 2, cardY, cardW, cardH);

            // ── Card: 퇴실 예정 ──
            BuildCard(this.cardMoveOut, this.lblCardMoveOutTitle, this.lblCardMoveOutCount, this.lblCardMoveOutSub,
                "퇴실 예정", Color.FromArgb(142, 68, 173),
                cardStartX + (cardW + cardGap) * 3, cardY, cardW, cardH);

            this.panelCards.Controls.Add(this.cardSend);
            this.panelCards.Controls.Add(this.cardUnpaid);
            this.panelCards.Controls.Add(this.cardRenewal);
            this.panelCards.Controls.Add(this.cardMoveOut);

            // ════════════════════════════════════════
            // 오늘 발송 현황 그리드
            // ════════════════════════════════════════
            this.lblGridTitle.AutoSize = true;
            this.lblGridTitle.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this.lblGridTitle.ForeColor = Color.FromArgb(44, 62, 80);
            this.lblGridTitle.Location = new Point(16, 168);
            this.lblGridTitle.Text = "오늘 발송 현황";
            this.lblGridTitle.Name = "lblGridTitle";

            this.dgvToday.Location = new Point(16, 194);
            this.dgvToday.Size = new Size(880, 230);
            this.dgvToday.AllowUserToAddRows = false;
            this.dgvToday.AllowUserToDeleteRows = false;
            this.dgvToday.ReadOnly = true;
            this.dgvToday.RowHeadersVisible = false;
            this.dgvToday.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvToday.BackgroundColor = Color.White;
            this.dgvToday.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dgvToday.AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(245, 248, 250)
            };
            this.dgvToday.ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter
            };
            this.dgvToday.EnableHeadersVisualStyles = false;
            this.dgvToday.Name = "dgvToday";

            // 컬럼 정의
            this.colType.HeaderText = "유형";
            this.colType.Name = "colType";
            this.colType.Width = 150;
            this.colType.ReadOnly = true;

            this.colSuccess.HeaderText = "성공";
            this.colSuccess.Name = "colSuccess";
            this.colSuccess.Width = 100;
            this.colSuccess.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colSuccess.DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);

            this.colFail.HeaderText = "실패";
            this.colFail.Name = "colFail";
            this.colFail.Width = 100;
            this.colFail.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;

            this.colSkip.HeaderText = "SKIP";
            this.colSkip.Name = "colSkip";
            this.colSkip.Width = 100;
            this.colSkip.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colSkip.DefaultCellStyle.ForeColor = Color.FromArgb(211, 84, 0);

            this.colTotal.HeaderText = "합계";
            this.colTotal.Name = "colTotal";
            this.colTotal.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colTotal.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.colTotal.DefaultCellStyle.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);

            this.dgvToday.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                this.colType, this.colSuccess, this.colFail, this.colSkip, this.colTotal
            });

            // ════════════════════════════════════════
            // panelMenu — 하단 메뉴 버튼
            // ════════════════════════════════════════
            this.panelMenu.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelMenu.Height = 64;
            this.panelMenu.BackColor = Color.FromArgb(236, 240, 241);
            this.panelMenu.Name = "panelMenu";

            int btnW = 140;
            int btnH = 40;
            int btnGap = 16;
            int btnY = 12;
            int btnStartX = 16;

            BuildMenuButton(this.btnRenewalList, "갱신 대상 목록",
                Color.FromArgb(41, 128, 185), btnStartX, btnY, btnW, btnH);

            BuildMenuButton(this.btnManualSend, "수동 발송",
                Color.FromArgb(39, 174, 96), btnStartX + (btnW + btnGap) * 1, btnY, btnW, btnH);

            BuildMenuButton(this.btnLogView, "발송 현황",
                Color.FromArgb(44, 62, 80), btnStartX + (btnW + btnGap) * 2, btnY, btnW, btnH);

            BuildMenuButton(this.btnSettings, "설정",
                Color.FromArgb(127, 140, 141), btnStartX + (btnW + btnGap) * 3, btnY, btnW, btnH);

            this.panelMenu.Controls.Add(this.btnRenewalList);
            this.panelMenu.Controls.Add(this.btnManualSend);
            this.panelMenu.Controls.Add(this.btnLogView);
            this.panelMenu.Controls.Add(this.btnSettings);

            // ════════════════════════════════════════
            // MainForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(910, 530);
            this.Controls.Add(this.dgvToday);
            this.Controls.Add(this.lblGridTitle);
            this.Controls.Add(this.panelCards);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelMenu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "BizSqNotifier — 공유오피스 자동 메일 발송 관리";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelCards.ResumeLayout(false);
            this.cardSend.ResumeLayout(false);
            this.cardSend.PerformLayout();
            this.cardUnpaid.ResumeLayout(false);
            this.cardUnpaid.PerformLayout();
            this.cardRenewal.ResumeLayout(false);
            this.cardRenewal.PerformLayout();
            this.cardMoveOut.ResumeLayout(false);
            this.cardMoveOut.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvToday)).EndInit();
            this.panelMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        #region 카드 / 버튼 빌더 헬퍼

        /// <summary>
        /// 요약 카드 UI를 구성합니다.
        /// </summary>
        private static void BuildCard(
            System.Windows.Forms.Panel card,
            System.Windows.Forms.Label titleLbl,
            System.Windows.Forms.Label countLbl,
            System.Windows.Forms.Label subLbl,
            string title, Color accentColor,
            int x, int y, int w, int h)
        {
            card.Location = new Point(x, y);
            card.Size = new Size(w, h);
            card.BackColor = Color.White;
            card.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            card.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);

            // 좌측 색상 바 효과 (Panel 안에 작은 Panel)
            var bar = new System.Windows.Forms.Panel
            {
                Location = new Point(0, 0),
                Size = new Size(4, h),
                BackColor = accentColor
            };
            card.Controls.Add(bar);

            titleLbl.AutoSize = true;
            titleLbl.Font = new Font("맑은 고딕", 9F);
            titleLbl.ForeColor = Color.FromArgb(127, 140, 141);
            titleLbl.Location = new Point(14, 10);
            titleLbl.Text = title;

            countLbl.AutoSize = true;
            countLbl.Font = new Font("맑은 고딕", 22F, FontStyle.Bold);
            countLbl.ForeColor = accentColor;
            countLbl.Location = new Point(14, 30);
            countLbl.Text = "-";

            subLbl.AutoSize = true;
            subLbl.Font = new Font("맑은 고딕", 8.5F);
            subLbl.ForeColor = Color.FromArgb(149, 165, 166);
            subLbl.Location = new Point(14, 66);
            subLbl.Text = "";

            card.Controls.Add(titleLbl);
            card.Controls.Add(countLbl);
            card.Controls.Add(subLbl);
        }

        /// <summary>
        /// 하단 메뉴 버튼 UI를 구성합니다.
        /// </summary>
        private static void BuildMenuButton(
            System.Windows.Forms.Button btn,
            string text, Color bgColor,
            int x, int y, int w, int h)
        {
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.Text = text;
            btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn.BackColor = bgColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = System.Windows.Forms.Cursors.Hand;
        }

        #endregion

        // ── 필드 선언 ──
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblLastUpdate;
        private System.Windows.Forms.Button btnRefresh;

        private System.Windows.Forms.Panel panelCards;
        private System.Windows.Forms.Panel cardSend;
        private System.Windows.Forms.Label lblCardSendTitle;
        private System.Windows.Forms.Label lblCardSendCount;
        private System.Windows.Forms.Label lblCardSendSub;
        private System.Windows.Forms.Panel cardUnpaid;
        private System.Windows.Forms.Label lblCardUnpaidTitle;
        private System.Windows.Forms.Label lblCardUnpaidCount;
        private System.Windows.Forms.Label lblCardUnpaidSub;
        private System.Windows.Forms.Panel cardRenewal;
        private System.Windows.Forms.Label lblCardRenewalTitle;
        private System.Windows.Forms.Label lblCardRenewalCount;
        private System.Windows.Forms.Label lblCardRenewalSub;
        private System.Windows.Forms.Panel cardMoveOut;
        private System.Windows.Forms.Label lblCardMoveOutTitle;
        private System.Windows.Forms.Label lblCardMoveOutCount;
        private System.Windows.Forms.Label lblCardMoveOutSub;

        private System.Windows.Forms.Label lblGridTitle;
        private System.Windows.Forms.DataGridView dgvToday;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSuccess;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSkip;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTotal;

        private System.Windows.Forms.Panel panelMenu;
        private System.Windows.Forms.Button btnRenewalList;
        private System.Windows.Forms.Button btnManualSend;
        private System.Windows.Forms.Button btnLogView;
        private System.Windows.Forms.Button btnSettings;
    }
}
