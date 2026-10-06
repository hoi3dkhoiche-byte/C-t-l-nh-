using System.Text.Json.Serialization;

namespace ClientAPP;

public class RoomModel
{
    public int Id { get; set; }

    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";

    [JsonPropertyName("host_user_id")]
    public int HostUserId { get; set; }

    public string HostUsername { get; set; } = "Chủ phòng";
    public int? GuestUserId { get; set; }
    public string? GuestUsername { get; set; }
    public string Status { get; set; } = "waiting";
    public int BetAmount { get; set; } = 1000;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFull => GuestUserId.HasValue;
    public int PlayerCount => IsFull ? 2 : 1;
}