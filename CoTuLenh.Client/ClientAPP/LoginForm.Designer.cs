namespace ClientAPP;

partial class LoginForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new Label();
        txtUsername = new TextBox();
        label2 = new Label();
        txtPassword = new TextBox();
        chkRememberMe = new CheckBox();
        btnLogin = new Button();
        btnRegister = new Button();
        lnkForgotPassword = new LinkLabel();
        lblErrorMessage = new Label();
        lblTitle = new Label();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(289, 178);
        label1.Name = "label1";
        label1.Size = new Size(129, 15);
        label1.TabIndex = 0;
        label1.Text = "Tên đăng nhập - email:";
        label1.Click += label1_Click;
        // 
        // txtUsername
        // 
        txtUsername.Location = new Point(435, 175);
        txtUsername.Name = "txtUsername";
        txtUsername.Size = new Size(184, 23);
        txtUsername.TabIndex = 1;
        txtUsername.TextChanged += txtUsername_TextChanged;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(315, 215);
        label2.Name = "label2";
        label2.Size = new Size(61, 15);
        label2.TabIndex = 2;
        label2.Text = "Mật Khẩu:";
        label2.Click += label2_Click;
        // 
        // txtPassword
        // 
        txtPassword.Location = new Point(435, 215);
        txtPassword.Name = "txtPassword";
        txtPassword.Size = new Size(184, 23);
        txtPassword.TabIndex = 3;
        txtPassword.UseSystemPasswordChar = true;
        txtPassword.TextChanged += txtPassword_TextChanged;
        // 
        // chkRememberMe
        // 
        chkRememberMe.AutoSize = true;
        chkRememberMe.Location = new Point(418, 262);
        chkRememberMe.Name = "chkRememberMe";
        chkRememberMe.Size = new Size(128, 19);
        chkRememberMe.TabIndex = 4;
        chkRememberMe.Text = "Ghi nhớ đăng nhập";
        chkRememberMe.UseVisualStyleBackColor = true;
        chkRememberMe.CheckedChanged += chkRememberMe_CheckedChanged;
        // 
        // btnLogin
        // 
        btnLogin.BackColor = Color.FromArgb(243, 114, 44);
        btnLogin.FlatStyle = FlatStyle.Flat;
        btnLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(415, 304);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(110, 36);
        btnLogin.TabIndex = 5;
        btnLogin.Text = "Đăng nhập";
        btnLogin.UseVisualStyleBackColor = false;
        btnLogin.Click += btnLogin_Click;
        // 
        // btnRegister
        // 
        btnRegister.BackColor = Color.FromArgb(46, 204, 113);
        btnRegister.FlatStyle = FlatStyle.Flat;
        btnRegister.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnRegister.ForeColor = Color.White;
        btnRegister.Location = new Point(540, 304);
        btnRegister.Name = "btnRegister";
        btnRegister.Size = new Size(110, 36);
        btnRegister.TabIndex = 6;
        btnRegister.Text = "Đăng ký";
        btnRegister.UseVisualStyleBackColor = false;
        btnRegister.Click += btnRegister_Click;
        // 
        // lnkForgotPassword
        // 
        lnkForgotPassword.AutoSize = true;
        lnkForgotPassword.Location = new Point(570, 266);
        lnkForgotPassword.Name = "lnkForgotPassword";
        lnkForgotPassword.Size = new Size(94, 15);
        lnkForgotPassword.TabIndex = 6;
        lnkForgotPassword.TabStop = true;
        lnkForgotPassword.Text = "Quên mật khẩu?";
        lnkForgotPassword.LinkClicked += lnkForgotPassword_LinkClicked;
        // 
        // lblErrorMessage
        // 
        lblErrorMessage.AutoSize = true;
        lblErrorMessage.ForeColor = Color.Red;
        lblErrorMessage.Location = new Point(466, 343);
        lblErrorMessage.Name = "lblErrorMessage";
        lblErrorMessage.Size = new Size(0, 15);
        lblErrorMessage.TabIndex = 7;
        // 
        // lblTitle
        // 
        lblTitle.BackColor = Color.FromArgb(74, 144, 226);
        lblTitle.Dock = DockStyle.Top;
        lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(1021, 60);
        lblTitle.TabIndex = 8;
        lblTitle.Text = "CHÀO MỪNG BẠN ĐẾN VỚI CỜ TƯ LỆNH";
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblTitle.Click += lblTitle_Click;
        // 
        // LoginForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1021, 483);
        Controls.Add(lblTitle);
        Controls.Add(lblErrorMessage);
        Controls.Add(lnkForgotPassword);
        Controls.Add(btnRegister);
        Controls.Add(btnLogin);
        Controls.Add(chkRememberMe);
        Controls.Add(txtPassword);
        Controls.Add(label2);
        Controls.Add(txtUsername);
        Controls.Add(label1);
        Name = "LoginForm";
        Text = "Form1";
        Load += Form1_Load;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private TextBox txtUsername;
    private Label label2;
    private TextBox txtPassword;
    private CheckBox chkRememberMe;
    private Button btnLogin;
    private Button btnRegister;
    private LinkLabel lnkForgotPassword;
    private Label lblErrorMessage;
    private Label lblTitle;
}
