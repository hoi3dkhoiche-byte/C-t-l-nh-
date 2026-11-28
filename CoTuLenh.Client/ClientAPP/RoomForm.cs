using System.Windows.Forms;

namespace ClientAPP;

public partial class RoomForm : BaseForm
{
    private readonly RoomModel _room;
    private string _myColor = "red";
    private int? _redUserId;
    private int? _blueUserId;
    private bool _hasSentReady;
    private bool _transitioningToGame;
    private bool _leavingHandled;

    public RoomForm(RoomModel room)
    {
        _room = room;
        InitializeComponent();
        this.Load += RoomForm_Load;
        this.FormClosed += RoomForm_FormClosed;
    }

    private async void RoomForm_Load(object? sender, EventArgs e)
    {
        lblRoomTitle.Text = $"PHÒNG CHỜ: {_room.RoomCode}";
        lblBetInfo.Text = $"Mức cược: {_room.BetAmount:N0} Xu";

        // Chế độ luyện tập Offline (nếu mở từ phòng Offline)
        if (_room.IsOfflinePractice)
        {
            lblHostName.Text = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
            lblHostStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG";
            lblHostStatus.ForeColor = Color.FromArgb(39, 174, 96);

            lblGuestName.Text = "Thiếu Tá Quân Khu (Bot)";
            lblGuestName.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblGuestName.ForeColor = Color.FromArgb(44, 62, 80);
            lblGuestStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG";
            lblGuestStatus.ForeColor = Color.FromArgb(39, 174, 96);
            btnAction.Text = "Bắt đầu trận đấu";
            return;
        }

        // Chế độ đấu LAN / Online qua Socket.IO
        lblHostName.Text = _room.HostUsername;
        lblGuestName.Text = "Đang chờ đối thủ vào phòng...";
        ResetReadyStatesUi();

        btnAction.Text = "✋ SẴN SÀNG";
        btnAction.Enabled = false;

        SubscribeRealtimeEvents();

        AddChatLine($"[Hệ thống] Đang kết nối vào phòng {_room.RoomCode}...");
        bool sent = await RealtimeService.Instance.JoinRoomAsync(_room.RoomCode);
        if (!sent)
        {
            ShowError($"Không thể kết nối Socket.IO tới máy chủ ({ServerConfig.BaseUrl})!");
            ReturnToLobby(sendLeaveEvent: false);
        }
    }

    private void SubscribeRealtimeEvents()
    {
        var rt = RealtimeService.Instance;
        rt.OnRoomJoined += HandleRoomJoined;
        rt.OnPlayerJoined += HandlePlayerJoined;
        rt.OnPlayerLeft += HandlePlayerLeft;
        rt.OnJoinPending += HandleJoinPending;
        rt.OnJoinRequest += HandleJoinRequest;
        rt.OnJoinRequestExpired += HandleJoinRequestExpired;
        rt.OnJoinRequestCancelled += HandleJoinRequestCancelled;
        rt.OnPlayerReady += HandlePlayerReady;
        rt.OnGameStarted += HandleGameStarted;
        rt.OnSocketError += HandleSocketError;
        rt.OnChatMessage += HandleChatMessage;
        rt.OnChatRejected += HandleChatRejected;
    }

    private void UnsubscribeRealtimeEvents()
    {
        var rt = RealtimeService.Instance;
        rt.OnRoomJoined -= HandleRoomJoined;
        rt.OnPlayerJoined -= HandlePlayerJoined;
        rt.OnPlayerLeft -= HandlePlayerLeft;
        rt.OnJoinPending -= HandleJoinPending;
        rt.OnJoinRequest -= HandleJoinRequest;
        rt.OnJoinRequestExpired -= HandleJoinRequestExpired;
        rt.OnJoinRequestCancelled -= HandleJoinRequestCancelled;
        rt.OnPlayerReady -= HandlePlayerReady;
        rt.OnGameStarted -= HandleGameStarted;
        rt.OnSocketError -= HandleSocketError;
        rt.OnChatMessage -= HandleChatMessage;
        rt.OnChatRejected -= HandleChatRejected;
    }

    private void RunOnUi(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(action); } catch { }
        }
        else
        {
            action();
        }
    }

    private void AddChatLine(string line)
    {
        lstChat.Items.Add(line);
        lstChat.TopIndex = lstChat.Items.Count - 1;
    }

    private void HandleChatMessage(ChatMessageModel msg)
    {
        RunOnUi(() => AddChatLine($"[{msg.Username}]: {msg.Message}"));
    }

    private void HandleChatRejected(string reasonText)
    {
        RunOnUi(() => AddChatLine($"[Hệ thống] {reasonText}"));
    }

    private void ResetReadyStatesUi()
    {
        _hasSentReady = false;

        lblHostStatus.Text = "Trạng thái: Chưa sẵn sàng";
        lblHostStatus.ForeColor = Color.OrangeRed;

        lblGuestStatus.Text = _blueUserId.HasValue ? "Trạng thái: Chưa sẵn sàng" : "Trạng thái: Đang chờ người vào...";
        lblGuestStatus.ForeColor = Color.OrangeRed;

        UpdateReadyButtonState();
    }

    private void UpdateReadyButtonState()
    {
        bool bothPresent = _redUserId.HasValue && _blueUserId.HasValue;
        if (!bothPresent)
        {
            btnAction.Enabled = false;
            btnAction.Text = "⏳ Đợi đủ 2 kỳ thủ...";
            btnAction.BackColor = Color.Gray;
        }
        else if (_hasSentReady)
        {
            btnAction.Enabled = false;
            btnAction.Text = "✓ ĐÃ SẴN SÀNG (Đợi đối thủ)";
            btnAction.BackColor = Color.FromArgb(41, 128, 185);
        }
        else
        {
            btnAction.Enabled = true;
            btnAction.Text = "✋ BẤM SẴN SÀNG";
            btnAction.BackColor = Color.FromArgb(39, 174, 96);
        }
    }

    private async Task RefreshPlayerNamesAsync()
    {
        string redName = await GameApiService.Instance.ResolveUsernameAsync(_redUserId, "Chủ phòng");
        string? blueName = _blueUserId.HasValue
            ? await GameApiService.Instance.ResolveUsernameAsync(_blueUserId, "Kỳ thủ")
            : null;

        RunOnUi(() =>
        {
            string myTagRed = _myColor == "red" ? " (Bạn)" : "";
            string myTagBlue = _myColor == "blue" ? " (Bạn)" : "";

            lblHostName.Text = $"{redName}{myTagRed}";
            _room.HostUsername = redName;

            if (!string.IsNullOrEmpty(blueName))
            {
                lblGuestName.Text = $"{blueName}{myTagBlue}";
                lblGuestName.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
                lblGuestName.ForeColor = Color.FromArgb(44, 62, 80);
                _room.GuestUsername = blueName;
            }
            else
            {
                lblGuestName.Text = "Đang chờ đối thủ...";
                lblGuestName.Font = new Font("Segoe UI", 13F, FontStyle.Italic);
                lblGuestName.ForeColor = Color.Gray;
                _room.GuestUsername = null;
            }

            UpdateReadyButtonState();
        });
    }

    private void HandleRoomJoined(RoomJoinedEvent ev)
    {
        RunOnUi(async () =>
        {
            _myColor = ev.YourColor;
            _redUserId = ev.Players.Red;
            _blueUserId = ev.Players.Blue;
            _room.HostUserId = _redUserId ?? _room.HostUserId;
            _room.GuestUserId = _blueUserId;

            if (!string.IsNullOrWhiteSpace(ev.PlayerNames?.Red) && _redUserId.HasValue)
            {
                GameApiService.Instance.CacheUser(_redUserId.Value, ev.PlayerNames.Red);
            }
            if (!string.IsNullOrWhiteSpace(ev.PlayerNames?.Blue) && _blueUserId.HasValue)
            {
                GameApiService.Instance.CacheUser(_blueUserId.Value, ev.PlayerNames.Blue);
            }

            await RefreshPlayerNamesAsync();

            AddChatLine($"[Hệ thống] Đã vào phòng {ev.RoomCode}! Bạn cầm quân {(_myColor == "red" ? "ĐỎ (Chủ phòng)" : "XANH (Khách)")}.");

            // Trường hợp kết nối lại (Reconnect) vào ván đấu đang diễn ra trên Server -> vào thẳng bàn cờ khôi phục trạng thái
            if (ev.Game != null && !_transitioningToGame)
            {
                _transitioningToGame = true;
                UnsubscribeRealtimeEvents();
                var gameForm = new GamePlayForm(
                    _room,
                    isOnline: true,
                    myColor: _myColor,
                    initialBoard: ev.Game.Board,
                    initialTurn: ev.Game.Turn,
                    turnSeconds: ev.Game.TurnSeconds,
                    initialMoveCount: ev.Game.MoveCount + 1,
                    initialTurnRemainingSeconds: ev.Game.TurnRemainingSeconds,
                    initialRevision: ev.Game.Revision
                );
                gameForm.Show();
                this.FormClosed -= RoomForm_FormClosed;
                this.Close();
                return;
            }

            ResetReadyStatesUi();
            var myId = SessionService.Instance.CurrentUser?.Id;
            foreach (int readyId in ev.ReadyUserIds)
            {
                if (_redUserId == readyId)
                {
                    lblHostStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG ✅";
                    lblHostStatus.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else if (_blueUserId == readyId)
                {
                    lblGuestStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG ✅";
                    lblGuestStatus.ForeColor = Color.FromArgb(39, 174, 96);
                }
                if (myId.HasValue && readyId == myId.Value)
                {
                    _hasSentReady = true;
                }
            }
            UpdateReadyButtonState();
        });
    }

    private void HandlePlayerJoined(PlayerJoinedEvent ev)
    {
        RunOnUi(async () =>
        {
            if (!string.IsNullOrWhiteSpace(ev.Username))
            {
                GameApiService.Instance.CacheUser(ev.UserId, ev.Username);
            }
            if (ev.Color == "blue")
            {
                _blueUserId = ev.UserId;
                _room.GuestUserId = ev.UserId;
            }
            else if (ev.Color == "red")
            {
                _redUserId = ev.UserId;
                _room.HostUserId = ev.UserId;
            }

            await RefreshPlayerNamesAsync();
            ResetReadyStatesUi();

            string joinedName = ev.Username ?? GameApiService.Instance.ResolveCachedUsername(ev.UserId);
            AddChatLine($"[Hệ thống] ⚔️ {joinedName} đã vào phòng! Cả hai hãy bấm SẴN SÀNG.");
        });
    }

    private void HandlePlayerLeft(PlayerLeftEvent ev)
    {
        RunOnUi(() =>
        {
            if (ev.RoomClosed)
            {
                ShowInfo("Chủ phòng đã rời phòng. Phòng chờ đã đóng!");
                ReturnToLobby(sendLeaveEvent: false);
                return;
            }

            if (_blueUserId == ev.UserId)
            {
                string leftName = GameApiService.Instance.ResolveCachedUsername(ev.UserId);
                _blueUserId = null;
                _room.GuestUsername = null;
                lblGuestName.Text = "Đang chờ đối thủ...";
                lblGuestName.Font = new Font("Segoe UI", 13F, FontStyle.Italic);
                lblGuestName.ForeColor = Color.Gray;

                ResetReadyStatesUi();
                AddChatLine($"[Hệ thống] {leftName} đã rời phòng.");
            }
        });
    }

    private void HandleJoinPending(JoinPendingEvent ev)
    {
        RunOnUi(() =>
        {
            lblGuestName.Text = "Đang chờ Chủ phòng duyệt...";
            btnAction.Enabled = false;
            btnAction.Text = $"⏳ Chờ duyệt ({ev.TimeoutSeconds}s)...";
            AddChatLine($"[Hệ thống] Điểm uy tín của bạn dưới mức chuẩn, đang chờ Chủ phòng phê duyệt ({ev.TimeoutSeconds}s)...");
        });
    }

    private void HandleJoinRequest(JoinRequestEvent ev)
    {
        RunOnUi(async () =>
        {
            AddChatLine($"[Hệ thống] ⚠️ {ev.Username} (Uy tín: {ev.Reputation}/10) xin vào phòng.");

            bool accept = Confirm(
                $"Kỳ thủ '{ev.Username}' có điểm uy tín thấp ({ev.Reputation}/10) muốn vào phòng.\n" +
                $"Bạn có đồng ý cho phép vào thi đấu không? (Tự hủy sau {ev.TimeoutSeconds}s)",
                "Xét duyệt người chơi vào phòng"
            );

            await RealtimeService.Instance.AnswerJoinAsync(accept);
        });
    }

    private void HandleJoinRequestExpired(UserIdEvent ev)
    {
        RunOnUi(() => AddChatLine("[Hệ thống] Yêu cầu xin vào phòng đã hết hạn."));
    }

    private void HandleJoinRequestCancelled(UserIdEvent ev)
    {
        RunOnUi(() => AddChatLine("[Hệ thống] Người xin vào phòng đã hủy kết nối."));
    }

    private void HandlePlayerReady(UserIdEvent ev)
    {
        RunOnUi(() =>
        {
            var myId = SessionService.Instance.CurrentUser?.Id;
            if (myId.HasValue && ev.UserId == myId.Value)
            {
                _hasSentReady = true;
            }

            if (_redUserId == ev.UserId)
            {
                lblHostStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG ✅";
                lblHostStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else if (_blueUserId == ev.UserId)
            {
                lblGuestStatus.Text = "Trạng thái: ĐÃ SẴN SÀNG ✅";
                lblGuestStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }

            string who = GameApiService.Instance.ResolveCachedUsername(ev.UserId);
            AddChatLine($"[Hệ thống] ✅ {who} đã sẵn sàng!");
            UpdateReadyButtonState();
        });
    }

    private void HandleGameStarted(GameStartedEvent ev)
    {
        RunOnUi(async () =>
        {
            if (_transitioningToGame) return;
            _transitioningToGame = true;

            _redUserId = ev.Players.Red;
            _blueUserId = ev.Players.Blue;
            await RefreshPlayerNamesAsync();

            UnsubscribeRealtimeEvents();

            var gameForm = new GamePlayForm(
                _room,
                isOnline: true,
                myColor: _myColor,
                initialBoard: ev.Board,
                initialTurn: ev.FirstTurn,
                turnSeconds: ev.TurnSeconds,
                initialMoveCount: 1,
                initialTurnRemainingSeconds: ev.TurnSeconds,
                initialRevision: ev.Revision
            );
            gameForm.Show();
            this.FormClosed -= RoomForm_FormClosed;
            this.Close();
        });
    }

    private void HandleSocketError(SocketErrorEvent err)
    {
        RunOnUi(() =>
        {
            string detail = string.IsNullOrWhiteSpace(err.Message) ? err.Code : $"{err.Message} ({err.Code})";
            if (!string.IsNullOrWhiteSpace(err.BannedUntil))
            {
                detail += $"\nThời hạn cấm đến: {err.BannedUntil}";
            }

            ShowError(detail, "Thông báo từ máy chủ");

            if (err.Code is "ROOM_FULL" or "ROOM_NOT_FOUND" or "BANNED" or "JOIN_REJECTED" or "JOIN_COOLDOWN" or "AUTH_REQUIRED" or "ALREADY_IN_ROOM")
            {
                ReturnToLobby(sendLeaveEvent: false);
            }
        });
    }

    private async void btnAction_Click(object? sender, EventArgs e)
    {
        if (_room.IsOfflinePractice)
        {
            var gameForm = new GamePlayForm(_room);
            gameForm.Show();
            this.FormClosed -= RoomForm_FormClosed;
            this.Close();
            return;
        }

        if (_hasSentReady) return;
        btnAction.Enabled = false;
        await RealtimeService.Instance.SetPlayerReadyAsync();
    }

    private async void btnSendChat_Click(object? sender, EventArgs e)
    {
        string msg = txtChatInput.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (_room.IsOfflinePractice)
        {
            var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
            AddChatLine($"[{user}]: {msg}");
            txtChatInput.Clear();
            txtChatInput.Focus();
            return;
        }

        bool sent = await RealtimeService.Instance.SendChatAsync(msg);
        if (!sent)
        {
            AddChatLine("[Hệ thống] Mất kết nối server, chưa gửi được tin nhắn.");
            return;
        }
        txtChatInput.Clear();
        txtChatInput.Focus();
    }

    private void btnLeave_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có muốn rời khỏi phòng chờ này?"))
        {
            ReturnToLobby(sendLeaveEvent: !_room.IsOfflinePractice);
        }
    }

    private async void ReturnToLobby(bool sendLeaveEvent)
    {
        if (_leavingHandled) return;
        _leavingHandled = true;

        UnsubscribeRealtimeEvents();
        if (sendLeaveEvent)
        {
            await RealtimeService.Instance.LeaveRoomAsync(_room.RoomCode);
        }

        new LobbyForm().Show();
        this.FormClosed -= RoomForm_FormClosed;
        this.Close();
    }

    private async void RoomForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_leavingHandled || _transitioningToGame) return;
        _leavingHandled = true;

        UnsubscribeRealtimeEvents();
        if (!_room.IsOfflinePractice)
        {
            await RealtimeService.Instance.LeaveRoomAsync(_room.RoomCode);
        }
        new LobbyForm().Show();
    }
}
