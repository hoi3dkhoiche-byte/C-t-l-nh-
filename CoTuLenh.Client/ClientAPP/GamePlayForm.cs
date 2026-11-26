using System.Windows.Forms;

namespace ClientAPP;

public partial class GamePlayForm : BaseForm
{
    private readonly RoomModel _room;
    private readonly bool _isOnline;
    private readonly string _myColor;
    private readonly List<ServerPieceDto>? _initialServerBoard;
    private readonly string _initialTurn;
    private readonly int _turnDurationSeconds;

    private int moveCount = 1;
    private int currentTurnSeconds = 45;
    private bool isBotEnabled = false;
    private bool _matchFinished = false;
    private Button? btnToggleBot;

    public GamePlayForm(
        RoomModel room,
        bool isOnline = false,
        string myColor = "red",
        List<ServerPieceDto>? initialBoard = null,
        string initialTurn = "red",
        int turnSeconds = 45,
        int initialMoveCount = 1)
    {
        _room = room;
        _isOnline = isOnline;
        _myColor = myColor;
        _initialServerBoard = initialBoard;
        _initialTurn = initialTurn;
        _turnDurationSeconds = turnSeconds > 0 ? turnSeconds : 45;
        currentTurnSeconds = _turnDurationSeconds;
        moveCount = Math.Max(1, initialMoveCount);

        InitializeComponent();
        this.Load += GamePlayForm_Load;
        this.FormClosed += GamePlayForm_FormClosed;

        board.IsOnlineMode = _isOnline;
        board.PlayerSide = _myColor;

        board.OnPieceMoved += Board_OnPieceMoved;
        board.OnOnlineMoveRequested += Board_OnOnlineMoveRequested;
        board.OnPieceSelected += Board_OnPieceSelected;
        board.OnCaptureModeChanged += Board_OnCaptureModeChanged;
        board.OnStackedGroupSelected += Board_OnStackedGroupSelected;
        board.OnFireRingInspected += Board_OnFireRingInspected;

        gameTimer.Interval = 1000;
        gameTimer.Tick += GameTimer_Tick;
    }

    private void GamePlayForm_Load(object? sender, EventArgs e)
    {
        var meName = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        string redName = !string.IsNullOrWhiteSpace(_room.HostUsername) ? _room.HostUsername : "Phe Đỏ";
        string blueName = !string.IsNullOrWhiteSpace(_room.GuestUsername) ? _room.GuestUsername : "Phe Xanh";

        if (_isOnline)
        {
            this.Text = $"Cờ Tư Lệnh Online - Phòng {_room.RoomCode} ({(_myColor == "red" ? "PHE ĐỎ" : "PHE XANH")})";

            if (_myColor == "red")
            {
                lblPlayerName.Text = $"🔴 {meName} (Bạn - Đỏ)";
                lblPlayerName.ForeColor = Color.FromArgb(192, 57, 43);
                lblOpponentName.Text = $"🔵 {blueName} (Đối thủ - Xanh)";
                lblOpponentName.ForeColor = Color.FromArgb(41, 128, 185);
            }
            else
            {
                lblPlayerName.Text = $"🔵 {meName} (Bạn - Xanh)";
                lblPlayerName.ForeColor = Color.FromArgb(41, 128, 185);
                lblOpponentName.Text = $"🔴 {redName} (Đối thủ - Đỏ)";
                lblOpponentName.ForeColor = Color.FromArgb(192, 57, 43);
            }

            // Nạp bàn cờ chuẩn từ Server và đăng ký nhận sự kiện Realtime
            board.LoadBoardFromServer(_initialServerBoard, _initialTurn);
            SubscribeOnlineEvents();

            // Ẩn nút Xin hoà (Server hiện chưa có sự kiện xin hoà) và không tạo nút Bot khi đấu Online 2 máy
            btnDraw.Enabled = false;
            btnDraw.BackColor = Color.LightGray;

            lstChat.Items.Add($"[Hệ thống] ⚔️ Trận đấu LAN/Online phòng {_room.RoomCode} bắt đầu!");
            lstChat.Items.Add($"[Hệ thống] Bạn cầm quân {(_myColor == "red" ? "🔴 PHE ĐỎ (Nửa dưới Y=0..4)" : "🔵 PHE XANH (Nửa trên Y=7..11)")}.");
            lstChat.Items.Add($"[Hệ thống] Thời gian mỗi lượt: {_turnDurationSeconds} giây.");
        }
        else
        {
            lblPlayerName.Text = $"🔴 {meName} (Đỏ)";
            lblOpponentName.Text = $"🔵 {blueName} (Xanh)";

            // Nút chuyển đổi chế độ Bot / Tự cầm 2 bên khi tập luyện Offline
            btnToggleBot = new Button
            {
                Text = isBotEnabled ? "🤖 Bot: ĐANG BẬT" : "👤 Chế độ: TỰ CẦM 2 BÊN",
                Location = new Point(10, 605),
                Size = new Size(240, 36),
                BackColor = isBotEnabled ? Color.FromArgb(39, 174, 96) : Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnToggleBot.Click += (s, ev) =>
            {
                isBotEnabled = !isBotEnabled;
                btnToggleBot.Text = isBotEnabled ? "🤖 Bot: ĐANG BẬT" : "👤 Chế độ: TỰ CẦM 2 BÊN";
                btnToggleBot.BackColor = isBotEnabled ? Color.FromArgb(39, 174, 96) : Color.FromArgb(41, 128, 185);
                lstChat.Items.Add(isBotEnabled ? "[Hệ thống] Đã BẬT Bot tự đánh cho phe Xanh." : "[Hệ thống] Đã TẮT Bot (Bạn tự điều khiển cả 2 bên).");
                if (isBotEnabled && board.CurrentTurn == "blue")
                {
                    SimulateOpponentMove();
                }
            };
            pnlRight.Controls.Add(btnToggleBot);

            lstChat.Items.Add("[Hệ thống] Chế độ Tập luyện Offline bắt đầu!");
            lstChat.Items.Add("[Hệ thống] Lượt đầu tiên: PHE ĐỎ");
        }

        // Sự kiện khung chế độ ăn quân và nhóm quân xếp chồng
        btnToggleCaptureMode.Click += (s, ev) => board.ToggleCaptureMode();
        chkMoveStackTogether.CheckedChanged += (s, ev) =>
        {
            board.MoveStackTogether = chkMoveStackTogether.Checked;
            lstChat.Items.Add(board.MoveStackTogether
                ? "[Ghép quân] Đã BẬT: Cõng theo cả cụm khi di chuyển."
                : "[Ghép quân] Đã TẮT: Tách quân di chuyển riêng lẻ.");
            lstChat.TopIndex = lstChat.Items.Count - 1;
        };

        UpdateTurnDisplay();
        UpdateScores();
        UpdateCaptureModeDisplay();
        UpdateStackSelectDisplay(new List<PieceView>());
        UpdateTimerLabels();
        gameTimer.Start();
    }

    private void SubscribeOnlineEvents()
    {
        var rt = RealtimeService.Instance;
        rt.OnMoveMade += HandleServerMoveMade;
        rt.OnMoveRejected += HandleServerMoveRejected;
        rt.OnTurnTimeout += HandleServerTurnTimeout;
        rt.OnOpponentDisconnected += HandleOpponentDisconnected;
        rt.OnPlayerReconnected += HandlePlayerReconnected;
        rt.OnRoomJoined += HandleRoomRejoinedResync;
        rt.OnGameOver += HandleServerGameOver;
        rt.OnSocketError += HandleSocketError;
    }

    private void UnsubscribeOnlineEvents()
    {
        var rt = RealtimeService.Instance;
        rt.OnMoveMade -= HandleServerMoveMade;
        rt.OnMoveRejected -= HandleServerMoveRejected;
        rt.OnTurnTimeout -= HandleServerTurnTimeout;
        rt.OnOpponentDisconnected -= HandleOpponentDisconnected;
        rt.OnPlayerReconnected -= HandlePlayerReconnected;
        rt.OnRoomJoined -= HandleRoomRejoinedResync;
        rt.OnGameOver -= HandleServerGameOver;
        rt.OnSocketError -= HandleSocketError;
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

    private async void Board_OnOnlineMoveRequested(PieceView piece, int toX, int toY, CaptureMode captureMode, bool moveStack)
    {
        await RealtimeService.Instance.MakeMoveAsync(
            piece.PieceId,
            piece.X,
            piece.Y,
            toX,
            toY,
            captureMode,
            moveStack
        );
    }

    private void HandleServerMoveMade(MoveMadeEvent ev)
    {
        RunOnUi(() =>
        {
            var m = ev.Move;
            var (actingPiece, capturedPieces) = board.ApplyServerMove(m, ev.NextTurn);

            string pieceName = actingPiece?.DisplayName ?? m.PieceId;
            string sideLabel = m.By == "red" ? "Đỏ" : "Xanh";
            int destX = m.To.Count > 0 ? m.To[0] : 0;
            int destY = m.To.Count > 1 ? m.To[1] : 0;

            string capText = capturedPieces.Count > 0
                ? $" [Ăn: {string.Join(", ", capturedPieces.Select(c => c.DisplayName))}]"
                : "";
            string downText = m.AttackerDestroyed ? " [Bị phòng không bắn hạ!]" : "";
            string actionStr = m.StandShoot ? " 🎯 Bắn tại chỗ ➔ " : " ➔ ";

            int num = m.No > 0 ? m.No : moveCount++;
            moveCount = Math.Max(moveCount, num + 1);

            string log = $"{num}. {pieceName} ({sideLabel}){actionStr}({destX}, {destY}){capText}{downText}";
            lstMoveLog.Items.Add(log);
            lstMoveLog.TopIndex = lstMoveLog.Items.Count - 1;

            currentTurnSeconds = _turnDurationSeconds;
            UpdateTimerLabels();
            UpdateTurnDisplay();
            UpdateScores();
            UpdateCaptureModeDisplay();
            UpdateStackSelectDisplay(new List<PieceView>());
        });
    }

    private void HandleServerMoveRejected(MoveRejectedEvent ev)
    {
        RunOnUi(() =>
        {
            string reasonVi = ev.Reason switch
            {
                "NOT_YOUR_TURN" => "Chưa tới lượt đi của bạn!",
                "ILLEGAL_MOVE" => "Nước đi không hợp lệ theo luật máy chủ!",
                "OUT_OF_SYNC" => "Vị trí quân cờ bị lệch đồng bộ!",
                "NOT_YOUR_PIECE" => "Quân cờ này không thuộc phe của bạn!",
                _ => $"Máy chủ từ chối nước đi ({ev.Reason})"
            };
            lstChat.Items.Add($"[Máy chủ] ❌ {reasonVi}");
            lstChat.TopIndex = lstChat.Items.Count - 1;
            board.ClearSelection();
        });
    }

    private void HandleServerTurnTimeout(TurnTimeoutEvent ev)
    {
        RunOnUi(() =>
        {
            board.CurrentTurn = ev.NextTurn;
            board.ClearSelection();
            currentTurnSeconds = _turnDurationSeconds;
            UpdateTimerLabels();
            UpdateTurnDisplay();
            UpdateCaptureModeDisplay();

            string skippedVi = ev.Skipped == "red" ? "Phe Đỏ" : "Phe Xanh";
            lstChat.Items.Add($"[Hệ thống] ⏰ {skippedVi} hết thời gian suy nghĩ và bị mất lượt!");
            lstChat.TopIndex = lstChat.Items.Count - 1;
        });
    }

    private void HandleOpponentDisconnected(OpponentDisconnectedEvent ev)
    {
        RunOnUi(() =>
        {
            lstChat.Items.Add($"[Hệ thống] ⚠️ Đối thủ bị mất kết nối mạng! Máy chủ đang giữ chỗ chờ {ev.TimeoutSeconds}s...");
            lstChat.TopIndex = lstChat.Items.Count - 1;
        });
    }

    private void HandlePlayerReconnected(UserIdEvent ev)
    {
        RunOnUi(() =>
        {
            lstChat.Items.Add("[Hệ thống] ✅ Đối thủ đã kết nối lại thành công! Trận đấu tiếp tục.");
            lstChat.TopIndex = lstChat.Items.Count - 1;
        });
    }

    private void HandleRoomRejoinedResync(RoomJoinedEvent ev)
    {
        RunOnUi(() =>
        {
            if (ev.Game != null)
            {
                board.LoadBoardFromServer(ev.Game.Board, ev.Game.Turn);
                moveCount = ev.Game.MoveCount + 1;
                UpdateTurnDisplay();
                UpdateScores();
                lstChat.Items.Add("[Hệ thống] 🔄 Đã khôi phục và đồng bộ lại bàn cờ từ máy chủ!");
                lstChat.TopIndex = lstChat.Items.Count - 1;
            }
        });
    }

    private void HandleSocketError(SocketErrorEvent err)
    {
        RunOnUi(() =>
        {
            lstChat.Items.Add($"[Lỗi máy chủ] {err.Message} ({err.Code})");
            lstChat.TopIndex = lstChat.Items.Count - 1;
        });
    }

    private void HandleServerGameOver(GameOverEvent ev)
    {
        RunOnUi(async () =>
        {
            if (_matchFinished) return;
            _matchFinished = true;
            gameTimer.Stop();
            UnsubscribeOnlineEvents();

            bool isDraw = ev.Result == "draw";
            bool isVictory = !isDraw && (
                (ev.Result == "red_win" && _myColor == "red") ||
                (ev.Result == "blue_win" && _myColor == "blue")
            );

            int myEloDelta = _myColor == "red"
                ? (ev.EloChange?.Red ?? 0)
                : (ev.EloChange?.Blue ?? 0);

            string reasonVi = ev.Reason switch
            {
                "commander_captured" => isVictory
                    ? "Tư Lệnh đối phương đã bị tiêu diệt!"
                    : "Tư Lệnh phe bạn đã bị tiêu diệt!",
                "no_ground_forces" => isVictory
                    ? "Toàn bộ lực lượng chủ lực bộ binh/tăng/pháo của đối phương đã bị tiêu diệt!"
                    : "Toàn bộ lực lượng chủ lực của bạn đã bị tiêu diệt!",
                "no_legal_moves" => isVictory
                    ? "Đối phương không còn nước đi hợp lệ!"
                    : "Phe bạn không còn nước đi hợp lệ!",
                "opponent_left" => isVictory
                    ? "Đối thủ đã rời trận / đầu hàng!"
                    : "Bạn đã rời khỏi trận đấu!",
                "disconnect_timeout" => isVictory
                    ? "Đối thủ mất kết nối quá thời gian quy định!"
                    : "Bạn mất kết nối quá thời gian quy định!",
                _ => $"Kết thúc ván đấu ({ev.Reason})"
            };

            // Đồng bộ lại hồ sơ người dùng từ /api/auth/me (ELO, Xu, Uy tín đã được Server lưu sẵn)
            var refreshedUser = await AuthService.GetMeAsync();
            if (refreshedUser != null)
            {
                SessionService.Instance.SetCurrentUser(refreshedUser, SessionService.Instance.Token);
            }

            string title = isDraw ? "HÒA TRẬN!" : (isVictory ? "CHIẾN THẮNG!" : "THẤT BẠI!");
            string outcomeText = isDraw ? "Hòa!" : (isVictory ? "Bạn đã chiến thắng!" : "Bạn đã thua!");
            string msg = $"{reasonVi}\n\n" +
                         $"Kết quả: {outcomeText}\n" +
                         $"Biến động ELO: {(myEloDelta >= 0 ? "+" : "")}{myEloDelta}";

            MessageBox.Show(
                this,
                msg,
                title,
                MessageBoxButtons.OK,
                isVictory ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation
            );

            this.FormClosed -= GamePlayForm_FormClosed;
            new LobbyForm().Show();
            this.Close();
        });
    }

    private async void GamePlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        gameTimer.Stop();
        UnsubscribeOnlineEvents();

        if (_isOnline && !_matchFinished)
        {
            _matchFinished = true;
            await RealtimeService.Instance.LeaveRoomAsync(_room.RoomCode);
            var refreshed = await AuthService.GetMeAsync();
            if (refreshed != null)
            {
                SessionService.Instance.SetCurrentUser(refreshed, SessionService.Instance.Token);
            }
        }

        new LobbyForm().Show();
    }

    private void UpdateTimerLabels()
    {
        string activeStr = $"⏱ 00:{Math.Max(0, currentTurnSeconds):D2}";
        string idleStr = $"⏱ 00:{_turnDurationSeconds:D2}";

        bool isMyTurn = board.CurrentTurn == (_isOnline ? _myColor : "red");
        if (isMyTurn)
        {
            lblPlayerTimer.Text = activeStr;
            lblOpponentTimer.Text = idleStr;
        }
        else
        {
            lblOpponentTimer.Text = activeStr;
            lblPlayerTimer.Text = idleStr;
        }
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (currentTurnSeconds > 0)
        {
            currentTurnSeconds--;
        }
        UpdateTimerLabels();

        // Khi đấu Online: Server là bên quyết định hết giờ qua sự kiện 'turn_timeout', Client không tự ý lật lượt cục bộ
        if (!_isOnline && currentTurnSeconds <= 0)
        {
            currentTurnSeconds = _turnDurationSeconds;
            board.CurrentTurn = board.CurrentTurn == "red" ? "blue" : "red";
            UpdateTurnDisplay();
            UpdateCaptureModeDisplay();
            UpdateStackSelectDisplay(new List<PieceView>());
            lstChat.Items.Add("[Hệ thống] Hết thời gian lượt đi!");
        }
    }

    private void Board_OnPieceSelected(PieceView? piece)
    {
        UpdateCaptureModeDisplay();
        if (piece == null)
        {
            UpdateStackSelectDisplay(new List<PieceView>());
        }
        else
        {
            var stacked = board.GetPiecesAt(piece.X, piece.Y);
            UpdateStackSelectDisplay(stacked.Count > 1 ? stacked : new List<PieceView>());
        }
    }

    private void Board_OnStackedGroupSelected(List<PieceView> pieces)
    {
        UpdateStackSelectDisplay(pieces);
        UpdateCaptureModeDisplay();
    }

    private void Board_OnCaptureModeChanged(CaptureMode mode)
    {
        UpdateCaptureModeDisplay();
    }

    private void Board_OnFireRingInspected(PieceView? piece)
    {
        if (piece != null)
        {
            string sideStr = piece.Side == "red" ? "Phe Đỏ" : "Phe Xanh";
            float radius = BoardControl.GetFireRingRadius(piece.Kind);
            lstChat.Items.Add($"[Trinh sát] 🔥 Đang soi vành đai lửa của {piece.DisplayName} ({sideStr}) - Bán kính {radius} nấc.");
        }
        else
        {
            lstChat.Items.Add("[Trinh sát] Đã tắt soi vành đai lửa.");
        }
        lstChat.TopIndex = lstChat.Items.Count - 1;
    }

    private void UpdateCaptureModeDisplay()
    {
        var piece = board.SelectedPiece;
        if (piece != null)
        {
            string sideStr = piece.Side == "red" ? "Đỏ" : "Xanh";
            lblCurrentPieceCapture.Text = $"Quân: {piece.DisplayName} ({sideStr})";

            if (BoardControl.HasMultipleCaptureModes(piece.Kind))
            {
                btnToggleCaptureMode.Enabled = true;
                if (board.CurrentCaptureMode == CaptureMode.Occupy)
                {
                    lblCaptureModeStatus.Text = "🎯 Chế độ: THẾ CHỖ MỤC TIÊU";
                    lblCaptureModeStatus.ForeColor = Color.FromArgb(192, 57, 43);
                    btnToggleCaptureMode.Text = "🔄 ĐỔI: BẮN TẠI CHỖ / BAY VỀ";
                    btnToggleCaptureMode.BackColor = Color.FromArgb(41, 128, 185);
                }
                else
                {
                    lblCaptureModeStatus.Text = "💥 Chế độ: BẮN TẠI CHỖ / BAY VỀ";
                    lblCaptureModeStatus.ForeColor = Color.FromArgb(211, 84, 0);
                    btnToggleCaptureMode.Text = "🔄 ĐỔI: THẾ CHỖ MỤC TIÊU";
                    btnToggleCaptureMode.BackColor = Color.FromArgb(230, 126, 34);
                }

                lblCaptureModeHelp.Text = piece.Kind switch
                {
                    PieceKind.AirForce => "• Oanh tạc tại chỗ: tiêu diệt mục tiêu mà không tiến vào ô địch.",
                    PieceKind.Artillery => "• Bắn tại chỗ: đứng tại chỗ nã pháo tiêu diệt từ xa (1-3 nấc).",
                    PieceKind.Warship => "• Pháo hạm tại chỗ: đứng tại chỗ bắn phá đất liền/biển từ xa.",
                    PieceKind.Tank => "• Bắn tại chỗ: đứng yên tiêu diệt mục tiêu trong tầm.",
                    _ => "• Chuyển đổi giữa ăn thế chỗ và bắn tại chỗ."
                };
            }
            else
            {
                btnToggleCaptureMode.Enabled = false;
                lblCaptureModeStatus.Text = "🎯 Ăn thế chỗ (Mặc định)";
                lblCaptureModeStatus.ForeColor = Color.FromArgb(44, 62, 80);
                btnToggleCaptureMode.Text = "🔄 ĐỔI CHẾ ĐỘ ĂN";
                btnToggleCaptureMode.BackColor = Color.Gray;
                lblCaptureModeHelp.Text = "• Quân này chỉ có 1 chế độ ăn tiêu chuẩn (Thế chỗ).";
            }
        }
        else
        {
            lblCurrentPieceCapture.Text = "Quân: Chưa chọn";
            btnToggleCaptureMode.Enabled = false;
            lblCaptureModeStatus.Text = board.CurrentCaptureMode == CaptureMode.Occupy
                ? "🎯 Ăn thế chỗ mục tiêu"
                : "💥 Bắn tại chỗ / Bay về";
            lblCaptureModeStatus.ForeColor = Color.Gray;
            btnToggleCaptureMode.Text = "🔄 ĐỔI CHẾ ĐỘ ĂN";
            btnToggleCaptureMode.BackColor = Color.Gray;
            lblCaptureModeHelp.Text = "• Chọn một quân cờ của bạn trên bàn cờ để xem hoặc đổi chế độ ăn.";
        }
    }

    private void UpdateStackSelectDisplay(List<PieceView> pieces)
    {
        flowStackPieces.Controls.Clear();
        if (pieces == null || pieces.Count <= 1)
        {
            lblStackTitle.Text = "🎖️ NHÓM QUÂN XẾP CHỒNG";
            lblStackInfo.Text = "Nhấp vào ô có nhóm quân để chọn quân:";
            chkMoveStackTogether.Visible = false;
            return;
        }

        string sideStr = pieces[0].Side == "red" ? "🔴 Phe Đỏ" : "🔵 Phe Xanh";
        lblStackTitle.Text = $"🎖️ NHÓM GHÉP ({pieces.Count} quân)";
        lblStackInfo.Text = $"Ô ({pieces[0].X}, {pieces[0].Y}) - {sideStr}. Chọn quân:";

        bool hasCarrier = pieces.Any(p => BoardControl.IsCarrier(p.Kind));
        chkMoveStackTogether.Visible = hasCarrier;
        chkMoveStackTogether.Checked = board.MoveStackTogether;

        foreach (var p in pieces)
        {
            var btn = new Button
            {
                Size = new Size(205, 27),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(2, 2, 2, 2),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = p
            };

            string heroStar = p.IsHero ? " ★" : "";
            string role = BoardControl.IsCarrier(p.Kind) ? "[Cõng]" : "[Quân]";
            btn.Text = $" {GetPieceIcon(p.Kind)} {p.DisplayName} ({p.Score}đ){heroStar} {role}";

            bool isSelected = (board.SelectedPiece == p);
            if (isSelected)
            {
                btn.BackColor = Color.FromArgb(241, 196, 15);
                btn.ForeColor = Color.Black;
                btn.FlatAppearance.BorderColor = Color.FromArgb(218, 165, 32);
                btn.FlatAppearance.BorderSize = 2;
            }
            else
            {
                btn.BackColor = p.Side == "red" ? Color.FromArgb(253, 237, 236) : Color.FromArgb(235, 245, 255);
                btn.ForeColor = p.Side == "red" ? Color.FromArgb(192, 57, 43) : Color.FromArgb(41, 128, 185);
                btn.FlatAppearance.BorderColor = Color.Silver;
                btn.FlatAppearance.BorderSize = 1;
            }

            btn.Click += (s, e) =>
            {
                board.SelectSpecificPiece(p);
                UpdateStackSelectDisplay(pieces);
                UpdateCaptureModeDisplay();
                lstChat.Items.Add($"[Ghép quân] Đã chọn {p.DisplayName} trong nhóm quân để điều khiển.");
                lstChat.TopIndex = lstChat.Items.Count - 1;
            };

            flowStackPieces.Controls.Add(btn);
        }
    }

    private static string GetPieceIcon(PieceKind kind)
    {
        return kind switch
        {
            PieceKind.Commander => "★",
            PieceKind.Infantry => "🪖",
            PieceKind.Tank => "🚜",
            PieceKind.Militia => "👒",
            PieceKind.Engineer => "🌉",
            PieceKind.Artillery => "💥",
            PieceKind.AntiAir => "🎯",
            PieceKind.Rocket => "🚀",
            PieceKind.AirForce => "🛩️",
            PieceKind.Warship => "⚓",
            _ => "♟"
        };
    }

    // Chế độ Offline (chỉ kích hoạt khi _isOnline == false)
    private void Board_OnPieceMoved(PieceView piece, int toX, int toY, PieceView? capturedPiece)
    {
        if (_isOnline) return;

        string capInfo = capturedPiece != null ? $" [Ăn: {capturedPiece.DisplayName}]" : "";
        string actionType = (piece.X == toX && piece.Y == toY) ? " ➔ " : " 🎯 Bắn tại chỗ ➔ ";

        var stackedAtDest = board.GetPiecesAt(toX, toY);
        string log;
        if (capturedPiece == null && stackedAtDest.Count > 1)
        {
            log = $"{moveCount++}. {piece.DisplayName} ({piece.Side}) 👥 Ghép quân ➔ ({toX}, {toY}) [{stackedAtDest.Count} quân]";
        }
        else
        {
            log = $"{moveCount++}. {piece.DisplayName} ({piece.Side}){actionType}({toX}, {toY}){capInfo}";
        }

        lstMoveLog.Items.Add(log);
        lstMoveLog.TopIndex = lstMoveLog.Items.Count - 1;

        currentTurnSeconds = _turnDurationSeconds;
        UpdateTurnDisplay();
        UpdateScores();
        UpdateCaptureModeDisplay();
        UpdateStackSelectDisplay(new List<PieceView>());

        var blueCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "b_cmd");
        if (blueCmd != null && !blueCmd.IsAlive)
        {
            EndOfflineGame(true, "Tư Lệnh đối phương đã bị tiêu diệt!");
            return;
        }

        var redCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "r_cmd");
        if (redCmd != null && !redCmd.IsAlive)
        {
            EndOfflineGame(false, "Tư Lệnh phe bạn đã bị tiêu diệt!");
            return;
        }

        bool blueGroundAssault = board.Pieces.Any(p => p.Side == "blue" && p.IsAlive &&
            (p.Kind == PieceKind.Tank || p.Kind == PieceKind.Infantry || p.Kind == PieceKind.Artillery));
        bool redGroundAssault = board.Pieces.Any(p => p.Side == "red" && p.IsAlive &&
            (p.Kind == PieceKind.Tank || p.Kind == PieceKind.Infantry || p.Kind == PieceKind.Artillery));

        if (!blueGroundAssault)
        {
            EndOfflineGame(true, "Toàn bộ lực lượng chủ lực (Xe tăng, Bộ binh, Pháo binh) đối phương đã bị tiêu diệt!");
            return;
        }
        if (!redGroundAssault)
        {
            EndOfflineGame(false, "Toàn bộ lực lượng chủ lực của bạn đã bị tiêu diệt!");
            return;
        }

        if (isBotEnabled && board.CurrentTurn == "blue")
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                if (this.IsDisposed) return;
                this.Invoke(() =>
                {
                    if (isBotEnabled && board.CurrentTurn == "blue")
                    {
                        SimulateOpponentMove();
                    }
                });
            });
        }
    }

    private void SimulateOpponentMove()
    {
        var bluePieces = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).ToList();
        if (bluePieces.Count == 0) return;

        var allMovable = bluePieces
            .Select(p => new { Piece = p, Moves = board.GetValidMoves(p) })
            .Where(x => x.Moves.Count > 0)
            .ToList();

        if (allMovable.Count == 0)
        {
            EndOfflineGame(true, "Đối phương không còn nước đi hợp lệ!");
            return;
        }

        var captureMoves = allMovable
            .SelectMany(m => m.Moves
                .Where(dest => board.Pieces.Any(target => target.X == dest.X && target.Y == dest.Y && target.Side == "red" && target.IsAlive))
                .Select(dest => new { m.Piece, Dest = dest, Target = board.Pieces.First(target => target.X == dest.X && target.Y == dest.Y && target.Side == "red" && target.IsAlive) }))
            .OrderByDescending(c => c.Target.Score)
            .ToList();

        if (captureMoves.Count > 0 && Random.Shared.Next(100) < 75)
        {
            var bestCap = captureMoves.First();
            board.MovePiece(bestCap.Piece.PieceId, bestCap.Dest.X, bestCap.Dest.Y);
        }
        else
        {
            var pick = allMovable[Random.Shared.Next(allMovable.Count)];
            var dest = pick.Moves[Random.Shared.Next(pick.Moves.Count)];
            board.MovePiece(pick.Piece.PieceId, dest.X, dest.Y);
        }
    }

    private void UpdateTurnDisplay()
    {
        if (_isOnline)
        {
            bool isMyTurn = board.CurrentTurn == _myColor;
            if (board.CurrentTurn == "red")
            {
                lblTurn.Text = isMyTurn ? "LƯỢT ĐI:\n🔴 PHE ĐỎ (LƯỢT CỦA BẠN)" : "LƯỢT ĐI:\n🔴 PHE ĐỎ (ĐỐI THỦ)";
                lblTurn.ForeColor = Color.FromArgb(192, 57, 43);
            }
            else
            {
                lblTurn.Text = isMyTurn ? "LƯỢT ĐI:\n🔵 PHE XANH (LƯỢT CỦA BẠN)" : "LƯỢT ĐI:\n🔵 PHE XANH (ĐỐI THỦ)";
                lblTurn.ForeColor = Color.FromArgb(41, 128, 185);
            }
            return;
        }

        if (board.CurrentTurn == "red")
        {
            lblTurn.Text = "LƯỢT ĐI:\n🔴 PHE ĐỎ (BẠN)";
            lblTurn.ForeColor = Color.FromArgb(192, 57, 43);
        }
        else
        {
            lblTurn.Text = "LƯỢT ĐI:\n🔵 PHE XANH (ĐỐI THỦ)";
            lblTurn.ForeColor = Color.FromArgb(41, 128, 185);
        }
    }

    private void UpdateScores()
    {
        int redScore = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score);
        int blueScore = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score);

        if (_isOnline && _myColor == "blue")
        {
            lblPlayerScore.Text = $"Lực lượng (Xanh): {blueScore}đ";
            lblOpponentScore.Text = $"Lực lượng (Đỏ): {redScore}đ";
        }
        else
        {
            lblPlayerScore.Text = $"Lực lượng (Đỏ): {redScore}đ";
            lblOpponentScore.Text = $"Lực lượng (Xanh): {blueScore}đ";
        }
    }

    private void EndOfflineGame(bool isVictory, string reason)
    {
        if (_matchFinished) return;
        _matchFinished = true;
        gameTimer.Stop();

        string title = isVictory ? "CHIẾN THẮNG!" : "THẤT BẠI!";
        string msg = $"{reason}\n\nKết quả tập luyện: {(isVictory ? "Phe Đỏ thắng!" : "Phe Xanh thắng!")}";
        MessageBox.Show(this, msg, title, MessageBoxButtons.OK, isVictory ? MessageBoxIcon.Information : MessageBoxIcon.Exclamation);

        this.FormClosed -= GamePlayForm_FormClosed;
        new LobbyForm().Show();
        this.Close();
    }

    private void btnSend_Click(object? sender, EventArgs e)
    {
        string msg = txtChat.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lstChat.Items.Add($"[{user}]: {msg}");
        lstChat.TopIndex = lstChat.Items.Count - 1;
        txtChat.Clear();
    }

    private void btnDraw_Click(object? sender, EventArgs e)
    {
        if (_isOnline)
        {
            ShowInfo("Máy chủ hiện chưa hỗ trợ tính năng xin hoà trong trận Online.");
            return;
        }

        if (Confirm("Bạn có muốn kết thúc ván tập luyện với kết quả Hoà?"))
        {
            _matchFinished = true;
            gameTimer.Stop();
            ShowInfo("Ván tập luyện kết thúc Hoà!");
            this.FormClosed -= GamePlayForm_FormClosed;
            new LobbyForm().Show();
            this.Close();
        }
    }

    private async void btnSurrender_Click(object? sender, EventArgs e)
    {
        if (_isOnline)
        {
            if (Confirm("Rời trận / đầu hàng lúc đang thi đấu sẽ bị xử thua (trừ ELO và điểm uy tín). Bạn chắc chắn chứ?"))
            {
                await LeaveOnlineMatchAsync();
            }
            return;
        }

        if (Confirm("Bạn có chắc chắn muốn ĐẦU HÀNG ván tập luyện này?"))
        {
            EndOfflineGame(false, "Bạn đã đầu hàng ván đấu.");
        }
    }

    private async void btnLeave_Click(object? sender, EventArgs e)
    {
        if (_isOnline)
        {
            if (Confirm("Thoát ván đấu giữa chừng sẽ bị máy chủ xử thua và trừ điểm uy tín. Bạn có chắc chắn?"))
            {
                await LeaveOnlineMatchAsync();
            }
            return;
        }

        if (Confirm("Bạn có muốn thoát ván tập luyện?"))
        {
            _matchFinished = true;
            gameTimer.Stop();
            this.FormClosed -= GamePlayForm_FormClosed;
            new LobbyForm().Show();
            this.Close();
        }
    }

    private async Task LeaveOnlineMatchAsync()
    {
        if (_matchFinished) return;
        _matchFinished = true;
        gameTimer.Stop();
        UnsubscribeOnlineEvents();

        await RealtimeService.Instance.LeaveRoomAsync(_room.RoomCode);
        var refreshed = await AuthService.GetMeAsync();
        if (refreshed != null)
        {
            SessionService.Instance.SetCurrentUser(refreshed, SessionService.Instance.Token);
        }

        ShowInfo("Bạn đã rời trận đấu (bị xử thua theo luật máy chủ).");
        this.FormClosed -= GamePlayForm_FormClosed;
        new LobbyForm().Show();
        this.Close();
    }
}
