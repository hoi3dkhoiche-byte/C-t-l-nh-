namespace ClientAPP;

partial class ProfileForm
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

    private PictureBox picAvatar;
    private Button btnChangeAvatar;

    private Label lblUsername;
    private Label lblEmail;
    private Label lblElo;
    private Label lblCoins;

    private GroupBox grpStats;
    private Label lblTotalMatches;
    private Label lblWins;
    private Label lblLosses;
    private Label lblWinRate;

    private GroupBox grpChangePassword;
    private TextBox txtOldPassword;
    private TextBox txtNewPassword;
    private TextBox txtConfirmPassword;
    private Button btnUpdatePassword;

    private Button btnClose;

    private void InitializeComponent()
    {
        picAvatar = new PictureBox();
        btnChangeAvatar = new Button();

        lblUsername = new Label();
        lblEmail = new Label();
        lblElo = new Label();
        lblCoins = new Label();

        grpStats = new GroupBox();
        lblTotalMatches = new Label();
        lblWins = new Label();
        lblLosses = new Label();
        lblWinRate = new Label();

        grpChangePassword = new GroupBox();
        txtOldPassword = new TextBox();
        txtNewPassword = new TextBox();
        txtConfirmPassword = new TextBox();
        btnUpdatePassword = new Button();

        btnClose = new Button();

        grpStats.SuspendLayout();
        grpChangePassword.SuspendLayout();
        SuspendLayout();

        // picAvatar
        picAvatar.Location = new Point(30, 30);
        picAvatar.Size = new Size(120, 120);
        picAvatar.BorderStyle = BorderStyle.FixedSingle;
        picAvatar.BackColor = Color.FromArgb(220, 230, 242);

        // btnChangeAvatar
        btnChangeAvatar.Text = "Đổi Avatar";
        btnChangeAvatar.Location = new Point(30, 160);
        btnChangeAvatar.Size = new Size(120, 32);
        btnChangeAvatar.FlatStyle = FlatStyle.Flat;
        btnChangeAvatar.Click += btnChangeAvatar_Click;

        // User info
        lblUsername.Text = "Tài khoản: DaiTuLenh";
        lblUsername.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblUsername.ForeColor = Color.FromArgb(31, 58, 95);
        lblUsername.Location = new Point(175, 30);
        lblUsername.AutoSize = true;

        lblEmail.Text = "Email: user@example.com";
        lblEmail.Font = new Font("Segoe UI", 10F);
        lblEmail.Location = new Point(175, 65);
        lblEmail.AutoSize = true;

        lblElo.Text = "Điểm ELO: 1250";
        lblElo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblElo.ForeColor = Color.FromArgb(192, 57, 43);
        lblElo.Location = new Point(175, 95);
        lblElo.AutoSize = true;

        lblCoins.Text = "Số dư: 50,000 Xu";
        lblCoins.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblCoins.ForeColor = Color.FromArgb(201, 162, 39);
        lblCoins.Location = new Point(175, 125);
        lblCoins.AutoSize = true;

        // grpStats
        grpStats.Text = "Thành tích thi đấu";
        grpStats.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grpStats.Location = new Point(30, 210);
        grpStats.Size = new Size(580, 85);
        grpStats.Controls.Add(lblTotalMatches);
        grpStats.Controls.Add(lblWins);
        grpStats.Controls.Add(lblLosses);
        grpStats.Controls.Add(lblWinRate);

        lblTotalMatches.Text = "Tổng trận: 25";
        lblTotalMatches.Font = new Font("Segoe UI", 10F);
        lblTotalMatches.Location = new Point(20, 35);
        lblTotalMatches.AutoSize = true;

        lblWins.Text = "Thắng: 18";
        lblWins.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblWins.ForeColor = Color.FromArgb(39, 174, 96);
        lblWins.Location = new Point(160, 35);
        lblWins.AutoSize = true;

        lblLosses.Text = "Thua: 7";
        lblLosses.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblLosses.ForeColor = Color.FromArgb(192, 57, 43);
        lblLosses.Location = new Point(280, 35);
        lblLosses.AutoSize = true;

        lblWinRate.Text = "Tỉ lệ thắng: 72.0%";
        lblWinRate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblWinRate.ForeColor = Color.FromArgb(31, 58, 95);
        lblWinRate.Location = new Point(400, 35);
        lblWinRate.AutoSize = true;

        // grpChangePassword
        grpChangePassword.Text = "Đổi mật khẩu";
        grpChangePassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grpChangePassword.Location = new Point(30, 310);
        grpChangePassword.Size = new Size(580, 165);
        grpChangePassword.Controls.Add(txtOldPassword);
        grpChangePassword.Controls.Add(txtNewPassword);
        grpChangePassword.Controls.Add(txtConfirmPassword);
        grpChangePassword.Controls.Add(btnUpdatePassword);

        txtOldPassword.PlaceholderText = "Mật khẩu hiện tại";
        txtOldPassword.UseSystemPasswordChar = true;
        txtOldPassword.Location = new Point(20, 30);
        txtOldPassword.Size = new Size(320, 27);

        txtNewPassword.PlaceholderText = "Mật khẩu mới";
        txtNewPassword.UseSystemPasswordChar = true;
        txtNewPassword.Location = new Point(20, 68);
        txtNewPassword.Size = new Size(320, 27);

        txtConfirmPassword.PlaceholderText = "Xác nhận mật khẩu mới";
        txtConfirmPassword.UseSystemPasswordChar = true;
        txtConfirmPassword.Location = new Point(20, 106);
        txtConfirmPassword.Size = new Size(320, 27);

        btnUpdatePassword.Text = "Cập nhật";
        btnUpdatePassword.BackColor = Color.FromArgb(31, 58, 95);
        btnUpdatePassword.ForeColor = Color.White;
        btnUpdatePassword.FlatStyle = FlatStyle.Flat;
        btnUpdatePassword.Location = new Point(370, 65);
        btnUpdatePassword.Size = new Size(180, 38);
        btnUpdatePassword.Click += btnUpdatePassword_Click;

        // btnClose
        btnClose.Text = "Đóng";
        btnClose.Location = new Point(480, 490);
        btnClose.Size = new Size(130, 36);
        btnClose.Click += (s, e) => this.Close();

        // ProfileForm
        ClientSize = new Size(640, 545);
        Controls.Add(btnClose);
        Controls.Add(grpChangePassword);
        Controls.Add(grpStats);
        Controls.Add(lblCoins);
        Controls.Add(lblElo);
        Controls.Add(lblEmail);
        Controls.Add(lblUsername);
        Controls.Add(btnChangeAvatar);
        Controls.Add(picAvatar);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Hồ Sơ Cá Nhân - Cờ Tư Lệnh Online";

        grpStats.ResumeLayout(false);
        grpStats.PerformLayout();
        grpChangePassword.ResumeLayout(false);
        grpChangePassword.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
