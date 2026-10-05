using System.Windows.Forms;

namespace ClientAPP;

public partial class GamePlayForm : BaseForm
{
    private readonly RoomModel _room;
    private int moveCount = 1;
    private int currentTurnSeconds = 45;

    public GamePlayForm(RoomModel room)
    {
        _room = room;
        InitializeComponent();
        this.Load += GamePlayForm_Load;
        this.FormClosed += GamePlayForm_FormClosed;

        board.OnPieceMoved += Board_OnPieceMoved;

        gameTimer.Interval = 1000;
        gameTimer.Tick += GameTimer_Tick;
    }

    private void GamePlayForm_Load(object? sender, EventArgs e)
    {
        var user = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        lblPlayerName.Text = $"🔴 {user} (Đỏ)";
        lblOpponentName.Text = $"🔵 {_room.GuestUsername ?? "Đối thủ"} (Xanh)";

        lstChat.Items.Add("[Hệ thống] Trận đấu đã bắt đầu!");
        lstChat.Items.Add("[Hệ thống] Lượt đầu tiên: PHE ĐỎ");

        UpdateTurnDisplay();
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
            // Hết giờ -> tự động đổi lượt
            board.CurrentTurn = board.CurrentTurn == "red" ? "blue" : "red";
            UpdateTurnDisplay();
            lstChat.Items.Add("[Hệ thống] Hết thời gian lượt đi!");
        }
    }

    private void Board_OnPieceMoved(PieceView piece, int toX, int toY)
    {
        string log = $"{moveCount++}. {piece.DisplayName} ({piece.Side}) ➔ ({toX}, {toY})";
        lstMoveLog.Items.Add(log);
        lstMoveLog.TopIndex = lstMoveLog.Items.Count - 1;

        currentTurnSeconds = 45;
        UpdateTurnDisplay();

        // Kiểm tra điều kiện thắng (Nếu ăn được Tư lệnh đối phương)
        var blueCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "b_cmd");
        if (blueCmd != null && !blueCmd.IsAlive)
        {
            EndGame(true, "Ăn được Tư Lệnh đối phương!");
            return;
        }

        var redCmd = board.Pieces.FirstOrDefault(p => p.PieceId == "r_cmd");
        if (redCmd != null && !redCmd.IsAlive)
        {
            EndGame(false, "Tư Lệnh phe bạn đã bị tiêu diệt!");
            return;
        }

        // Nếu đối thủ là bot/mô phỏng, tự động đi sau 1 giây
        if (board.CurrentTurn == "blue")
        {
            Task.Delay(1200).ContinueWith(_ =>
            {
                if (this.IsDisposed) return;
                this.Invoke(() =>
                {
                    SimulateOpponentMove();
                });
            });
        }
    }

    private void SimulateOpponentMove()
    {
        var bluePieces = board.Pieces.Where(p => p.Side == "blue" && p.IsAlive).ToList();
        if (bluePieces.Count == 0) return;

        var rnd = new Random();
        var piece = bluePieces[rnd.Next(bluePieces.Count)];
        int newY = Math.Min(BoardControl.ROWS - 1, piece.Y + 1); // Đi về phía trước 1 ô

        board.MovePiece(piece.PieceId, piece.X, newY);
        string log = $"{moveCount++}. {piece.DisplayName} (blue) ➔ ({piece.X}, {newY})";
        lstMoveLog.Items.Add(log);
        lstMoveLog.TopIndex = lstMoveLog.Items.Count - 1;

        currentTurnSeconds = 45;
        UpdateTurnDisplay();
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
            // Giả lập đối thủ phản hồi
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
