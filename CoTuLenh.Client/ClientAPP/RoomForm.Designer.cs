namespace ClientAPP;

partial class RoomForm
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
    private Label lblRoomTitle;
    private Label lblBetInfo;

    private Panel pnlHost;
    private Label lblHostTitle;
    private Label lblHostName;
    private Label lblHostStatus;

    private Panel pnlGuest;
    private Label lblGuestTitle;
    private Label lblGuestName;
    private Label lblGuestStatus;

    private Panel pnlChat;
    private ListBox lstChat;
    private TextBox txtChatInput;
    private Button btnSendChat;

    private Panel pnlBottom;
    private Button btnLeave;
    private Button btnAction; // "Sẵn sàng" hoặc "Bắt đầu"

    private void InitializeComponent()
    {
        pnlHeader = new Panel();
        lblRoomTitle = new Label();
        lblBetInfo = new Label();

        pnlHost = new Panel();
        lblHostTitle = new Label();
        lblHostName = new Label();
        lblHostStatus = new Label();

        pnlGuest = new Panel();
        lblGuestTitle = new Label();
        lblGuestName = new Label();
        lblGuestStatus = new Label();

        pnlChat = new Panel();
        lstChat = new ListBox();
        txtChatInput = new TextBox();
        btnSendChat = new Button();

        pnlBottom = new Panel();
        btnLeave = new Button();
        btnAction = new Button();

        pnlHeader.SuspendLayout();
        pnlHost.SuspendLayout();
        pnlGuest.SuspendLayout();
        pnlChat.SuspendLayout();
        pnlBottom.SuspendLayout();
        SuspendLayout();

        // 
        // pnlHeader
        // 
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 70;
        pnlHeader.BackColor = Color.FromArgb(31, 58, 95);
        pnlHeader.Controls.Add(lblRoomTitle);
        pnlHeader.Controls.Add(lblBetInfo);

        lblRoomTitle.Text = "PHÒNG CHỜ: #ROOM";
        lblRoomTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblRoomTitle.ForeColor = Color.FromArgb(241, 196, 15);
        lblRoomTitle.Location = new Point(25, 20);
        lblRoomTitle.AutoSize = true;

        lblBetInfo.Text = "Mức cược: 1,000 Xu";
        lblBetInfo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblBetInfo.ForeColor = Color.White;
        lblBetInfo.Location = new Point(600, 24);
        lblBetInfo.AutoSize = true;

        // 
        // pnlHost
        // 
        pnlHost.BackColor = Color.White;
        pnlHost.BorderStyle = BorderStyle.FixedSingle;
        pnlHost.Location = new Point(30, 95);
        pnlHost.Size = new Size(260, 240);
        pnlHost.Controls.Add(lblHostTitle);
        pnlHost.Controls.Add(lblHostName);
        pnlHost.Controls.Add(lblHostStatus);

        lblHostTitle.Text = "★ CHỦ PHÒNG (ĐỎ) ★";
        lblHostTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblHostTitle.ForeColor = Color.FromArgb(192, 57, 43);
        lblHostTitle.Location = new Point(20, 20);
        lblHostTitle.AutoSize = true;

        lblHostName.Text = "Người chơi 1";
        lblHostName.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblHostName.ForeColor = Color.FromArgb(44, 62, 80);
        lblHostName.Location = new Point(20, 80);
        lblHostName.AutoSize = true;

        lblHostStatus.Text = "Trạng thái: SẴN SÀNG";
        lblHostStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblHostStatus.ForeColor = Color.FromArgb(39, 174, 96);
        lblHostStatus.Location = new Point(20, 160);
        lblHostStatus.AutoSize = true;

        // 
        // pnlGuest
        // 
        pnlGuest.BackColor = Color.White;
        pnlGuest.BorderStyle = BorderStyle.FixedSingle;
        pnlGuest.Location = new Point(320, 95);
        pnlGuest.Size = new Size(260, 240);
        pnlGuest.Controls.Add(lblGuestTitle);
        pnlGuest.Controls.Add(lblGuestName);
        pnlGuest.Controls.Add(lblGuestStatus);

        lblGuestTitle.Text = "⚔ ĐỐI THỦ (XANH) ⚔";
        lblGuestTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblGuestTitle.ForeColor = Color.FromArgb(41, 128, 185);
        lblGuestTitle.Location = new Point(20, 20);
        lblGuestTitle.AutoSize = true;

        lblGuestName.Text = "Đang chờ đối thủ...";
        lblGuestName.Font = new Font("Segoe UI", 13F, FontStyle.Italic);
        lblGuestName.ForeColor = Color.Gray;
        lblGuestName.Location = new Point(20, 80);
        lblGuestName.AutoSize = true;

        lblGuestStatus.Text = "Trạng thái: Chưa sẵn sàng";
        lblGuestStatus.Font = new Font("Segoe UI", 10F);
        lblGuestStatus.ForeColor = Color.OrangeRed;
        lblGuestStatus.Location = new Point(20, 160);
        lblGuestStatus.AutoSize = true;

        // 
        // pnlChat
        // 
        pnlChat.BackColor = Color.White;
        pnlChat.BorderStyle = BorderStyle.FixedSingle;
        pnlChat.Location = new Point(610, 95);
        pnlChat.Size = new Size(265, 360);
        pnlChat.Controls.Add(lstChat);
        pnlChat.Controls.Add(txtChatInput);
        pnlChat.Controls.Add(btnSendChat);

        lstChat.Dock = DockStyle.Top;
        lstChat.Height = 310;
        lstChat.BorderStyle = BorderStyle.None;
        lstChat.Font = new Font("Segoe UI", 9.5F);

        txtChatInput.Location = new Point(5, 322);
        txtChatInput.Size = new Size(190, 25);
        txtChatInput.PlaceholderText = "Nhập tin nhắn...";

        btnSendChat.Text = "Gửi";
        btnSendChat.Location = new Point(200, 320);
        btnSendChat.Size = new Size(60, 29);
        btnSendChat.BackColor = Color.FromArgb(31, 58, 95);
        btnSendChat.ForeColor = Color.White;
        btnSendChat.FlatStyle = FlatStyle.Flat;
        btnSendChat.Click += btnSendChat_Click;

        // 
        // pnlBottom
        // 
        pnlBottom.Dock = DockStyle.Bottom;
        pnlBottom.Height = 80;
        pnlBottom.BackColor = Color.White;
        pnlBottom.Controls.Add(btnLeave);
        pnlBottom.Controls.Add(btnAction);

        btnLeave.Text = "← Rời phòng";
        btnLeave.Location = new Point(30, 20);
        btnLeave.Size = new Size(130, 42);
        btnLeave.BackColor = Color.FromArgb(235, 240, 245);
        btnLeave.FlatStyle = FlatStyle.Flat;
        btnLeave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnLeave.Click += btnLeave_Click;

        btnAction.Text = "Bắt đầu trận đấu";
        btnAction.Location = new Point(380, 18);
        btnAction.Size = new Size(200, 45);
        btnAction.BackColor = Color.FromArgb(39, 174, 96);
        btnAction.ForeColor = Color.White;
        btnAction.FlatStyle = FlatStyle.Flat;
        btnAction.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnAction.Click += btnAction_Click;

        // 
        // RoomForm
        // 
        ClientSize = new Size(905, 520);
        Controls.Add(pnlBottom);
        Controls.Add(pnlChat);
        Controls.Add(pnlGuest);
        Controls.Add(pnlHost);
        Controls.Add(pnlHeader);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Text = "Cờ Tư Lệnh Online - Phòng Chờ";

        pnlHeader.ResumeLayout(false);
        pnlHeader.PerformLayout();
        pnlHost.ResumeLayout(false);
        pnlHost.PerformLayout();
        pnlGuest.ResumeLayout(false);
        pnlGuest.PerformLayout();
        pnlChat.ResumeLayout(false);
        pnlChat.PerformLayout();
        pnlBottom.ResumeLayout(false);
        ResumeLayout(false);
    }
}
