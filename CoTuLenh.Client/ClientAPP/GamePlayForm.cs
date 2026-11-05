using System.Windows.Forms;

namespace ClientAPP;

public partial class GamePlayForm : BaseForm
{
    private readonly RoomModel _room;
    private int moveCount = 1;
    private int currentTurnSeconds = 45;
    private bool isBotEnabled = false; // Mặc định tắt để bạn tự do thử nghiệm điều khiển cả 2 bên Đỏ và Xanh
    private Button? btnToggleBot;

    public GamePlayForm(RoomModel room)
    {
        _room = room;
        InitializeComponent();
        this.Load += GamePlayForm_Load;
        this.FormClosed += GamePlayForm_FormClosed;

        board.OnPieceMoved += Board_OnPieceMoved;
        board.OnPieceSelected += Board_OnPieceSelected;
        board.OnCaptureModeChanged += Board_OnCaptureModeChanged;
        board.OnStackedGroupSelected += Board_OnStackedGroupSelected;
        board.OnFireRingInspected += Board_OnFireRingInspected;

        gameTimer.Interval = 1000;
        gameTimer.Tick += GameTimer_Tick;
    }

    private void GamePlayForm_Load(object? sender, EventArgs e)
    {
        var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lblPlayerName.Text = $"🔴 {user} (Đỏ)";
        lblOpponentName.Text = $"🔵 {_room.GuestUsername ?? "Đối thủ"} (Xanh)";

        // Nút chuyển đổi chế độ Bot / Tự cầm 2 bên
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

        lstChat.Items.Add("[Hệ thống] Trận đấu Cờ Tư Lệnh bắt đầu!");
        lstChat.Items.Add("[Hệ thống] Hỗ trợ: Nhóm xếp chồng (Ghép quân), Đổi chế độ ăn & Soi vành đai lửa bằng chuột phải.");
        lstChat.Items.Add("[Hệ thống] Lượt đầu tiên: PHE ĐỎ");

        UpdateTurnDisplay();
        UpdateScores();
        UpdateCaptureModeDisplay();
        UpdateStackSelectDisplay(new List<PieceView>());
        gameTimer.Start();
    }

    private void GamePlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        gameTimer.Stop();
        new LobbyForm().Show();
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        currentTurnSeconds--;
        string timeStr = $"⏱ 00:{currentTurnSeconds:D2}";

        if (board.CurrentTurn == "red")
        {
            lblPlayerTimer.Text = timeStr;
            lblOpponentTimer.Text = "⏱ 00:45";
        }
        else
        {
            lblOpponentTimer.Text = timeStr;
            lblPlayerTimer.Text = "⏱ 00:45";
        }

        if (currentTurnSeconds <= 0)
        {
            currentTurnSeconds = 45;
            // Hết giờ -> Đổi lượt
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
            if (stacked.Count > 1)
            {
                UpdateStackSelectDisplay(stacked);
            }
            else
            {
                UpdateStackSelectDisplay(new List<PieceView>());
            }
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
                    PieceKind.AirForce => "• Oanh tạc bay về: diệt địch xong bay trở về vị trí an toàn.",
                    PieceKind.Artillery => "• Bắn tại chỗ: đứng tại chỗ nã pháo tiêu diệt từ xa (1-3 nấc).",
                    PieceKind.Warship => "• Pháo hạm tại chỗ: đứng tại chỗ bắn phá đất liền/biển từ xa.",
                    PieceKind.Tank => "• Bắn tại chỗ: đứng trên bờ bắn tiêu diệt mục tiêu ven biển/hải quân.",
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
            lblCaptureModeHelp.Text = "• Chọn một quân cờ trên bàn cờ để xem hoặc đổi chế độ ăn.";
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

    private void Board_OnPieceMoved(PieceView piece, int toX, int toY, PieceView? capturedPiece)
    {
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

        currentTurnSeconds = 45;
        UpdateTurnDisplay();
        UpdateScores();
        UpdateCaptureModeDisplay();
        UpdateStackSelectDisplay(new List<PieceView>());

        // =========================================================================
        // KIỂM TRA ĐIỀU KIỆN KẾT THÚC VÁN CỜ THEO LUẬT CỜ TƯ LỆNH:
        // =========================================================================

        // 1. Tướng (Tư lệnh) bị bắt hoặc tiêu diệt
        var blueCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "b_cmd");
        if (blueCmd != null && !blueCmd.IsAlive)
        {
            EndGame(true, "Tư Lệnh đối phương đã bị tiêu diệt!");
            return;
        }

        var redCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "r_cmd");
        if (redCmd != null && !redCmd.IsAlive)
        {
            EndGame(false, "Tư Lệnh phe bạn đã bị tiêu diệt!");
            return;
        }

        // 2. Toàn bộ quân Không chiến (Không quân) bị tiêu diệt
        bool blueAirAlive = board.Pieces.Any(p => p.Side == "blue" && p.Kind == PieceKind.AirForce && p.IsAlive);
        bool redAirAlive = board.Pieces.Any(p => p.Side == "red" && p.Kind == PieceKind.AirForce && p.IsAlive);
        if (!blueAirAlive && blueCmd != null)
        {
            // Đối phương mất toàn bộ không quân
        }

        // 3. Toàn bộ quân Hải chiến bị tiêu diệt
        bool blueSeaAlive = board.Pieces.Any(p => p.Side == "blue" && p.Kind == PieceKind.Warship && p.IsAlive);
        bool redSeaAlive = board.Pieces.Any(p => p.Side == "red" && p.Kind == PieceKind.Warship && p.IsAlive);

        // 4. Toàn bộ Xe tăng, Bộ binh, Pháo binh bị tiêu diệt
        bool blueGroundAssault = board.Pieces.Any(p => p.Side == "blue" && p.IsAlive &&
            (p.Kind == PieceKind.Tank || p.Kind == PieceKind.Infantry || p.Kind == PieceKind.Artillery));
        bool redGroundAssault = board.Pieces.Any(p => p.Side == "red" && p.IsAlive &&
            (p.Kind == PieceKind.Tank || p.Kind == PieceKind.Infantry || p.Kind == PieceKind.Artillery));

        if (!blueGroundAssault)
        {
            EndGame(true, "Toàn bộ lực lượng chủ lực (Xe tăng, Bộ binh, Pháo binh) đối phương đã bị tiêu diệt!");
            return;
        }
        if (!redGroundAssault)
        {
            EndGame(false, "Toàn bộ lực lượng chủ lực của bạn đã bị tiêu diệt!");
            return;
        }

        // Nếu đối thủ là máy/mô phỏng (chỉ khi bật chế độ Bot) và đến lượt Xanh
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

    /// <summary>
    /// Mô phỏng nước đi của đối phương tuân thủ nghiêm ngặt Luật Cờ Tư Lệnh
    /// </summary>
    private void SimulateOpponentMove()
    {
        var bluePieces = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).ToList();
        if (bluePieces.Count == 0) return;

        // Lấy tất cả các nước đi hợp lệ của tất cả quân xanh
        var allMovable = bluePieces
            .Select(p => new { Piece = p, Moves = board.GetValidMoves(p) })
            .Where(x => x.Moves.Count > 0)
            .ToList();

        if (allMovable.Count == 0)
        {
            EndGame(true, "Đối phương không còn nước đi hợp lệ!");
            return;
        }

        // Ưu tiên 1: Nước đi ăn được quân đỏ (đặc biệt là Tư lệnh hoặc vũ khí lớn)
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
            // Đi ngẫu nhiên một nước hợp lệ
            var pick = allMovable[Random.Shared.Next(allMovable.Count)];
            var dest = pick.Moves[Random.Shared.Next(pick.Moves.Count)];
            board.MovePiece(pick.Piece.PieceId, dest.X, dest.Y);
        }
    }

    private void UpdateTurnDisplay()
    {
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
        lblPlayerScore.Text = $"Lực lượng: {redScore}đ";
        lblOpponentScore.Text = $"Lực lượng: {blueScore}đ";
    }

    private void EndGame(bool isVictory, string reason)
    {
        gameTimer.Stop();
        int eloChange = isVictory ? 15 : -12;
        int coinReward = isVictory ? _room.BetAmount : -_room.BetAmount;

        SessionService.Instance.UpdateElo(eloChange, isVictory);
        SessionService.Instance.AddCoins(coinReward);

        string title = isVictory ? "CHIẾN THẮNG!" : "THẤT BẠI!";
        string msg = $"{reason}\n\nKết quả: {(isVictory ? "Bạn đã thắng!" : "Bạn đã thua!")}\n" +
                     $"Biến động ELO: {(eloChange >= 0 ? "+" : "")}{eloChange}\n" +
                     $"Biến động Xu: {(coinReward >= 0 ? "+" : "")}{coinReward:N0} Xu";

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
        txtChat.Clear();
    }

    private void btnDraw_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có muốn gửi lời xin hoà tới đối thủ?"))
        {
            lstChat.Items.Add("[Hệ thống] Bạn đã gửi lời xin hoà...");
            ShowInfo("Đối thủ đã đồng ý hoà trận!");
            EndGame(false, "Hai bên đồng ý kết quả Hoà.");
        }
    }

    private void btnSurrender_Click(object? sender, EventArgs e)
    {
        if (Confirm("Bạn có chắc chắn muốn ĐẦU HÀNG ván đấu này?"))
        {
            EndGame(false, "Bạn đã đầu hàng ván đấu.");
        }
    }

    private void btnLeave_Click(object? sender, EventArgs e)
    {
        if (Confirm("Thoát ván đấu giữa chừng sẽ bị xử thua. Bạn có chắc chắn?"))
        {
            EndGame(false, "Bạn đã rời khỏi trận đấu.");
        }
    }
}
