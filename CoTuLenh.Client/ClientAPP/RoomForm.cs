using System.Windows.Forms;

namespace ClientAPP;

public partial class RoomForm : BaseForm
{
    private readonly RoomModel _room;
    private bool isGuestReady = false;

    public RoomForm(RoomModel room)
    {
        _room = room;
        InitializeComponent();
        this.Load += RoomForm_Load;
        this.FormClosed += RoomForm_FormClosed;
    }

    private void RoomForm_Load(object? sender, EventArgs e)
    {
        lblRoomTitle.Text = $"PHÒNG CHỜ: {_room.RoomCode}";
        lblBetInfo.Text = $"Mức cược: {_room.BetAmount:N0} Xu";

        var currentUser = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lblHostName.Text = _room.HostUsername;

        if (!string.IsNullOrEmpty(_room.GuestUsername))
        {
            lblGuestName.Text = _room.GuestUsername;
            lblGuestStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG";
            lblGuestStatus.ForeColor = Color.FromArgb(39, 174, 96);
            isGuestReady = true;
        }
        else
        {
            // Tự động gán bot / người chơi mẫu để test
            lblGuestName.Text = "Thiếu Tá Quân Khu (Mô phỏng)";
            lblGuestStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG";
            lblGuestStatus.ForeColor = Color.FromArgb(39, 174, 96);
            isGuestReady = true;
        }

        lstChat.Items.Add($"[Hệ thống] Bạn đã vào phòng {_room.RoomCode}");
        lstChat.Items.Add("[Hệ thống] Cả hai kỳ thủ hãy chuẩn bị!");
    }

    private void RoomForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        new LobbyForm().Show();
    }

    private void btnSendChat_Click(object? sender, EventArgs e)
    {
        string msg = txtChatInput.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lstChat.Items.Add($"[{user}]: {msg}");
        txtChatInput.Clear();
        txtChatInput.Focus();
    }

    private void btnAction_Click(object? sender, EventArgs e)
    {
        if (!isGuestReady)
        {
            ShowInfo("Vui lòng đợi đối thủ sẵn sàng!");
            return;
        }

        // Bắt đầu ván đấu -> chuyển sang GamePlayForm
        var gameForm = new GamePlayForm(_room);
        gameForm.Show();
        this.FormClosed -= RoomForm_FormClosed;
        this.Close();
    }

    private void btnLeave_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có muốn rời khỏi phòng này?"))
        {
            this.Close();
        }
    }
}
