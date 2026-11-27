using System.Windows.Forms;

namespace ClientAPP;

public partial class GamePlayForm : BaseForm
{
    private readonly RoomModel _room;
    private int moveCount = 1;
    private int currentTurnSeconds = 45;
    private int totalMatchSeconds = 30 * 60; // Chế độ Tấn công tổng lực: 30 phút (1800 giây)
    private bool isBotEnabled = false; // Mặc định tắt để bạn tự do thử nghiệm điều khiển cả 2 bên Đỏ và Xanh
    private Button? btnToggleBot;
    private Label? lblMatchTimer;

    public GamePlayForm(RoomModel room)
    {
        _room = room;
        InitializeComponent();
        this.Load += GamePlayForm_Load;
        this.FormClosed += GamePlayForm_FormClosed;

        board.OnPieceMoved += Board_OnPieceMoved;
        board.OnFreeAntiAirStrike += Board_OnFreeAntiAirStrike;
        board.OnFireRingInspected += Board_OnFireRingInspected;

        gameTimer.Interval = 1000;
        gameTimer.Tick += GameTimer_Tick;
    }

    private void GamePlayForm_Load(object? sender, EventArgs e)
    {
        var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lblPlayerName.Text = $"{user} (Đỏ)";
        lblOpponentName.Text = $"{_room.GuestUsername ?? "Đối thủ"} (Xanh)";

        // Đồng hồ tổng trận đấu 30 phút (Chế độ Tấn công tổng lực)
        lblMatchTimer = new Label
        {
            Text = "TỔNG LỰC: 30:00",
            Location = new Point(10, 568),
            Size = new Size(240, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(44, 62, 80),
            ForeColor = Color.FromArgb(241, 196, 15),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        pnlRight.Controls.Add(lblMatchTimer);

        // Nút chuyển đổi chế độ Bot / Tự cầm 2 bên
        btnToggleBot = new Button
        {
            Text = isBotEnabled ? "Bot: ĐANG BẬT" : "Chế độ: TỰ CẦM 2 BÊN",
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
            btnToggleBot.Text = isBotEnabled ? "Bot: ĐANG BẬT" : "Chế độ: TỰ CẦM 2 BÊN";
            btnToggleBot.BackColor = isBotEnabled ? Color.FromArgb(39, 174, 96) : Color.FromArgb(41, 128, 185);
            lstChat.Items.Add(isBotEnabled ? "[Hệ thống] Đã BẬT Bot tự đánh cho phe Xanh." : "[Hệ thống] Đã TẮT Bot (Bạn tự điều khiển cả 2 bên).");
            if (isBotEnabled && board.CurrentTurn == "blue")
            {
                SimulateOpponentMove();
            }
        };
        pnlRight.Controls.Add(btnToggleBot);

        lstChat.Items.Add("[Hệ thống] Trận đấu Cờ Tư Lệnh bắt đầu! (Chế độ: TẤN CÔNG TỔNG LỰC - 30 phút)");
        lstChat.Items.Add("[Hệ thống] Điều kiện thắng: Diệt Tư lệnh đối phương (+50đ) hoặc hơn tổng điểm sau 30 phút.");
        lstChat.Items.Add("[Hệ thống] Lượt đầu tiên: PHE ĐỎ");

        UpdateTurnDisplay();
        UpdateScores();
        gameTimer.Start();
    }

    private void GamePlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        gameTimer.Stop();
        new LobbyForm().Show();
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (board.IsAnimating) return;

        // 1. Đếm ngược đồng hồ tổng 30 phút (Tấn công tổng lực)
        totalMatchSeconds--;
        if (lblMatchTimer != null)
        {
            int mins = Math.Max(0, totalMatchSeconds) / 60;
            int secs = Math.Max(0, totalMatchSeconds) % 60;
            lblMatchTimer.Text = $"TỔNG LỰC: {mins:D2}:{secs:D2}";
        }

        if (totalMatchSeconds <= 0)
        {
            int redScore = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score);
            int blueScore = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score);
            if (redScore > blueScore)
            {
                EndGame(true, $"Hết 30 phút Tấn công tổng lực! Phe Đỏ thắng điểm ({redScore}đ vs {blueScore}đ).");
            }
            else if (blueScore > redScore)
            {
                EndGame(false, $"Hết 30 phút Tấn công tổng lực! Phe Xanh thắng điểm ({blueScore}đ vs {redScore}đ).");
            }
            else
            {
                EndGame(true, $"Hết 30 phút Tấn công tổng lực! Hai bên hòa điểm ({redScore}đ - {blueScore}đ).");
            }
            return;
        }

        // 2. Đếm ngược 45 giây mỗi lượt
        currentTurnSeconds--;
        string timeStr = $"00:{currentTurnSeconds:D2}";

        if (board.CurrentTurn == "red")
        {
            lblPlayerTimer.Text = timeStr;
            lblOpponentTimer.Text = "00:45";
        }
        else
        {
            lblOpponentTimer.Text = timeStr;
            lblPlayerTimer.Text = "00:45";
        }

        if (currentTurnSeconds <= 0)
        {
            currentTurnSeconds = 45;
            // Hết giờ -> Đổi lượt
            board.CurrentTurn = board.CurrentTurn == "red" ? "blue" : "red";
            UpdateTurnDisplay();
            lstChat.Items.Add("[Hệ thống] Hết thời gian lượt đi!");
        }
    }

    private void Board_OnFireRingInspected(PieceView? piece)
    {
        if (piece != null)
        {
            string sideStr = piece.Side == "red" ? "Phe Đỏ" : "Phe Xanh";
            float radius = BoardControl.GetFireRingRadius(piece.Kind);
            lstChat.Items.Add($"[Trinh sát] Đang soi vành đai lửa của {piece.DisplayName} ({sideStr}) - Bán kính {radius} nấc.");
        }
        else
        {
            lstChat.Items.Add("[Trinh sát] Đã tắt soi vành đai lửa.");
        }
        lstChat.TopIndex = lstChat.Items.Count - 1;
    }

    private void Board_OnFreeAntiAirStrike(PieceView piece, int toX, int toY, PieceView? capturedPiece)
    {
        string capInfo = capturedPiece != null ? $" [Ăn: {capturedPiece.DisplayName}]" : "";
        lstMoveLog.Items.Add($"{moveCount++}. {piece.DisplayName} ({piece.Side}) Phòng không (Không tốn lượt) -> ({toX}, {toY}){capInfo}");
        lstMoveLog.TopIndex = lstMoveLog.Items.Count - 1;

        UpdateScores();

        // Kiểm tra nếu Tư lệnh đang ngồi trên Không quân bị bắn hạ
        var blueCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "b_cmd");
        if (blueCmd != null && !blueCmd.IsAlive)
        {
            int redTotal = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score) + 50;
            int blueTotal = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score);
            EndGame(true, $"Tư Lệnh đối phương đã bị tiêu diệt! (+50đ chiến thuật)\nTổng điểm: Đỏ {redTotal}đ - Xanh {blueTotal}đ");
            return;
        }

        var redCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "r_cmd");
        if (redCmd != null && !redCmd.IsAlive)
        {
            int redTotal = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score);
            int blueTotal = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score) + 50;
            EndGame(false, $"Tư Lệnh phe bạn đã bị tiêu diệt! (+50đ chiến thuật cho Xanh)\nTổng điểm: Đỏ {redTotal}đ - Xanh {blueTotal}đ");
            return;
        }

        if (isBotEnabled && board.CurrentTurn == "blue")
        {
            Task.Delay(700).ContinueWith(_ =>
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

    private void Board_OnPieceMoved(PieceView piece, int toX, int toY, PieceView? capturedPiece)
    {
        string capInfo = capturedPiece != null ? $" [Ăn: {capturedPiece.DisplayName}]" : "";
        string actionType;
        if (!piece.IsAlive && piece.Kind == PieceKind.AirForce)
        {
            actionType = capturedPiece != null ? " Cảm tử -> " : " Bị phòng không bắn hạ tại ";
        }
        else if (piece.X == toX && piece.Y == toY)
        {
            actionType = " -> ";
        }
        else
        {
            actionType = piece.Kind switch
            {
                PieceKind.AirForce => " Ném bom -> ",
                PieceKind.Warship => " Xạ kích -> ",
                _ => " Bắn tại chỗ -> "
            };
        }

        var stackedAtDest = board.GetPiecesAt(toX, toY);
        string log;
        if (capturedPiece == null && stackedAtDest.Count > 1)
        {
            log = $"{moveCount++}. {piece.DisplayName} ({piece.Side}) Ghép quân -> ({toX}, {toY}) [{stackedAtDest.Count} quân]";
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

        // =========================================================================
        // KIỂM TRA ĐIỀU KIỆN KẾT THÚC VÁN CỜ - CHẾ ĐỘ TẤN CÔNG TỔNG LỰC (30 PHÚT):
        // Theo Rule.pdf (Mục V.B): Ván cờ chỉ dừng ngay khi một bên mất trước quân Tư lệnh
        // (thưởng +50 điểm chiến thuật). Nếu hết 30 phút thì tính tổng điểm lực lượng.
        // =========================================================================
        var blueCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "b_cmd");
        if (blueCmd != null && !blueCmd.IsAlive)
        {
            int redTotal = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score) + 50;
            int blueTotal = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score);
            EndGame(true, $"Tư Lệnh đối phương đã bị tiêu diệt! (+50đ chiến thuật)\nTổng điểm: Đỏ {redTotal}đ - Xanh {blueTotal}đ");
            return;
        }

        var redCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "r_cmd");
        if (redCmd != null && !redCmd.IsAlive)
        {
            int redTotal = board.Pieces.Where(p => p.Side == "red" && p.IsAlive).Sum(p => p.Score);
            int blueTotal = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).Sum(p => p.Score) + 50;
            EndGame(false, $"Tư Lệnh phe bạn đã bị tiêu diệt! (+50đ chiến thuật cho Xanh)\nTổng điểm: Đỏ {redTotal}đ - Xanh {blueTotal}đ");
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
            board.MovePiece(bestCap.Piece.PieceId, bestCap.Dest.X, bestCap.Dest.Y, CaptureMode.StandStill);
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
            lblTurn.Text = "LƯỢT ĐI:\nPHE ĐỎ (BẠN)";
            lblTurn.ForeColor = Color.FromArgb(192, 57, 43);
        }
        else
        {
            lblTurn.Text = "LƯỢT ĐI:\nPHE XANH (ĐỐI THỦ)";
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
