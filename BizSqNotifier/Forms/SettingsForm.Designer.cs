namespace BizSqNotifier
{
    partial class SettingsForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPrinter = new System.Windows.Forms.TabPage();
            this.tabSchedule = new System.Windows.Forms.TabPage();
            this.tabConnection = new System.Windows.Forms.TabPage();

            // ── 복합기 탭 컨트롤 ──
            this.lblPrinterId = new System.Windows.Forms.Label();
            this.txtPrinterId = new System.Windows.Forms.TextBox();
            this.lblPrinterPw = new System.Windows.Forms.Label();
            this.txtPrinterPw = new System.Windows.Forms.TextBox();

            // ── 발송 시각/기준일 탭 컨트롤 ──
            this.grpSendTime = new System.Windows.Forms.GroupBox();
            this.chkAutoSend = new System.Windows.Forms.CheckBox();
            this.lblGeneralTime = new System.Windows.Forms.Label();
            this.dtpGeneralTime = new System.Windows.Forms.DateTimePicker();
            this.lblUnpaidTime = new System.Windows.Forms.Label();
            this.dtpUnpaidTime = new System.Windows.Forms.DateTimePicker();

            this.grpDays = new System.Windows.Forms.GroupBox();
            this.lblUnpaid1st = new System.Windows.Forms.Label();
            this.nudUnpaid1st = new System.Windows.Forms.NumericUpDown();
            this.lblUnpaid2nd = new System.Windows.Forms.Label();
            this.nudUnpaid2nd = new System.Windows.Forms.NumericUpDown();
            this.lblUnpaidFinal = new System.Windows.Forms.Label();
            this.nudUnpaidFinal = new System.Windows.Forms.NumericUpDown();
            this.lblRenewalAuto = new System.Windows.Forms.Label();
            this.nudRenewalAuto = new System.Windows.Forms.NumericUpDown();
            this.lblRenewalManual = new System.Windows.Forms.Label();
            this.nudRenewalManual = new System.Windows.Forms.NumericUpDown();
            this.lblMoveOut = new System.Windows.Forms.Label();
            this.nudMoveOut = new System.Windows.Forms.NumericUpDown();

            // ── 연결 테스트 탭 컨트롤 ──
            this.grpDb = new System.Windows.Forms.GroupBox();
            this.btnTestDb = new System.Windows.Forms.Button();
            this.lblDbResult = new System.Windows.Forms.Label();
            this.grpSmtp = new System.Windows.Forms.GroupBox();
            this.lblSmtpBranch = new System.Windows.Forms.Label();
            this.cmbBranch = new System.Windows.Forms.ComboBox();
            this.btnTestSmtp = new System.Windows.Forms.Button();
            this.lblSmtpResult = new System.Windows.Forms.Label();

            // ── 하단 버튼 ──
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblSaveStatus = new System.Windows.Forms.Label();

            this.tabControl.SuspendLayout();
            this.tabPrinter.SuspendLayout();
            this.tabSchedule.SuspendLayout();
            this.tabConnection.SuspendLayout();
            this.grpSendTime.SuspendLayout();
            this.grpDays.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaid1st)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaid2nd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaidFinal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRenewalAuto)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRenewalManual)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMoveOut)).BeginInit();
            this.grpDb.SuspendLayout();
            this.grpSmtp.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();

            // ════════════════════════════════════════
            // tabControl
            // ════════════════════════════════════════
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.TabPages.Add(this.tabPrinter);
            this.tabControl.TabPages.Add(this.tabSchedule);
            this.tabControl.TabPages.Add(this.tabConnection);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(12, 4);

            // ════════════════════════════════════════
            // tabPrinter — 복합기 로그인 정보
            // ════════════════════════════════════════
            this.tabPrinter.Text = "복합기 설정";
            this.tabPrinter.Padding = new System.Windows.Forms.Padding(20, 20, 20, 10);
            this.tabPrinter.Name = "tabPrinter";

            this.lblPrinterId.AutoSize = true;
            this.lblPrinterId.Location = new System.Drawing.Point(20, 30);
            this.lblPrinterId.Text = "복합기 ID:";
            this.lblPrinterId.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.lblPrinterId.Name = "lblPrinterId";

            this.txtPrinterId.Location = new System.Drawing.Point(120, 27);
            this.txtPrinterId.Size = new System.Drawing.Size(300, 23);
            this.txtPrinterId.Name = "txtPrinterId";

            this.lblPrinterPw.AutoSize = true;
            this.lblPrinterPw.Location = new System.Drawing.Point(20, 66);
            this.lblPrinterPw.Text = "복합기 PW:";
            this.lblPrinterPw.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.lblPrinterPw.Name = "lblPrinterPw";

            this.txtPrinterPw.Location = new System.Drawing.Point(120, 63);
            this.txtPrinterPw.Size = new System.Drawing.Size(300, 23);
            this.txtPrinterPw.Name = "txtPrinterPw";

            this.tabPrinter.Controls.Add(this.lblPrinterId);
            this.tabPrinter.Controls.Add(this.txtPrinterId);
            this.tabPrinter.Controls.Add(this.lblPrinterPw);
            this.tabPrinter.Controls.Add(this.txtPrinterPw);

            // ════════════════════════════════════════
            // tabSchedule — 발송 시각 + 기준일
            // ════════════════════════════════════════
            this.tabSchedule.Text = "발송 스케줄";
            this.tabSchedule.Padding = new System.Windows.Forms.Padding(10);
            this.tabSchedule.Name = "tabSchedule";

            // ── grpSendTime ──
            this.grpSendTime.Text = "발송 시각";
            this.grpSendTime.Location = new System.Drawing.Point(14, 12);
            this.grpSendTime.Size = new System.Drawing.Size(520, 120);
            this.grpSendTime.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpSendTime.Name = "grpSendTime";

            this.chkAutoSend.AutoSize = true;
            this.chkAutoSend.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkAutoSend.ForeColor = System.Drawing.Color.FromArgb(192, 57, 43);
            this.chkAutoSend.Location = new System.Drawing.Point(16, 24);
            this.chkAutoSend.Text = "자동 발송 활성화 (체크 해제 시 스케줄 발송 중단)";
            this.chkAutoSend.Name = "chkAutoSend";

            this.lblGeneralTime.AutoSize = true;
            this.lblGeneralTime.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblGeneralTime.Location = new System.Drawing.Point(16, 52);
            this.lblGeneralTime.Text = "입주/퇴실/갱신자동:";
            this.lblGeneralTime.Name = "lblGeneralTime";

            this.dtpGeneralTime.Location = new System.Drawing.Point(160, 48);
            this.dtpGeneralTime.Size = new System.Drawing.Size(100, 23);
            this.dtpGeneralTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpGeneralTime.CustomFormat = "HH:mm";
            this.dtpGeneralTime.ShowUpDown = true;
            this.dtpGeneralTime.Name = "dtpGeneralTime";

            this.lblUnpaidTime.AutoSize = true;
            this.lblUnpaidTime.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUnpaidTime.Location = new System.Drawing.Point(16, 82);
            this.lblUnpaidTime.Text = "미납 안내:";
            this.lblUnpaidTime.Name = "lblUnpaidTime";

            this.dtpUnpaidTime.Location = new System.Drawing.Point(160, 78);
            this.dtpUnpaidTime.Size = new System.Drawing.Size(100, 23);
            this.dtpUnpaidTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpUnpaidTime.CustomFormat = "HH:mm";
            this.dtpUnpaidTime.ShowUpDown = true;
            this.dtpUnpaidTime.Name = "dtpUnpaidTime";

            this.grpSendTime.Controls.Add(this.chkAutoSend);
            this.grpSendTime.Controls.Add(this.lblGeneralTime);
            this.grpSendTime.Controls.Add(this.dtpGeneralTime);
            this.grpSendTime.Controls.Add(this.lblUnpaidTime);
            this.grpSendTime.Controls.Add(this.dtpUnpaidTime);

            // ── grpDays ──
            this.grpDays.Text = "발송 기준일";
            this.grpDays.Location = new System.Drawing.Point(14, 142);
            this.grpDays.Size = new System.Drawing.Size(520, 220);
            this.grpDays.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpDays.Name = "grpDays";

            // 기준일 레이블/NUD 배열 생성
            int dy = 26;
            var dayItems = new[]
            {
                new { Label = "미납 1차 (납부일 경과):", Nud = "nudUnpaid1st",     Y = dy },
                new { Label = "미납 2차 (납부일 경과):", Nud = "nudUnpaid2nd",     Y = dy + 30 },
                new { Label = "미납 최종 (납부일 경과):", Nud = "nudUnpaidFinal",   Y = dy + 60 },
                new { Label = "갱신자동 (계약종료 전):",  Nud = "nudRenewalAuto",   Y = dy + 90 },
                new { Label = "갱신수동 목록 (계약종료 전):", Nud = "nudRenewalManual", Y = dy + 120 },
                new { Label = "퇴실 안내 (퇴실예정 전):", Nud = "nudMoveOut",       Y = dy + 150 }
            };

            // lblUnpaid1st
            this.lblUnpaid1st.AutoSize = true;
            this.lblUnpaid1st.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUnpaid1st.Location = new System.Drawing.Point(16, dayItems[0].Y + 2);
            this.lblUnpaid1st.Text = dayItems[0].Label;
            this.lblUnpaid1st.Name = "lblUnpaid1st";

            this.nudUnpaid1st.Location = new System.Drawing.Point(220, dayItems[0].Y);
            this.nudUnpaid1st.Size = new System.Drawing.Size(70, 23);
            this.nudUnpaid1st.Minimum = 1; this.nudUnpaid1st.Maximum = 90;
            this.nudUnpaid1st.Name = "nudUnpaid1st";

            // lblUnpaid2nd
            this.lblUnpaid2nd.AutoSize = true;
            this.lblUnpaid2nd.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUnpaid2nd.Location = new System.Drawing.Point(16, dayItems[1].Y + 2);
            this.lblUnpaid2nd.Text = dayItems[1].Label;
            this.lblUnpaid2nd.Name = "lblUnpaid2nd";

            this.nudUnpaid2nd.Location = new System.Drawing.Point(220, dayItems[1].Y);
            this.nudUnpaid2nd.Size = new System.Drawing.Size(70, 23);
            this.nudUnpaid2nd.Minimum = 1; this.nudUnpaid2nd.Maximum = 90;
            this.nudUnpaid2nd.Name = "nudUnpaid2nd";

            // lblUnpaidFinal
            this.lblUnpaidFinal.AutoSize = true;
            this.lblUnpaidFinal.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUnpaidFinal.Location = new System.Drawing.Point(16, dayItems[2].Y + 2);
            this.lblUnpaidFinal.Text = dayItems[2].Label;
            this.lblUnpaidFinal.Name = "lblUnpaidFinal";

            this.nudUnpaidFinal.Location = new System.Drawing.Point(220, dayItems[2].Y);
            this.nudUnpaidFinal.Size = new System.Drawing.Size(70, 23);
            this.nudUnpaidFinal.Minimum = 1; this.nudUnpaidFinal.Maximum = 90;
            this.nudUnpaidFinal.Name = "nudUnpaidFinal";

            // lblRenewalAuto
            this.lblRenewalAuto.AutoSize = true;
            this.lblRenewalAuto.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblRenewalAuto.Location = new System.Drawing.Point(16, dayItems[3].Y + 2);
            this.lblRenewalAuto.Text = dayItems[3].Label;
            this.lblRenewalAuto.Name = "lblRenewalAuto";

            this.nudRenewalAuto.Location = new System.Drawing.Point(220, dayItems[3].Y);
            this.nudRenewalAuto.Size = new System.Drawing.Size(70, 23);
            this.nudRenewalAuto.Minimum = 1; this.nudRenewalAuto.Maximum = 90;
            this.nudRenewalAuto.Name = "nudRenewalAuto";

            // lblRenewalManual
            this.lblRenewalManual.AutoSize = true;
            this.lblRenewalManual.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblRenewalManual.Location = new System.Drawing.Point(16, dayItems[4].Y + 2);
            this.lblRenewalManual.Text = dayItems[4].Label;
            this.lblRenewalManual.Name = "lblRenewalManual";

            this.nudRenewalManual.Location = new System.Drawing.Point(220, dayItems[4].Y);
            this.nudRenewalManual.Size = new System.Drawing.Size(70, 23);
            this.nudRenewalManual.Minimum = 1; this.nudRenewalManual.Maximum = 90;
            this.nudRenewalManual.Name = "nudRenewalManual";

            // lblMoveOut
            this.lblMoveOut.AutoSize = true;
            this.lblMoveOut.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblMoveOut.Location = new System.Drawing.Point(16, dayItems[5].Y + 2);
            this.lblMoveOut.Text = dayItems[5].Label;
            this.lblMoveOut.Name = "lblMoveOut";

            this.nudMoveOut.Location = new System.Drawing.Point(220, dayItems[5].Y);
            this.nudMoveOut.Size = new System.Drawing.Size(70, 23);
            this.nudMoveOut.Minimum = 1; this.nudMoveOut.Maximum = 30;
            this.nudMoveOut.Name = "nudMoveOut";

            // 단위 레이블 (일)
            var lblDayUnit1 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[0].Y + 2) };
            var lblDayUnit2 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[1].Y + 2) };
            var lblDayUnit3 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[2].Y + 2) };
            var lblDayUnit4 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[3].Y + 2) };
            var lblDayUnit5 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[4].Y + 2) };
            var lblDayUnit6 = new System.Windows.Forms.Label { AutoSize = true, Text = "일", Font = new System.Drawing.Font("맑은 고딕", 9F), Location = new System.Drawing.Point(296, dayItems[5].Y + 2) };

            this.grpDays.Controls.Add(this.lblUnpaid1st);
            this.grpDays.Controls.Add(this.nudUnpaid1st);
            this.grpDays.Controls.Add(lblDayUnit1);
            this.grpDays.Controls.Add(this.lblUnpaid2nd);
            this.grpDays.Controls.Add(this.nudUnpaid2nd);
            this.grpDays.Controls.Add(lblDayUnit2);
            this.grpDays.Controls.Add(this.lblUnpaidFinal);
            this.grpDays.Controls.Add(this.nudUnpaidFinal);
            this.grpDays.Controls.Add(lblDayUnit3);
            this.grpDays.Controls.Add(this.lblRenewalAuto);
            this.grpDays.Controls.Add(this.nudRenewalAuto);
            this.grpDays.Controls.Add(lblDayUnit4);
            this.grpDays.Controls.Add(this.lblRenewalManual);
            this.grpDays.Controls.Add(this.nudRenewalManual);
            this.grpDays.Controls.Add(lblDayUnit5);
            this.grpDays.Controls.Add(this.lblMoveOut);
            this.grpDays.Controls.Add(this.nudMoveOut);
            this.grpDays.Controls.Add(lblDayUnit6);

            this.tabSchedule.Controls.Add(this.grpSendTime);
            this.tabSchedule.Controls.Add(this.grpDays);

            // ════════════════════════════════════════
            // tabConnection — DB/SMTP 연결 테스트
            // ════════════════════════════════════════
            this.tabConnection.Text = "연결 테스트";
            this.tabConnection.Padding = new System.Windows.Forms.Padding(10);
            this.tabConnection.Name = "tabConnection";

            // ── grpDb ──
            this.grpDb.Text = "DB 연결";
            this.grpDb.Location = new System.Drawing.Point(14, 12);
            this.grpDb.Size = new System.Drawing.Size(520, 70);
            this.grpDb.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpDb.Name = "grpDb";

            this.btnTestDb.Location = new System.Drawing.Point(16, 26);
            this.btnTestDb.Size = new System.Drawing.Size(100, 28);
            this.btnTestDb.Text = "연결 테스트";
            this.btnTestDb.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestDb.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnTestDb.ForeColor = System.Drawing.Color.White;
            this.btnTestDb.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnTestDb.Name = "btnTestDb";

            this.lblDbResult.AutoSize = true;
            this.lblDbResult.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblDbResult.Location = new System.Drawing.Point(128, 32);
            this.lblDbResult.Text = "";
            this.lblDbResult.Name = "lblDbResult";

            this.grpDb.Controls.Add(this.btnTestDb);
            this.grpDb.Controls.Add(this.lblDbResult);

            // ── grpSmtp ──
            this.grpSmtp.Text = "SMTP 연결";
            this.grpSmtp.Location = new System.Drawing.Point(14, 94);
            this.grpSmtp.Size = new System.Drawing.Size(520, 80);
            this.grpSmtp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpSmtp.Name = "grpSmtp";

            this.lblSmtpBranch.AutoSize = true;
            this.lblSmtpBranch.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSmtpBranch.Location = new System.Drawing.Point(16, 30);
            this.lblSmtpBranch.Text = "지점:";
            this.lblSmtpBranch.Name = "lblSmtpBranch";

            this.cmbBranch.Location = new System.Drawing.Point(56, 26);
            this.cmbBranch.Size = new System.Drawing.Size(200, 23);
            this.cmbBranch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBranch.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.cmbBranch.Name = "cmbBranch";

            this.btnTestSmtp.Location = new System.Drawing.Point(268, 24);
            this.btnTestSmtp.Size = new System.Drawing.Size(100, 28);
            this.btnTestSmtp.Text = "SMTP 테스트";
            this.btnTestSmtp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestSmtp.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnTestSmtp.ForeColor = System.Drawing.Color.White;
            this.btnTestSmtp.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnTestSmtp.Name = "btnTestSmtp";

            this.lblSmtpResult.AutoSize = true;
            this.lblSmtpResult.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSmtpResult.Location = new System.Drawing.Point(16, 56);
            this.lblSmtpResult.Text = "";
            this.lblSmtpResult.Name = "lblSmtpResult";

            this.grpSmtp.Controls.Add(this.lblSmtpBranch);
            this.grpSmtp.Controls.Add(this.cmbBranch);
            this.grpSmtp.Controls.Add(this.btnTestSmtp);
            this.grpSmtp.Controls.Add(this.lblSmtpResult);

            // ── grpScheduler ──
            this.grpScheduler = new System.Windows.Forms.GroupBox();
            this.btnRegisterTasks = new System.Windows.Forms.Button();
            this.btnRemoveTasks = new System.Windows.Forms.Button();
            this.lblSchedulerStatus = new System.Windows.Forms.Label();

            this.grpScheduler.Text = "작업 스케줄러";
            this.grpScheduler.Location = new System.Drawing.Point(14, 186);
            this.grpScheduler.Size = new System.Drawing.Size(520, 80);
            this.grpScheduler.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.grpScheduler.Name = "grpScheduler";

            this.btnRegisterTasks.Location = new System.Drawing.Point(16, 26);
            this.btnRegisterTasks.Size = new System.Drawing.Size(110, 28);
            this.btnRegisterTasks.Text = "스케줄러 등록";
            this.btnRegisterTasks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRegisterTasks.BackColor = System.Drawing.Color.FromArgb(41, 128, 185);
            this.btnRegisterTasks.ForeColor = System.Drawing.Color.White;
            this.btnRegisterTasks.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnRegisterTasks.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRegisterTasks.Name = "btnRegisterTasks";

            this.btnRemoveTasks.Location = new System.Drawing.Point(136, 26);
            this.btnRemoveTasks.Size = new System.Drawing.Size(110, 28);
            this.btnRemoveTasks.Text = "스케줄러 해제";
            this.btnRemoveTasks.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveTasks.BackColor = System.Drawing.Color.FromArgb(192, 57, 43);
            this.btnRemoveTasks.ForeColor = System.Drawing.Color.White;
            this.btnRemoveTasks.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.btnRemoveTasks.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRemoveTasks.Name = "btnRemoveTasks";

            this.lblSchedulerStatus.AutoSize = true;
            this.lblSchedulerStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSchedulerStatus.Location = new System.Drawing.Point(16, 58);
            this.lblSchedulerStatus.Text = "";
            this.lblSchedulerStatus.Name = "lblSchedulerStatus";

            this.grpScheduler.Controls.Add(this.btnRegisterTasks);
            this.grpScheduler.Controls.Add(this.btnRemoveTasks);
            this.grpScheduler.Controls.Add(this.lblSchedulerStatus);

            this.tabConnection.Controls.Add(this.grpDb);
            this.tabConnection.Controls.Add(this.grpSmtp);
            this.tabConnection.Controls.Add(this.grpScheduler);

            // ════════════════════════════════════════
            // panelBottom — 저장 / 취소
            // ════════════════════════════════════════
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Height = 52;
            this.panelBottom.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.panelBottom.Name = "panelBottom";

            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(370, 10);
            this.btnSave.Size = new System.Drawing.Size(90, 32);
            this.btnSave.Text = "저장";
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSave.Name = "btnSave";

            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.Location = new System.Drawing.Point(468, 10);
            this.btnCancel.Size = new System.Drawing.Size(90, 32);
            this.btnCancel.Text = "취소";
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Font = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.btnCancel.Name = "btnCancel";

            this.lblSaveStatus.AutoSize = true;
            this.lblSaveStatus.Location = new System.Drawing.Point(14, 18);
            this.lblSaveStatus.Font = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblSaveStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblSaveStatus.Text = "";
            this.lblSaveStatus.Name = "lblSaveStatus";

            this.panelBottom.Controls.Add(this.lblSaveStatus);
            this.panelBottom.Controls.Add(this.btnSave);
            this.panelBottom.Controls.Add(this.btnCancel);

            // ════════════════════════════════════════
            // SettingsForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 440);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.panelBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "관리자 설정";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            this.tabControl.ResumeLayout(false);
            this.tabPrinter.ResumeLayout(false);
            this.tabPrinter.PerformLayout();
            this.tabSchedule.ResumeLayout(false);
            this.tabConnection.ResumeLayout(false);
            this.grpSendTime.ResumeLayout(false);
            this.grpSendTime.PerformLayout();
            this.grpDays.ResumeLayout(false);
            this.grpDays.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaid1st)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaid2nd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudUnpaidFinal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRenewalAuto)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRenewalManual)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMoveOut)).EndInit();
            this.grpDb.ResumeLayout(false);
            this.grpDb.PerformLayout();
            this.grpSmtp.ResumeLayout(false);
            this.grpSmtp.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.ResumeLayout(false);


        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPrinter;
        private System.Windows.Forms.TabPage tabSchedule;
        private System.Windows.Forms.TabPage tabConnection;
        private System.Windows.Forms.GroupBox grpScheduler;
        private System.Windows.Forms.Button btnRegisterTasks;
        private System.Windows.Forms.Button btnRemoveTasks;
        private System.Windows.Forms.Label lblSchedulerStatus;

        private System.Windows.Forms.Label lblPrinterId;
        private System.Windows.Forms.TextBox txtPrinterId;
        private System.Windows.Forms.Label lblPrinterPw;
        private System.Windows.Forms.TextBox txtPrinterPw;

        private System.Windows.Forms.GroupBox grpSendTime;
        private System.Windows.Forms.CheckBox chkAutoSend;
        private System.Windows.Forms.Label lblGeneralTime;
        private System.Windows.Forms.DateTimePicker dtpGeneralTime;
        private System.Windows.Forms.Label lblUnpaidTime;
        private System.Windows.Forms.DateTimePicker dtpUnpaidTime;

        private System.Windows.Forms.GroupBox grpDays;
        private System.Windows.Forms.Label lblUnpaid1st;
        private System.Windows.Forms.NumericUpDown nudUnpaid1st;
        private System.Windows.Forms.Label lblUnpaid2nd;
        private System.Windows.Forms.NumericUpDown nudUnpaid2nd;
        private System.Windows.Forms.Label lblUnpaidFinal;
        private System.Windows.Forms.NumericUpDown nudUnpaidFinal;
        private System.Windows.Forms.Label lblRenewalAuto;
        private System.Windows.Forms.NumericUpDown nudRenewalAuto;
        private System.Windows.Forms.Label lblRenewalManual;
        private System.Windows.Forms.NumericUpDown nudRenewalManual;
        private System.Windows.Forms.Label lblMoveOut;
        private System.Windows.Forms.NumericUpDown nudMoveOut;

        private System.Windows.Forms.GroupBox grpDb;
        private System.Windows.Forms.Button btnTestDb;
        private System.Windows.Forms.Label lblDbResult;
        private System.Windows.Forms.GroupBox grpSmtp;
        private System.Windows.Forms.Label lblSmtpBranch;
        private System.Windows.Forms.ComboBox cmbBranch;
        private System.Windows.Forms.Button btnTestSmtp;
        private System.Windows.Forms.Label lblSmtpResult;

        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblSaveStatus;
    }
}
