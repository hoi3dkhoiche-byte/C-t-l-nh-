using System.Windows.Forms;

namespace ClientAPP;

public partial class LobbyForm : BaseForm
{
    public LobbyForm()
    {
        InitializeComponent();
        this.Load += LobbyForm_Load;
        this.FormClosed += LobbyForm_FormClosed;
        SessionService.Instance.OnUserDataChanged += UpdateUserInfoDisplay;
    }

    private async void LobbyForm_Load(object? sender, EventArgs e)
    {
        UpdateUserInfoDisplay();
        await LoadRoomsAsync();
    }

    private void LobbyForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        SessionService.Instance.OnUserDataChanged -= UpdateUserInfoDisplay;
        Application.Exit();
    }

    private void UpdateUserInfoDisplay()
    {
        var user = SessionService.Instance.CurrentUser;
        if (user != null)
        {
            lblUserInfo.Text = $"👤 {user.Username} | ELO: {user.EloRating}";
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

        if (rooms.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Hiện chưa có phòng nào. Hãy tạo phòng mới ngay!",
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

    private async void btnCreateRoom_Click(object? sender, EventArgs e)
    {
        // Chọn mức cược
        int bet = 1000;
        using (var dlg = new Form())
        {
            dlg.Text = "Tạo phòng mới";
            dlg.Size = new Size(320, 200);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            var lblPrompt = new Label { Text = "Chọn mức cược (Xu):", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var cmbBet = new ComboBox { Location = new Point(20, 50), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbBet.Items.AddRange(new object[] { "500 Xu", "1,000 Xu", "2,000 Xu", "5,000 Xu", "10,000 Xu", "50,000 Xu" });
            cmbBet.SelectedIndex = 1;

            var btnOk = new Button { Text = "Tạo ngay", DialogResult = DialogResult.OK, Location = new Point(20, 100), Width = 120, Height = 35, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "Huỷ", DialogResult = DialogResult.Cancel, Location = new Point(160, 100), Width = 120, Height = 35, FlatStyle = FlatStyle.Flat };

            dlg.Controls.AddRange(new Control[] { lblPrompt, cmbBet, btnOk, btnCancel });

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var betStr = cmbBet.SelectedItem?.ToString()?.Replace(" Xu", "").Replace(",", "").Trim();
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
    }

    private void btnJoinByCode_Click(object? sender, EventArgs e)
    {
        string code = txtRoomCode.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Vui lòng nhập mã phòng!");
            return;
        }

        var room = new RoomModel
        {
            RoomCode = code,
            HostUsername = "Đối thủ",
            BetAmount = 1000
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

    private void btnLogout_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có chắc chắn muốn đăng xuất?"))
        {
            SessionService.Instance.Logout();
            new LoginForm().Show();
            this.FormClosed -= LobbyForm_FormClosed;
            this.Close();
        }
    }
}
