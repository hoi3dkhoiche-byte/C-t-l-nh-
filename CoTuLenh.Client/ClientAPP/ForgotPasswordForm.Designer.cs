namespace ClientAPP;

partial class ForgotPasswordForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pnlSendOtp = new Panel();
        lnkBackFromOtp = new LinkLabel();
        btnSendOtp = new Button();
        lblOtpError = new Label();
        txtEmail = new TextBox();
        lblEmailPrompt = new Label();
        lblTitleOtp = new Label();
        pnlResetPassword = new Panel();
        lnkBackFromReset = new LinkLabel();
        lnkResendOtp = new LinkLabel();
        lblCountdown = new Label();
        btnResetPassword = new Button();
        lblResetError = new Label();
        txtConfirmNewPassword = new TextBox();
        lblConfirmNewPassword = new Label();
        txtNewPassword = new TextBox();
        lblNewPassword = new Label();
        txtOtp = new TextBox();
        lblOtpPrompt = new Label();
        lblTitleReset = new Label();
        timerCountdown = new System.Windows.Forms.Timer(components);
        pnlSendOtp.SuspendLayout();
        pnlResetPassword.SuspendLayout();
        SuspendLayout();
        // 
        // pnlSendOtp
        // 
        pnlSendOtp.Controls.Add(lnkBackFromOtp);
        pnlSendOtp.Controls.Add(btnSendOtp);
        pnlSendOtp.Controls.Add(lblOtpError);
        pnlSendOtp.Controls.Add(txtEmail);
        pnlSendOtp.Controls.Add(lblEmailPrompt);
        pnlSendOtp.Controls.Add(lblTitleOtp);
        pnlSendOtp.Dock = DockStyle.Fill;
        pnlSendOtp.Location = new Point(0, 0);
        pnlSendOtp.Name = "pnlSendOtp";
        pnlSendOtp.Size = new Size(460, 480);
        pnlSendOtp.TabIndex = 0;
        // 
        // lnkBackFromOtp
        // 
        lnkBackFromOtp.AutoSize = true;
        lnkBackFromOtp.Font = new Font("Segoe UI", 10F);
        lnkBackFromOtp.Location = new Point(165, 290);
        lnkBackFromOtp.Name = "lnkBackFromOtp";
        lnkBackFromOtp.Size = new Size(129, 19);
        lnkBackFromOtp.TabIndex = 5;
        lnkBackFromOtp.TabStop = true;
        lnkBackFromOtp.Text = "Quay lại đăng nhập";
        lnkBackFromOtp.LinkClicked += lnkBack_LinkClicked;
        // 
        // btnSendOtp
        // 
        btnSendOtp.BackColor = Color.FromArgb(243, 114, 44);
        btnSendOtp.FlatStyle = FlatStyle.Flat;
        btnSendOtp.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnSendOtp.ForeColor = Color.White;
        btnSendOtp.Location = new Point(140, 230);
        btnSendOtp.Name = "btnSendOtp";
        btnSendOtp.Size = new Size(180, 40);
        btnSendOtp.TabIndex = 4;
        btnSendOtp.Text = "Gửi mã OTP";
        btnSendOtp.UseVisualStyleBackColor = false;
        btnSendOtp.Click += btnSendOtp_Click;
        // 
        // lblOtpError
        // 
        lblOtpError.Font = new Font("Segoe UI", 9F);
        lblOtpError.ForeColor = Color.Red;
        lblOtpError.Location = new Point(45, 190);
        lblOtpError.Name = "lblOtpError";
        lblOtpError.Size = new Size(370, 25);
        lblOtpError.TabIndex = 3;
        lblOtpError.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // txtEmail
        // 
        txtEmail.Font = new Font("Segoe UI", 10F);
        txtEmail.Location = new Point(45, 150);
        txtEmail.Name = "txtEmail";
        txtEmail.PlaceholderText = "Ex: example123@gmail.com";
        txtEmail.Size = new Size(370, 25);
        txtEmail.TabIndex = 2;
        // 
        // lblEmailPrompt
        // 
        lblEmailPrompt.AutoSize = true;
        lblEmailPrompt.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblEmailPrompt.Location = new Point(45, 120);
        lblEmailPrompt.Name = "lblEmailPrompt";
        lblEmailPrompt.Size = new Size(188, 19);
        lblEmailPrompt.TabIndex = 1;
        lblEmailPrompt.Text = "Nhập email để gửi lại OTP:";
        // 
        // lblTitleOtp
        // 
        lblTitleOtp.BackColor = Color.FromArgb(74, 144, 226);
        lblTitleOtp.Dock = DockStyle.Top;
        lblTitleOtp.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTitleOtp.ForeColor = Color.White;
        lblTitleOtp.Location = new Point(0, 0);
        lblTitleOtp.Name = "lblTitleOtp";
        lblTitleOtp.Size = new Size(460, 60);
        lblTitleOtp.TabIndex = 0;
        lblTitleOtp.Text = "Quên mật khẩu";
        lblTitleOtp.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pnlResetPassword
        // 
        pnlResetPassword.Controls.Add(lnkBackFromReset);
        pnlResetPassword.Controls.Add(lnkResendOtp);
        pnlResetPassword.Controls.Add(lblCountdown);
        pnlResetPassword.Controls.Add(btnResetPassword);
        pnlResetPassword.Controls.Add(lblResetError);
        pnlResetPassword.Controls.Add(txtConfirmNewPassword);
        pnlResetPassword.Controls.Add(lblConfirmNewPassword);
        pnlResetPassword.Controls.Add(txtNewPassword);
        pnlResetPassword.Controls.Add(lblNewPassword);
        pnlResetPassword.Controls.Add(txtOtp);
        pnlResetPassword.Controls.Add(lblOtpPrompt);
        pnlResetPassword.Controls.Add(lblTitleReset);
        pnlResetPassword.Dock = DockStyle.Fill;
        pnlResetPassword.Location = new Point(0, 0);
        pnlResetPassword.Name = "pnlResetPassword";
        pnlResetPassword.Size = new Size(460, 480);
        pnlResetPassword.TabIndex = 6;
        pnlResetPassword.Visible = false;
        pnlResetPassword.Paint += pnlResetPassword_Paint;
        // 
        // lnkBackFromReset
        // 
        lnkBackFromReset.AutoSize = true;
        lnkBackFromReset.Font = new Font("Segoe UI", 10F);
        lnkBackFromReset.Location = new Point(165, 370);
        lnkBackFromReset.Name = "lnkBackFromReset";
        lnkBackFromReset.Size = new Size(129, 19);
        lnkBackFromReset.TabIndex = 11;
        lnkBackFromReset.TabStop = true;
        lnkBackFromReset.Text = "Quay lại đăng nhập";
        lnkBackFromReset.LinkClicked += lnkBack_LinkClicked;
        // 
        // lnkResendOtp
        // 
        lnkResendOtp.AutoSize = true;
        lnkResendOtp.Enabled = false;
        lnkResendOtp.Font = new Font("Segoe UI", 9F);
        lnkResendOtp.Location = new Point(240, 112);
        lnkResendOtp.Name = "lnkResendOtp";
        lnkResendOtp.Size = new Size(85, 15);
        lnkResendOtp.TabIndex = 4;
        lnkResendOtp.TabStop = true;
        lnkResendOtp.Text = "Gửi lại mã OTP";
        lnkResendOtp.Visible = false;
        lnkResendOtp.LinkClicked += lnkResendOtp_LinkClicked;
        // 
        // lblCountdown
        // 
        lblCountdown.AutoSize = true;
        lblCountdown.Font = new Font("Segoe UI", 9F);
        lblCountdown.ForeColor = Color.Gray;
        lblCountdown.Location = new Point(240, 112);
        lblCountdown.Name = "lblCountdown";
        lblCountdown.Size = new Size(84, 15);
        lblCountdown.TabIndex = 3;
        lblCountdown.Text = "Gửi lại sau: 60s";
        // 
        // btnResetPassword
        // 
        btnResetPassword.BackColor = Color.FromArgb(243, 114, 44);
        btnResetPassword.FlatStyle = FlatStyle.Flat;
        btnResetPassword.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnResetPassword.ForeColor = Color.White;
        btnResetPassword.Location = new Point(140, 315);
        btnResetPassword.Name = "btnResetPassword";
        btnResetPassword.Size = new Size(180, 40);
        btnResetPassword.TabIndex = 10;
        btnResetPassword.Text = "Đổi mật khẩu";
        btnResetPassword.UseVisualStyleBackColor = false;
        btnResetPassword.Click += btnResetPassword_Click;
        // 
        // lblResetError
        // 
        lblResetError.Font = new Font("Segoe UI", 9F);
        lblResetError.ForeColor = Color.Red;
        lblResetError.Location = new Point(45, 280);
        lblResetError.Name = "lblResetError";
        lblResetError.Size = new Size(370, 25);
        lblResetError.TabIndex = 9;
        lblResetError.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // txtConfirmNewPassword
        // 
        txtConfirmNewPassword.Font = new Font("Segoe UI", 10F);
        txtConfirmNewPassword.Location = new Point(45, 240);
        txtConfirmNewPassword.Name = "txtConfirmNewPassword";
        txtConfirmNewPassword.PlaceholderText = "Xác nhận mật khẩu mới";
        txtConfirmNewPassword.Size = new Size(370, 25);
        txtConfirmNewPassword.TabIndex = 8;
        txtConfirmNewPassword.UseSystemPasswordChar = true;
        // 
        // lblConfirmNewPassword
        // 
        lblConfirmNewPassword.AutoSize = true;
        lblConfirmNewPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblConfirmNewPassword.Location = new Point(45, 215);
        lblConfirmNewPassword.Name = "lblConfirmNewPassword";
        lblConfirmNewPassword.Size = new Size(165, 19);
        lblConfirmNewPassword.TabIndex = 7;
        lblConfirmNewPassword.Text = "Nhập lại mật khẩu mới:";
        // 
        // txtNewPassword
        // 
        txtNewPassword.Font = new Font("Segoe UI", 10F);
        txtNewPassword.Location = new Point(45, 175);
        txtNewPassword.Name = "txtNewPassword";
        txtNewPassword.PlaceholderText = "Nhập mật khẩu mới";
        txtNewPassword.Size = new Size(370, 25);
        txtNewPassword.TabIndex = 6;
        txtNewPassword.UseSystemPasswordChar = true;
        // 
        // lblNewPassword
        // 
        lblNewPassword.AutoSize = true;
        lblNewPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblNewPassword.Location = new Point(45, 150);
        lblNewPassword.Name = "lblNewPassword";
        lblNewPassword.Size = new Size(105, 19);
        lblNewPassword.TabIndex = 5;
        lblNewPassword.Text = "Mật khẩu mới:";
        // 
        // txtOtp
        // 
        txtOtp.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        txtOtp.Location = new Point(45, 105);
        txtOtp.MaxLength = 6;
        txtOtp.Name = "txtOtp";
        txtOtp.PlaceholderText = "Nhập 6 số OTP";
        txtOtp.Size = new Size(180, 29);
        txtOtp.TabIndex = 2;
        txtOtp.TextAlign = HorizontalAlignment.Center;
        // 
        // lblOtpPrompt
        // 
        lblOtpPrompt.AutoSize = true;
        lblOtpPrompt.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblOtpPrompt.Location = new Point(45, 80);
        lblOtpPrompt.Name = "lblOtpPrompt";
        lblOtpPrompt.Size = new Size(134, 19);
        lblOtpPrompt.TabIndex = 1;
        lblOtpPrompt.Text = "Mã OTP (6 chữ số):";
        // 
        // lblTitleReset
        // 
        lblTitleReset.BackColor = Color.FromArgb(74, 144, 226);
        lblTitleReset.Dock = DockStyle.Top;
        lblTitleReset.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTitleReset.ForeColor = Color.White;
        lblTitleReset.Location = new Point(0, 0);
        lblTitleReset.Name = "lblTitleReset";
        lblTitleReset.Size = new Size(460, 60);
        lblTitleReset.TabIndex = 0;
        lblTitleReset.Text = "Đặt lại mật khẩu";
        lblTitleReset.TextAlign = ContentAlignment.MiddleCenter;
        lblTitleReset.Click += lblTitleReset_Click;
        // 
        // timerCountdown
        // 
        timerCountdown.Interval = 1000;
        timerCountdown.Tick += timerCountdown_Tick;
        // 
        // ForgotPasswordForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(220, 224, 230);
        ClientSize = new Size(460, 480);
        Controls.Add(pnlResetPassword);
        Controls.Add(pnlSendOtp);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "ForgotPasswordForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Quên mật khẩu - Cờ Tư Lệnh";
        pnlSendOtp.ResumeLayout(false);
        pnlSendOtp.PerformLayout();
        pnlResetPassword.ResumeLayout(false);
        pnlResetPassword.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlSendOtp;
    private Label lblTitleOtp;
    private Label lblEmailPrompt;
    private TextBox txtEmail;
    private Label lblOtpError;
    private Button btnSendOtp;
    private LinkLabel lnkBackFromOtp;

    private Panel pnlResetPassword;
    private Label lblTitleReset;
    private Label lblOtpPrompt;
    private TextBox txtOtp;
    private Label lblCountdown;
    private LinkLabel lnkResendOtp;
    private Label lblNewPassword;
    private TextBox txtNewPassword;
    private Label lblConfirmNewPassword;
    private TextBox txtConfirmNewPassword;
    private Label lblResetError;
    private Button btnResetPassword;
    private LinkLabel lnkBackFromReset;
    private System.Windows.Forms.Timer timerCountdown;
}
