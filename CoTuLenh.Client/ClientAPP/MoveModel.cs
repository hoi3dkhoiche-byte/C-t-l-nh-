namespace ClientAPP;

public class MoveModel
{
    public int MoveNumber { get; set; }
    public string PieceId { get; set; } = "";
    public string PieceName { get; set; } = "";
    public string Side { get; set; } = "red"; // "red" or "blue"
    public int FromX { get; set; }
    public int FromY { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }
    public string? CapturedPieceName { get; set; }
    public string Notation { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class MatchModel
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = "";
    public string OpponentName { get; set; } = "";
    public string Side { get; set; } = "red";
    public string Result { get; set; } = "Thắng"; // "Thắng", "Thua", "Hòa"
    public int EloChange { get; set; } = 15;
    public int CoinChange { get; set; } = 1000;
    public DateTime PlayedAt { get; set; } = DateTime.Now;
    public List<MoveModel> Moves { get; set; } = new();
}
