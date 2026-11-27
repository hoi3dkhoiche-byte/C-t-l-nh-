using System.Windows.Forms;

namespace ClientAPP;

public partial class LobbyForm : BaseForm
{
    private Button? btnOfflinePractice;

    public LobbyForm()
    {
        InitializeComponent();
        SetupExtraControls();
        this.Load += LobbyForm_Load;
        this.FormClosed += LobbyForm_FormClosed;
        SessionService.Instance.OnUserDataChanged += UpdateUserInfoDisplay;
    }

    private void SetupExtraControls()
    {
        // Nút luyện tập Offline riêng biệt để danh sách phòng chờ luôn hiển thị phòng thật trên Server LAN
        btnOfflinePractice = new Button
        {
            Text = "🤖 Tập luyện Offline",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(165, 36),
            Location = new Point(380, 15),
            Cursor = Cursors.Hand
        };
        btnOfflinePractice.FlatAppearance.BorderSize = 0;
        btnOfflinePractice.Click += BtnOfflinePractice_Click;
        pnlActionBar.Controls.Add(btnOfflinePractice);
    }

    private async void LobbyForm_Load(object? sender, EventArgs e)
    {
        this.Text = $"Cờ Tư Lệnh Online - Sảnh Chờ ({ServerConfig.BaseUrl})";

        // Đồng bộ lại thông tin mới nhất (ELO, Xu, Uy tín) từ Server
        var refreshed = await AuthService.GetMeAsync();
        if (refreshed != null)
        {
            SessionService.Instance.SetCurrentUser(refreshed, SessionService.Instance.Token);
        }

        UpdateUserInfoDisplay();
        _ = RealtimeService.Instance.ConnectAsync();
        await LoadRoomsAsync();
    }

    private void LobbyForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        SessionService.Instance.OnUserDataChanged -= UpdateUserInfoDisplay;
        Application.Exit();
    }

    private void UpdateUserInfoDisplay()
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(UpdateUserInfoDisplay); } catch { }
            return;
        }

        var user = SessionService.Instance.CurrentUser;
        if (user != null)
        {
            lblUserInfo.Text = $"👤 {user.Username} | ELO: {user.EloRating} | ⭐{user.Reputation}";
            lblCoin.Text = $"💰 {user.Coins:N0} Xu";
        }
        else
        {
            lblUserInfo.Text = "👤 Khách";
            lblCoin.Text = "💰 0 Xu";
        }
    }

    private async Task LoadRoomsAsync()
    {
        ShowLoading(true);
        flowRooms.Controls.Clear();

        var rooms = await GameApiService.Instance.GetRoomsAsync();
        ShowLoading(false);

        if (!string.IsNullOrEmpty(GameApiService.Instance.LastError) && rooms.Count == 0)
        {
            var lblErr = new Label
            {
                Text = $"⚠️ {GameApiService.Instance.LastError}\nHãy kiểm tra lại IP máy chủ hoặc nhấn '🤖 Tập luyện Offline'.",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(192, 57, 43),
                AutoSize = true,
                Margin = new Padding(20)
            };
            flowRooms.Controls.Add(lblErr);
            return;
        }

        if (rooms.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Hiện chưa có phòng chờ nào trên máy chủ. Hãy nhấn '＋ Tạo phòng mới' để mời bạn cùng LAN vào đấu!",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(20)
            };
            flowRooms.Controls.Add(lblEmpty);
            return;
        }

        foreach (var room in rooms)
        {
            var card = new RoomCard(room);
            card.OnJoinClicked += Card_OnJoinClicked;
            flowRooms.Controls.Add(card);
        }
    }

    private void Card_OnJoinClicked(RoomModel room)
    {
        JoinRoom(room);
    }

    private void JoinRoom(RoomModel room)
    {
        var roomForm = new RoomForm(room);
        roomForm.Show();
        this.Hide();
    }

    private void BtnOfflinePractice_Click(object? sender, EventArgs e)
    {
        var practiceRoom = new RoomModel
        {
            RoomCode = "OFFLINE",
            HostUsername = SessionService.Instance.CurrentUser?.Username ?? "Tôi",
            GuestUsername = "Thiếu Tá Quân Khu (Bot)",
            BetAmount = 0,
            IsOfflinePractice = true
        };
        var gameForm = new GamePlayForm(practiceRoom);
        gameForm.Show();
        this.Hide();
    }

    private async void btnCreateRoom_Click(object? sender, EventArgs e)
    {
        int bet = 1000;
        using (var dlg = new Form())
        {
            dlg.Text = "Tạo phòng thi đấu LAN / Online";
            dlg.Size = new Size(320, 200);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            var lblPrompt = new Label { Text = "Chọn mức cược (Xu):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var cmbBet = new ComboBox { Location = new Point(20, 50), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbBet.Items.AddRange(new object[] { "0 Xu (Giao hữu)", "500 Xu", "1,000 Xu", "2,000 Xu", "5,000 Xu", "10,000 Xu", "50,000 Xu" });
            cmbBet.SelectedIndex = 2;

            var btnOk = new Button { Text = "Tạo ngay", DialogResult = DialogResult.OK, Location = new Point(20, 100), Width = 120, Height = 35, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "Huỷ", DialogResult = DialogResult.Cancel, Location = new Point(160, 100), Width = 120, Height = 35, FlatStyle = FlatStyle.Flat };

            dlg.Controls.AddRange(new Control[] { lblPrompt, cmbBet, btnOk, btnCancel });

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var betStr = cmbBet.SelectedItem?.ToString()?.Split(' ')[0].Replace(",", "").Trim();
                int.TryParse(betStr, out bet);
            }
            else
            {
                return;
            }
        }

        ShowLoading(true);
        var created = await GameApiService.Instance.CreateRoomAsync(bet);
        ShowLoading(false);

        if (created != null)
        {
            JoinRoom(created);
        }
        else
        {
            ShowError(GameApiService.Instance.LastError ?? "Không thể tạo phòng trên máy chủ!");
        }
    }

    private void btnJoinByCode_Click(object? sender, EventArgs e)
    {
        string code = txtRoomCode.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Vui lòng nhập mã phòng (6 ký tự)!");
            return;
        }

        var room = new RoomModel
        {
            RoomCode = code,
            HostUsername = "Chủ phòng",
            BetAmount = 1000,
            IsOfflinePractice = false
        };
        JoinRoom(room);
    }

    private async void btnRefresh_Click(object? sender, EventArgs e)
    {
        await LoadRoomsAsync();
    }

    private void btnDeposit_Click(object? sender, EventArgs e)
    {
        var depositForm = new DepositForm();
        depositForm.ShowDialog(this);
    }

    private void btnProfile_Click(object? sender, EventArgs e)
    {
        var profileForm = new ProfileForm();
        profileForm.ShowDialog(this);
    }

    private void btnLeaderboard_Click(object? sender, EventArgs e)
    {
        var lbForm = new LeaderboardForm();
        lbForm.ShowDialog(this);
    }

    private void btnHistory_Click(object? sender, EventArgs e)
    {
        var historyForm = new HistoryForm();
        historyForm.ShowDialog(this);
    }

    private async void btnLogout_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có chắc chắn muốn đăng xuất?"))
        {
            await RealtimeService.Instance.DisconnectAsync();
            SessionService.Instance.Logout();
            new LoginForm().Show();
            this.FormClosed -= LobbyForm_FormClosed;
            this.Close();
        }
    }
}
