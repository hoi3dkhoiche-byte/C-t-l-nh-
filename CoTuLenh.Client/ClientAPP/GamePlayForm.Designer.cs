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

    // Deployment Panel (Nước đi dàn trận)
    private Panel pnlDeployment;
    private Label lblDeploymentTitle;
    private Label lblDeploymentInfo;
    private Button btnEndDeployment;
    private Button btnUndoDeployment;

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

        pnlDeployment = new Panel();
        lblDeploymentTitle = new Label();
        lblDeploymentInfo = new Label();
        btnEndDeployment = new Button();
        btnUndoDeployment = new Button();

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
        pnlDeployment.SuspendLayout();
        pnlRight.SuspendLayout();
        SuspendLayout();

        // 
        // pnlLeft
        // 
        pnlLeft.Dock = DockStyle.Left;
        pnlLeft.Width = 265;
        pnlLeft.BackColor = Color.FromArgb(235, 240, 245);
        pnlLeft.Padding = new Padding(8);
        pnlLeft.AutoScroll = true;
        pnlLeft.Controls.Add(pnlDeployment);
        pnlLeft.Controls.Add(pnlTurn);
        pnlLeft.Controls.Add(pnlOpponent);
        pnlLeft.Controls.Add(pnlPlayer);

        // pnlOpponent
        pnlOpponent.Dock = DockStyle.Top;
        pnlOpponent.Height = 115;
        pnlOpponent.BackColor = Color.White;
        pnlOpponent.BorderStyle = BorderStyle.FixedSingle;
        pnlOpponent.Controls.Add(lblOpponentName);
        pnlOpponent.Controls.Add(lblOpponentTimer);
        pnlOpponent.Controls.Add(lblOpponentScore);
        pnlOpponent.SendToBack();

        lblOpponentName.Text = "Đối thủ (Xanh)";
        lblOpponentName.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblOpponentName.ForeColor = Color.FromArgb(41, 128, 185);
        lblOpponentName.Location = new Point(8, 8);
        lblOpponentName.AutoSize = true;

        lblOpponentTimer.Text = "00:45";
        lblOpponentTimer.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        lblOpponentTimer.ForeColor = Color.FromArgb(44, 62, 80);
        lblOpponentTimer.Location = new Point(8, 38);
        lblOpponentTimer.AutoSize = true;

        lblOpponentScore.Text = "Lực lượng: 240đ";
        lblOpponentScore.Font = new Font("Segoe UI", 9F);
        lblOpponentScore.ForeColor = Color.Gray;
        lblOpponentScore.Location = new Point(8, 78);
        lblOpponentScore.AutoSize = true;

        // pnlTurn
        pnlTurn.Dock = DockStyle.Top;
        pnlTurn.Height = 45;
        pnlTurn.BackColor = Color.Transparent;
        pnlTurn.Controls.Add(lblTurn);
        pnlTurn.SendToBack();

        lblTurn.Text = "LƯỢT ĐI:\nPHE ĐỎ";
        lblTurn.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
        lblTurn.ForeColor = Color.FromArgb(192, 57, 43);
        lblTurn.TextAlign = ContentAlignment.MiddleCenter;
        lblTurn.Dock = DockStyle.Fill;

        // pnlDeployment (Nước đi dàn trận)
        pnlDeployment.Dock = DockStyle.Top;
        pnlDeployment.Height = 148;
        pnlDeployment.BackColor = Color.White;
        pnlDeployment.BorderStyle = BorderStyle.FixedSingle;
        pnlDeployment.Visible = false;
        pnlDeployment.Controls.Add(btnUndoDeployment);
        pnlDeployment.Controls.Add(btnEndDeployment);
        pnlDeployment.Controls.Add(lblDeploymentInfo);
        pnlDeployment.Controls.Add(lblDeploymentTitle);
        pnlDeployment.SendToBack();

        lblDeploymentTitle.Text = "NƯỚC ĐI DÀN TRẬN";
        lblDeploymentTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblDeploymentTitle.ForeColor = Color.FromArgb(39, 174, 96);
        lblDeploymentTitle.Location = new Point(8, 6);
        lblDeploymentTitle.AutoSize = true;

        lblDeploymentInfo.Text = "Các quân còn lại trong cụm có thể đi:";
        lblDeploymentInfo.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
        lblDeploymentInfo.ForeColor = Color.FromArgb(44, 62, 80);
        lblDeploymentInfo.Location = new Point(8, 28);
        lblDeploymentInfo.Size = new Size(230, 36);

        btnEndDeployment.Text = "Kết thúc dàn trận";
        btnEndDeployment.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnEndDeployment.BackColor = Color.FromArgb(39, 174, 96);
        btnEndDeployment.ForeColor = Color.White;
        btnEndDeployment.FlatStyle = FlatStyle.Flat;
        btnEndDeployment.Location = new Point(8, 68);
        btnEndDeployment.Size = new Size(230, 32);
        btnEndDeployment.Cursor = Cursors.Hand;

        btnUndoDeployment.Text = "Hoàn tác nước dàn trận";
        btnUndoDeployment.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnUndoDeployment.BackColor = Color.FromArgb(230, 126, 34);
        btnUndoDeployment.ForeColor = Color.White;
        btnUndoDeployment.FlatStyle = FlatStyle.Flat;
        btnUndoDeployment.Location = new Point(8, 106);
        btnUndoDeployment.Size = new Size(230, 32);
        btnUndoDeployment.Cursor = Cursors.Hand;

        // pnlPlayer
        pnlPlayer.Dock = DockStyle.Bottom;
        pnlPlayer.Height = 115;
        pnlPlayer.BackColor = Color.White;
        pnlPlayer.BorderStyle = BorderStyle.FixedSingle;
        pnlPlayer.Controls.Add(lblPlayerName);
        pnlPlayer.Controls.Add(lblPlayerTimer);
        pnlPlayer.Controls.Add(lblPlayerScore);

        lblPlayerName.Text = "Tôi (Đỏ)";
        lblPlayerName.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblPlayerName.ForeColor = Color.FromArgb(192, 57, 43);
        lblPlayerName.Location = new Point(8, 8);
        lblPlayerName.AutoSize = true;

        lblPlayerTimer.Text = "00:45";
        lblPlayerTimer.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        lblPlayerTimer.ForeColor = Color.FromArgb(44, 62, 80);
        lblPlayerTimer.Location = new Point(8, 38);
        lblPlayerTimer.AutoSize = true;

        lblPlayerScore.Text = "Lực lượng: 240đ";
        lblPlayerScore.Font = new Font("Segoe UI", 9F);
        lblPlayerScore.ForeColor = Color.Gray;
        lblPlayerScore.Location = new Point(8, 78);
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
        ClientSize = new Size(1180, 750);
        Controls.Add(board);
        Controls.Add(pnlRight);
        Controls.Add(pnlLeft);
        MinimumSize = new Size(1050, 700);
        Text = "Cờ Tư Lệnh Online - Bàn Cờ Thi Đấu";

        pnlLeft.ResumeLayout(false);
        pnlOpponent.ResumeLayout(false);
        pnlOpponent.PerformLayout();
        pnlPlayer.ResumeLayout(false);
        pnlPlayer.PerformLayout();
        pnlTurn.ResumeLayout(false);
        pnlDeployment.ResumeLayout(false);
        pnlDeployment.PerformLayout();
        pnlRight.ResumeLayout(false);
        pnlRight.PerformLayout();
        ResumeLayout(false);
    }
}
