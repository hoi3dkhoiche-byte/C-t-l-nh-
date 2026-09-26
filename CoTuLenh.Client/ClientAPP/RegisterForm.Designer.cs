namespace ClientAPP;

partial class RegisterForm
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
        lblTitle = new Label();
        lblUsername = new Label();
        txtUsername = new TextBox();
        lblUsernameHint = new Label();
        lblPassword = new Label();
        txtPassword = new TextBox();
        lblPasswordHint = new Label();
        lblConfirmPassword = new Label();
        txtConfirmPassword = new TextBox();
        lblEmail = new Label();
        txtEmail = new TextBox();
        btnRegister = new Button();
        lnkBackToLogin = new LinkLabel();
        lblErrorMessage = new Label();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.BackColor = Color.FromArgb(74, 144, 226);
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(460, 60);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Đăng ký tài khoản";
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblTitle.Click += lblTitle_Click;
        // 
        // lblUsername
        // 
        lblUsername.AutoSize = true;
        lblUsername.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUsername.Location = new Point(45, 80);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(117, 19);
        lblUsername.TabIndex = 1;
        lblUsername.Text = "Tên đăng nhập*:";
        // 
        // txtUsername
        // 
        txtUsername.Font = new Font("Segoe UI", 10F);
        txtUsername.Location = new Point(45, 102);
        txtUsername.Name = "txtUsername";
        txtUsername.PlaceholderText = "Ex: vuatrochoi123";
        txtUsername.Size = new Size(370, 25);
        txtUsername.TabIndex = 2;
        // 
        // lblUsernameHint
        // 
        lblUsernameHint.AutoSize = true;
        lblUsernameHint.Font = new Font("Segoe UI", 8.25F);
        lblUsernameHint.ForeColor = Color.IndianRed;
        lblUsernameHint.Location = new Point(45, 130);
        lblUsernameHint.Name = "lblUsernameHint";
        lblUsernameHint.Size = new Size(265, 13);
        lblUsernameHint.TabIndex = 3;
        lblUsernameHint.Text = "Tên người dùng chỉ được chứa các ký tự chữ và số.";
        // 
        // lblPassword
        // 
        lblPassword.AutoSize = true;
        lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPassword.Location = new Point(45, 155);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(121, 19);
        lblPassword.TabIndex = 4;
        lblPassword.Text = "Nhập mật khẩu*:";
        // 
        // txtPassword
        // 
        txtPassword.Font = new Font("Segoe UI", 10F);
        txtPassword.Location = new Point(45, 177);
        txtPassword.Name = "txtPassword";
        txtPassword.PlaceholderText = "Ex: _Hellogame123";
        txtPassword.Size = new Size(370, 25);
        txtPassword.TabIndex = 5;
        txtPassword.UseSystemPasswordChar = true;
        // 
        // lblPasswordHint
        // 
        lblPasswordHint.AutoSize = true;
        lblPasswordHint.Font = new Font("Segoe UI", 8.25F);
        lblPasswordHint.ForeColor = Color.IndianRed;
        lblPasswordHint.Location = new Point(45, 205);
        lblPasswordHint.Name = "lblPasswordHint";
        lblPasswordHint.Size = new Size(245, 13);
        lblPasswordHint.TabIndex = 6;
        lblPasswordHint.Text = "Mật khẩu phải chứa ký tự, số và ký tự đặc biệt.";
        // 
        // lblConfirmPassword
        // 
        lblConfirmPassword.AutoSize = true;
        lblConfirmPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblConfirmPassword.Location = new Point(45, 230);
        lblConfirmPassword.Name = "lblConfirmPassword";
        lblConfirmPassword.Size = new Size(141, 19);
        lblConfirmPassword.TabIndex = 7;
        lblConfirmPassword.Text = "Nhập lại mật khẩu*:";
        // 
        // txtConfirmPassword
        // 
        txtConfirmPassword.Font = new Font("Segoe UI", 10F);
        txtConfirmPassword.Location = new Point(45, 252);
        txtConfirmPassword.Name = "txtConfirmPassword";
        txtConfirmPassword.PlaceholderText = "Ex: _Hellogame123";
        txtConfirmPassword.Size = new Size(370, 25);
        txtConfirmPassword.TabIndex = 8;
        txtConfirmPassword.UseSystemPasswordChar = true;
        // 
        // lblEmail
        // 
        lblEmail.AutoSize = true;
        lblEmail.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblEmail.Location = new Point(45, 290);
        lblEmail.Name = "lblEmail";
        lblEmail.Size = new Size(55, 19);
        lblEmail.TabIndex = 9;
        lblEmail.Text = "Email*:";
        // 
        // txtEmail
        // 
        txtEmail.Font = new Font("Segoe UI", 10F);
        txtEmail.Location = new Point(45, 312);
        txtEmail.Name = "txtEmail";
        txtEmail.PlaceholderText = "Ex: example123@gmail.com";
        txtEmail.Size = new Size(370, 25);
        txtEmail.TabIndex = 10;
        // 
        // btnRegister
        // 
        btnRegister.BackColor = Color.FromArgb(243, 114, 44);
        btnRegister.FlatStyle = FlatStyle.Flat;
        btnRegister.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnRegister.ForeColor = Color.White;
        btnRegister.Location = new Point(140, 375);
        btnRegister.Name = "btnRegister";
        btnRegister.Size = new Size(180, 40);
        btnRegister.TabIndex = 12;
        btnRegister.Text = "Đăng ký";
        btnRegister.UseVisualStyleBackColor = false;
        btnRegister.Click += btnRegister_Click;
        // 
        // lnkBackToLogin
        // 
        lnkBackToLogin.AutoSize = true;
        lnkBackToLogin.Font = new Font("Segoe UI", 9.5F);
        lnkBackToLogin.Location = new Point(145, 430);
        lnkBackToLogin.Name = "lnkBackToLogin";
        lnkBackToLogin.Size = new Size(176, 17);
        lnkBackToLogin.TabIndex = 13;
        lnkBackToLogin.TabStop = true;
        lnkBackToLogin.Text = "Đã có tài khoản? Đăng nhập.";
        lnkBackToLogin.LinkClicked += lnkBackToLogin_LinkClicked;
        // 
        // lblErrorMessage
        // 
        lblErrorMessage.Font = new Font("Segoe UI", 9F);
        lblErrorMessage.ForeColor = Color.Red;
        lblErrorMessage.Location = new Point(45, 345);
        lblErrorMessage.Name = "lblErrorMessage";
        lblErrorMessage.Size = new Size(370, 25);
        lblErrorMessage.TabIndex = 11;
        lblErrorMessage.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // RegisterForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(220, 224, 230);
        ClientSize = new Size(460, 480);
        Controls.Add(lnkBackToLogin);
        Controls.Add(btnRegister);
        Controls.Add(lblErrorMessage);
        Controls.Add(txtEmail);
        Controls.Add(lblEmail);
        Controls.Add(txtConfirmPassword);
        Controls.Add(lblConfirmPassword);
        Controls.Add(lblPasswordHint);
        Controls.Add(txtPassword);
        Controls.Add(lblPassword);
        Controls.Add(lblUsernameHint);
        Controls.Add(txtUsername);
        Controls.Add(lblUsername);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Name = "RegisterForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Đăng ký tài khoản - Cờ Tư Lệnh";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblUsername;
    private TextBox txtUsername;
    private Label lblUsernameHint;
    private Label lblPassword;
    private TextBox txtPassword;
    private Label lblPasswordHint;
    private Label lblConfirmPassword;
    private TextBox txtConfirmPassword;
    private Label lblEmail;
    private TextBox txtEmail;
    private Label lblErrorMessage;
    private Button btnRegister;
    private LinkLabel lnkBackToLogin;
}
