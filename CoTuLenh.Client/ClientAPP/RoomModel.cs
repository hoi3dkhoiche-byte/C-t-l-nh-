namespace ClientAPP;

public class RoomModel
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = "";
    public int HostUserId { get; set; }
    public string HostUsername { get; set; } = "Chủ phòng";
    public int? GuestUserId { get; set; }
    public string? GuestUsername { get; set; }
    public string Status { get; set; } = "waiting"; // "waiting", "playing", "finished"
    public int BetAmount { get; set; } = 1000;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFull => GuestUserId.HasValue;
    public int PlayerCount => IsFull ? 2 : 1;
}
