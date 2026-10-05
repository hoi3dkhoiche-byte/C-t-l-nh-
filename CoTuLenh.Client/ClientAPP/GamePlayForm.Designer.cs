namespace ClientAPP;

partial class GamePlayForm
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

    private BoardControl board;
    private Panel pnlLeft;
    private Panel pnlRight;

    // Opponent Panel
    private Panel pnlOpponent;
    private Label lblOpponentName;
    private Label lblOpponentTimer;
    private Label lblOpponentScore;

    // Player Panel
    private Panel pnlPlayer;
    private Label lblPlayerName;
    private Label lblPlayerTimer;
    private Label lblPlayerScore;

    // Turn indicator
    private Panel pnlTurn;
    private Label lblTurn;

    // Right Controls
    private Label lblLogTitle;
    private ListBox lstMoveLog;
    private Label lblChatTitle;
    private ListBox lstChat;
    private TextBox txtChat;
    private Button btnSend;
    private Button btnDraw;
    private Button btnSurrender;
    private Button btnLeave;

    private System.Windows.Forms.Timer gameTimer;

    private void InitializeComponent()
    {
        board = new BoardControl();
        pnlLeft = new Panel();
        pnlRight = new Panel();

        pnlOpponent = new Panel();
        lblOpponentName = new Label();
        lblOpponentTimer = new Label();
        lblOpponentScore = new Label();

        pnlPlayer = new Panel();
        lblPlayerName = new Label();
        lblPlayerTimer = new Label();
        lblPlayerScore = new Label();

        pnlTurn = new Panel();
        lblTurn = new Label();

        lblLogTitle = new Label();
        lstMoveLog = new ListBox();
        lblChatTitle = new Label();
        lstChat = new ListBox();
        txtChat = new TextBox();
        btnSend = new Button();

        btnDraw = new Button();
        btnSurrender = new Button();
        btnLeave = new Button();

        gameTimer = new System.Windows.Forms.Timer();

        pnlLeft.SuspendLayout();
        pnlOpponent.SuspendLayout();
        pnlPlayer.SuspendLayout();
        pnlTurn.SuspendLayout();
        pnlRight.SuspendLayout();
        SuspendLayout();

        // 
        // pnlLeft
        // 
        pnlLeft.Dock = DockStyle.Left;
        pnlLeft.Width = 240;
        pnlLeft.BackColor = Color.FromArgb(235, 240, 245);
        pnlLeft.Padding = new Padding(12);
        pnlLeft.Controls.Add(pnlOpponent);
        pnlLeft.Controls.Add(pnlTurn);
        pnlLeft.Controls.Add(pnlPlayer);

        // pnlOpponent
        pnlOpponent.Dock = DockStyle.Top;
        pnlOpponent.Height = 160;
        pnlOpponent.BackColor = Color.White;
        pnlOpponent.BorderStyle = BorderStyle.FixedSingle;
        pnlOpponent.Controls.Add(lblOpponentName);
        pnlOpponent.Controls.Add(lblOpponentTimer);
        pnlOpponent.Controls.Add(lblOpponentScore);

        lblOpponentName.Text = "🔵 Đối thủ (Xanh)";
        lblOpponentName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblOpponentName.ForeColor = Color.FromArgb(41, 128, 185);
        lblOpponentName.Location = new Point(10, 15);
        lblOpponentName.AutoSize = true;

        lblOpponentTimer.Text = "⏱ 00:45";
        lblOpponentTimer.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblOpponentTimer.ForeColor = Color.FromArgb(44, 62, 80);
        lblOpponentTimer.Location = new Point(10, 55);
        lblOpponentTimer.AutoSize = true;

        lblOpponentScore.Text = "Lực lượng: 240đ";
        lblOpponentScore.Font = new Font("Segoe UI", 9.5F);
        lblOpponentScore.ForeColor = Color.Gray;
        lblOpponentScore.Location = new Point(10, 110);
        lblOpponentScore.AutoSize = true;

        // pnlTurn
        pnlTurn.Dock = DockStyle.Fill;
        pnlTurn.BackColor = Color.Transparent;
        pnlTurn.Controls.Add(lblTurn);

        lblTurn.Text = "LƯỢT ĐI:\nPHE ĐỎ";
        lblTurn.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTurn.ForeColor = Color.FromArgb(192, 57, 43);
        lblTurn.TextAlign = ContentAlignment.MiddleCenter;
        lblTurn.Dock = DockStyle.Fill;

        // pnlPlayer
        pnlPlayer.Dock = DockStyle.Bottom;
        pnlPlayer.Height = 160;
        pnlPlayer.BackColor = Color.White;
        pnlPlayer.BorderStyle = BorderStyle.FixedSingle;
        pnlPlayer.Controls.Add(lblPlayerName);
        pnlPlayer.Controls.Add(lblPlayerTimer);
        pnlPlayer.Controls.Add(lblPlayerScore);

        lblPlayerName.Text = "🔴 Tôi (Đỏ)";
        lblPlayerName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblPlayerName.ForeColor = Color.FromArgb(192, 57, 43);
        lblPlayerName.Location = new Point(10, 15);
        lblPlayerName.AutoSize = true;

        lblPlayerTimer.Text = "⏱ 00:45";
        lblPlayerTimer.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblPlayerTimer.ForeColor = Color.FromArgb(44, 62, 80);
        lblPlayerTimer.Location = new Point(10, 55);
        lblPlayerTimer.AutoSize = true;

        lblPlayerScore.Text = "Lực lượng: 240đ";
        lblPlayerScore.Font = new Font("Segoe UI", 9.5F);
        lblPlayerScore.ForeColor = Color.Gray;
        lblPlayerScore.Location = new Point(10, 110);
        lblPlayerScore.AutoSize = true;

        // 
        // board
        // 
        board.Dock = DockStyle.Fill;

        // 
        // pnlRight
        // 
        pnlRight.Dock = DockStyle.Right;
        pnlRight.Width = 260;
        pnlRight.BackColor = Color.FromArgb(235, 240, 245);
        pnlRight.Padding = new Padding(10);
        pnlRight.Controls.Add(btnLeave);
        pnlRight.Controls.Add(btnSurrender);
        pnlRight.Controls.Add(btnDraw);
        pnlRight.Controls.Add(btnSend);
        pnlRight.Controls.Add(txtChat);
        pnlRight.Controls.Add(lstChat);
        pnlRight.Controls.Add(lblChatTitle);
        pnlRight.Controls.Add(lstMoveLog);
        pnlRight.Controls.Add(lblLogTitle);

        // Biên bản
        lblLogTitle.Text = "Biên bản nước đi:";
        lblLogTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblLogTitle.Location = new Point(10, 10);
        lblLogTitle.AutoSize = true;

        lstMoveLog.Location = new Point(10, 32);
        lstMoveLog.Size = new Size(240, 220);
        lstMoveLog.Font = new Font("Segoe UI", 9F);
        lstMoveLog.BorderStyle = BorderStyle.FixedSingle;

        // Chat
        lblChatTitle.Text = "Trò chuyện:";
        lblChatTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblChatTitle.Location = new Point(10, 260);
        lblChatTitle.AutoSize = true;

        lstChat.Location = new Point(10, 282);
        lstChat.Size = new Size(240, 180);
        lstChat.Font = new Font("Segoe UI", 9F);
        lstChat.BorderStyle = BorderStyle.FixedSingle;

        txtChat.Location = new Point(10, 470);
        txtChat.Size = new Size(175, 25);
        txtChat.PlaceholderText = "Tin nhắn...";

        btnSend.Text = "Gửi";
        btnSend.Location = new Point(190, 468);
        btnSend.Size = new Size(60, 29);
        btnSend.BackColor = Color.FromArgb(31, 58, 95);
        btnSend.ForeColor = Color.White;
        btnSend.FlatStyle = FlatStyle.Flat;
        btnSend.Click += btnSend_Click;

        // Action buttons
        btnDraw.Text = "Xin hoà";
        btnDraw.Location = new Point(10, 515);
        btnDraw.Size = new Size(115, 36);
        btnDraw.BackColor = Color.FromArgb(241, 196, 15);
        btnDraw.FlatStyle = FlatStyle.Flat;
        btnDraw.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnDraw.Click += btnDraw_Click;

        btnSurrender.Text = "Đầu hàng";
        btnSurrender.Location = new Point(135, 515);
        btnSurrender.Size = new Size(115, 36);
        btnSurrender.BackColor = Color.FromArgb(231, 76, 60);
        btnSurrender.ForeColor = Color.White;
        btnSurrender.FlatStyle = FlatStyle.Flat;
        btnSurrender.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSurrender.Click += btnSurrender_Click;

        btnLeave.Text = "Thoát ván đấu";
        btnLeave.Location = new Point(10, 560);
        btnLeave.Size = new Size(240, 36);
        btnLeave.BackColor = Color.FromArgb(149, 165, 166);
        btnLeave.ForeColor = Color.White;
        btnLeave.FlatStyle = FlatStyle.Flat;
        btnLeave.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnLeave.Click += btnLeave_Click;

        // 
        // GamePlayForm
        // 
        ClientSize = new Size(1140, 720);
        Controls.Add(board);
        Controls.Add(pnlRight);
        Controls.Add(pnlLeft);
        MinimumSize = new Size(1000, 680);
        Text = "Cờ Tư Lệnh Online - Bàn Cờ Thi Đấu";

        pnlLeft.ResumeLayout(false);
        pnlOpponent.ResumeLayout(false);
        pnlOpponent.PerformLayout();
        pnlPlayer.ResumeLayout(false);
        pnlPlayer.PerformLayout();
        pnlTurn.ResumeLayout(false);
        pnlRight.ResumeLayout(false);
        pnlRight.PerformLayout();
        ResumeLayout(false);
    }
}
