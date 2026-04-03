using System.Drawing;

namespace BizSqNotifier
{
    partial class LoginForm
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
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubTitle = new System.Windows.Forms.Label();
            this.panelBody = new System.Windows.Forms.Panel();
            this.lblUserId = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();

            this.panelHeader.SuspendLayout();
            this.panelBody.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // panelHeader
            // ════════════════════════════════════════
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 70;
            this.panelHeader.BackColor = Color.FromArgb(44, 62, 80);
            this.panelHeader.Name = "panelHeader";

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("맑은 고딕", 15F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Location = new Point(24, 12);
            this.lblTitle.Text = "BizSqNotifier";
            this.lblTitle.Name = "lblTitle";

            this.lblSubTitle.AutoSize = true;
            this.lblSubTitle.Font = new Font("맑은 고딕", 9F);
            this.lblSubTitle.ForeColor = Color.FromArgb(149, 165, 166);
            this.lblSubTitle.Location = new Point(26, 42);
            this.lblSubTitle.Text = "공유오피스 자동 메일 발송 관리";
            this.lblSubTitle.Name = "lblSubTitle";

            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.lblSubTitle);

            // ════════════════════════════════════════
            // panelBody
            // ════════════════════════════════════════
            this.panelBody.Location = new Point(0, 70);
            this.panelBody.Size = new Size(380, 220);
            this.panelBody.BackColor = Color.White;
            this.panelBody.Name = "panelBody";

            // 아이디
            this.lblUserId.AutoSize = true;
            this.lblUserId.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            this.lblUserId.ForeColor = Color.FromArgb(44, 62, 80);
            this.lblUserId.Location = new Point(40, 24);
            this.lblUserId.Text = "아이디";
            this.lblUserId.Name = "lblUserId";

            this.txtUserId.Location = new Point(40, 48);
            this.txtUserId.Size = new Size(300, 26);
            this.txtUserId.Font = new Font("맑은 고딕", 10F);
            this.txtUserId.Name = "txtUserId";

            // 비밀번호
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            this.lblPassword.ForeColor = Color.FromArgb(44, 62, 80);
            this.lblPassword.Location = new Point(40, 84);
            this.lblPassword.Text = "비밀번호";
            this.lblPassword.Name = "lblPassword";

            this.txtPassword.Location = new Point(40, 108);
            this.txtPassword.Size = new Size(300, 26);
            this.txtPassword.Font = new Font("맑은 고딕", 10F);
            this.txtPassword.UseSystemPasswordChar = true;
            this.txtPassword.Name = "txtPassword";

            // 로그인 버튼
            this.btnLogin.Location = new Point(40, 150);
            this.btnLogin.Size = new Size(145, 38);
            this.btnLogin.Text = "로그인";
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.BackColor = Color.FromArgb(41, 128, 185);
            this.btnLogin.ForeColor = Color.White;
            this.btnLogin.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.Name = "btnLogin";

            // 취소 버튼
            this.btnCancel.Location = new Point(195, 150);
            this.btnCancel.Size = new Size(145, 38);
            this.btnCancel.Text = "취소";
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.BackColor = Color.FromArgb(149, 165, 166);
            this.btnCancel.ForeColor = Color.White;
            this.btnCancel.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Name = "btnCancel";

            // 메시지 레이블
            this.lblMessage.Location = new Point(40, 196);
            this.lblMessage.Size = new Size(300, 18);
            this.lblMessage.Font = new Font("맑은 고딕", 8.5F);
            this.lblMessage.ForeColor = Color.Gray;
            this.lblMessage.Text = "";
            this.lblMessage.Name = "lblMessage";

            this.panelBody.Controls.Add(this.lblUserId);
            this.panelBody.Controls.Add(this.txtUserId);
            this.panelBody.Controls.Add(this.lblPassword);
            this.panelBody.Controls.Add(this.txtPassword);
            this.panelBody.Controls.Add(this.btnLogin);
            this.panelBody.Controls.Add(this.btnCancel);
            this.panelBody.Controls.Add(this.lblMessage);

            // ════════════════════════════════════════
            // LoginForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new Size(380, 290);
            this.Controls.Add(this.panelBody);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.Text = "BizSqNotifier — 로그인";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.AcceptButton = this.btnLogin;
            this.CancelButton = this.btnCancel;

            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelBody.ResumeLayout(false);
            this.panelBody.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubTitle;
        private System.Windows.Forms.Panel panelBody;
        private System.Windows.Forms.Label lblUserId;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblMessage;
    }
}
