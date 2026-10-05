namespace ClientAPP;

partial class LobbyForm
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

    private Panel pnlHeader;
    private Label lblTitle;
    private Label lblUserInfo;
    private Label lblCoin;
    private Button btnDeposit;
    private Button btnProfile;
    private Button btnLeaderboard;
    private Button btnHistory;
    private Button btnLogout;

    private Panel pnlActionBar;
    private Button btnCreateRoom;
    private Button btnRefresh;
    private TextBox txtRoomCode;
    private Button btnJoinByCode;
    private FlowLayoutPanel flowRooms;
    private Label lblSectionTitle;

    private void InitializeComponent()
    {
        pnlHeader = new Panel();
        lblTitle = new Label();
        lblUserInfo = new Label();
        lblCoin = new Label();
        btnDeposit = new Button();
        btnProfile = new Button();
        btnLeaderboard = new Button();
        btnHistory = new Button();
        btnLogout = new Button();

        pnlActionBar = new Panel();
        btnCreateRoom = new Button();
        btnRefresh = new Button();
        txtRoomCode = new TextBox();
        btnJoinByCode = new Button();
        lblSectionTitle = new Label();
        flowRooms = new FlowLayoutPanel();

        pnlHeader.SuspendLayout();
        pnlActionBar.SuspendLayout();
        SuspendLayout();

        // 
        // pnlHeader
        // 
        pnlHeader.BackColor = Color.FromArgb(31, 58, 95); // Military Navy
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 70;
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblUserInfo);
        pnlHeader.Controls.Add(lblCoin);
        pnlHeader.Controls.Add(btnDeposit);
        pnlHeader.Controls.Add(btnProfile);
        pnlHeader.Controls.Add(btnLeaderboard);
        pnlHeader.Controls.Add(btnHistory);
        pnlHeader.Controls.Add(btnLogout);

        // lblTitle
        lblTitle.Text = "★ CỜ TƯ LỆNH ONLINE ★";
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(241, 196, 15);
        lblTitle.Location = new Point(20, 18);
        lblTitle.AutoSize = true;

        // lblUserInfo
        lblUserInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUserInfo.ForeColor = Color.White;
        lblUserInfo.Location = new Point(320, 24);
        lblUserInfo.AutoSize = true;

        // lblCoin
        lblCoin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblCoin.ForeColor = Color.FromArgb(241, 196, 15);
        lblCoin.Location = new Point(480, 24);
        lblCoin.AutoSize = true;

        // btnDeposit
        btnDeposit.Text = "+ Nạp Xu";
        btnDeposit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnDeposit.BackColor = Color.FromArgb(201, 162, 39);
        btnDeposit.ForeColor = Color.White;
        btnDeposit.FlatStyle = FlatStyle.Flat;
        btnDeposit.FlatAppearance.BorderSize = 0;
        btnDeposit.Size = new Size(80, 30);
        btnDeposit.Location = new Point(620, 20);
        btnDeposit.Cursor = Cursors.Hand;
        btnDeposit.Click += btnDeposit_Click;

        // btnProfile
        btnProfile.Text = "Hồ sơ";
        btnProfile.Font = new Font("Segoe UI", 9F);
        btnProfile.BackColor = Color.FromArgb(41, 78, 125);
        btnProfile.ForeColor = Color.White;
        btnProfile.FlatStyle = FlatStyle.Flat;
        btnProfile.FlatAppearance.BorderSize = 0;
        btnProfile.Size = new Size(75, 30);
        btnProfile.Location = new Point(715, 20);
        btnProfile.Cursor = Cursors.Hand;
        btnProfile.Click += btnProfile_Click;

        // btnLeaderboard
        btnLeaderboard.Text = "Xếp hạng";
        btnLeaderboard.Font = new Font("Segoe UI", 9F);
        btnLeaderboard.BackColor = Color.FromArgb(41, 78, 125);
        btnLeaderboard.ForeColor = Color.White;
        btnLeaderboard.FlatStyle = FlatStyle.Flat;
        btnLeaderboard.FlatAppearance.BorderSize = 0;
        btnLeaderboard.Size = new Size(85, 30);
        btnLeaderboard.Location = new Point(800, 20);
        btnLeaderboard.Cursor = Cursors.Hand;
        btnLeaderboard.Click += btnLeaderboard_Click;

        // btnHistory
        btnHistory.Text = "Lịch sử";
        btnHistory.Font = new Font("Segoe UI", 9F);
        btnHistory.BackColor = Color.FromArgb(41, 78, 125);
        btnHistory.ForeColor = Color.White;
        btnHistory.FlatStyle = FlatStyle.Flat;
        btnHistory.FlatAppearance.BorderSize = 0;
        btnHistory.Size = new Size(75, 30);
        btnHistory.Location = new Point(895, 20);
        btnHistory.Cursor = Cursors.Hand;
        btnHistory.Click += btnHistory_Click;

        // btnLogout
        btnLogout.Text = "Đăng xuất";
        btnLogout.Font = new Font("Segoe UI", 9F);
        btnLogout.BackColor = Color.FromArgb(192, 57, 43);
        btnLogout.ForeColor = Color.White;
        btnLogout.FlatStyle = FlatStyle.Flat;
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Size = new Size(85, 30);
        btnLogout.Location = new Point(980, 20);
        btnLogout.Cursor = Cursors.Hand;
        btnLogout.Click += btnLogout_Click;

        // 
        // pnlActionBar
        // 
        pnlActionBar.Dock = DockStyle.Top;
        pnlActionBar.Height = 65;
        pnlActionBar.BackColor = Color.White;
        pnlActionBar.Padding = new Padding(20, 15, 20, 15);
        pnlActionBar.Controls.Add(lblSectionTitle);
        pnlActionBar.Controls.Add(btnCreateRoom);
        pnlActionBar.Controls.Add(btnRefresh);
        pnlActionBar.Controls.Add(btnJoinByCode);
        pnlActionBar.Controls.Add(txtRoomCode);

        // lblSectionTitle
        lblSectionTitle.Text = "Danh Sách Phòng Chờ";
        lblSectionTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblSectionTitle.ForeColor = Color.FromArgb(31, 58, 95);
        lblSectionTitle.Location = new Point(20, 20);
        lblSectionTitle.AutoSize = true;

        // btnCreateRoom
        btnCreateRoom.Text = "＋ Tạo phòng mới";
        btnCreateRoom.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnCreateRoom.BackColor = Color.FromArgb(39, 174, 96);
        btnCreateRoom.ForeColor = Color.White;
        btnCreateRoom.FlatStyle = FlatStyle.Flat;
        btnCreateRoom.FlatAppearance.BorderSize = 0;
        btnCreateRoom.Size = new Size(150, 36);
        btnCreateRoom.Location = new Point(560, 15);
        btnCreateRoom.Cursor = Cursors.Hand;
        btnCreateRoom.Click += btnCreateRoom_Click;

        // txtRoomCode
        txtRoomCode.PlaceholderText = "Nhập mã phòng...";
        txtRoomCode.Size = new Size(140, 29);
        txtRoomCode.Location = new Point(730, 18);

        // btnJoinByCode
        btnJoinByCode.Text = "Vào";
        btnJoinByCode.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnJoinByCode.BackColor = Color.FromArgb(31, 58, 95);
        btnJoinByCode.ForeColor = Color.White;
        btnJoinByCode.FlatStyle = FlatStyle.Flat;
        btnJoinByCode.FlatAppearance.BorderSize = 0;
        btnJoinByCode.Size = new Size(60, 31);
        btnJoinByCode.Location = new Point(880, 17);
        btnJoinByCode.Cursor = Cursors.Hand;
        btnJoinByCode.Click += btnJoinByCode_Click;

        // btnRefresh
        btnRefresh.Text = "↻ Làm mới";
        btnRefresh.Font = new Font("Segoe UI", 9.5F);
        btnRefresh.BackColor = Color.FromArgb(235, 240, 245);
        btnRefresh.ForeColor = Color.FromArgb(31, 58, 95);
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.FlatAppearance.BorderSize = 1;
        btnRefresh.FlatAppearance.BorderColor = Color.LightGray;
        btnRefresh.Size = new Size(100, 31);
        btnRefresh.Location = new Point(960, 17);
        btnRefresh.Cursor = Cursors.Hand;
        btnRefresh.Click += btnRefresh_Click;

        // 
        // flowRooms
        // 
        flowRooms.Dock = DockStyle.Fill;
        flowRooms.AutoScroll = true;
        flowRooms.BackColor = Color.FromArgb(244, 246, 249);
        flowRooms.Padding = new Padding(15);

        // 
        // LobbyForm
        // 
        ClientSize = new Size(1085, 680);
        Controls.Add(flowRooms);
        Controls.Add(pnlActionBar);
        Controls.Add(pnlHeader);
        MinimumSize = new Size(950, 600);
        Text = "Cờ Tư Lệnh Online - Sảnh Chờ (Lobby)";

        pnlHeader.ResumeLayout(false);
        pnlHeader.PerformLayout();
        pnlActionBar.ResumeLayout(false);
        pnlActionBar.PerformLayout();
        ResumeLayout(false);
    }
}
