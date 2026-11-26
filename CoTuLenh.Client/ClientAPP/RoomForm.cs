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
        // Handler riêng, luôn chạy khi form đóng (kể cả lúc chuyển sang GamePlayForm)
        this.FormClosed += RoomForm_UnsubscribeRealtime;
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

        // Nghe chat và thông báo phòng từ server
        RealtimeService.Instance.OnChatMessage += Realtime_OnChatMessage;
        RealtimeService.Instance.OnChatRejected += Realtime_OnSystemText;
        RealtimeService.Instance.OnRoomNotice += Realtime_OnSystemText;

        _ = JoinRoomRealtimeAsync();
    }

    private async Task JoinRoomRealtimeAsync()
    {
        bool ok = await RealtimeService.Instance.JoinRoomAsync(_room.RoomCode);
        if (!ok)
        {
            AddChatLine("[Hệ thống] Không kết nối được server, chat đang tắt");
        }
    }

    private void RoomForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _ = RealtimeService.Instance.LeaveRoomAsync();
        new LobbyForm().Show();
    }

    private void RoomForm_UnsubscribeRealtime(object? sender, FormClosedEventArgs e)
    {
        RealtimeService.Instance.OnChatMessage -= Realtime_OnChatMessage;
        RealtimeService.Instance.OnChatRejected -= Realtime_OnSystemText;
        RealtimeService.Instance.OnRoomNotice -= Realtime_OnSystemText;
    }

    private async void btnSendChat_Click(object? sender, EventArgs e)
    {
        string msg = txtChatInput.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        bool sent = await RealtimeService.Instance.SendChatAsync(msg);
        if (!sent)
        {
            AddChatLine("[Hệ thống] Mất kết nối server, chưa gửi được");
            return;
        }
        txtChatInput.Clear();
        txtChatInput.Focus();
    }

    private void Realtime_OnChatMessage(ChatMessageModel msg)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;
        this.BeginInvoke(new Action(() => AddChatLine($"[{msg.Username}]: {msg.Message}")));
    }

    private void Realtime_OnSystemText(string text)
    {
        if (this.IsDisposed || !this.IsHandleCreated) return;
        this.BeginInvoke(new Action(() => AddChatLine($"[Hệ thống] {text}")));
    }

    // Thêm một dòng vào khung chat và cuộn xuống dòng mới nhất
    private void AddChatLine(string line)
    {
        lstChat.Items.Add(line);
        lstChat.TopIndex = lstChat.Items.Count - 1;
    }

    private void btnAction_Click(object? sender, EventArgs e)
    {
        if (!isGuestReady)
        {
            ShowInfo("Vui lòng đợi đối thủ sẵn sàng!");
            return;
        }

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