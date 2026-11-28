using System.Text.Json.Serialization;

namespace ClientAPP;

public class RoomModel
{
    public int Id { get; set; }

    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";

    [JsonPropertyName("host_user_id")]
    public int HostUserId { get; set; }

    [JsonPropertyName("host_reputation")]
    public int HostReputation { get; set; } = 10;

    [JsonPropertyName("host_username")]
    public string HostUsername { get; set; } = "Chủ phòng";

    [JsonPropertyName("guest_user_id")]
    public int? GuestUserId { get; set; }

    [JsonPropertyName("guest_username")]
    public string? GuestUsername { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "waiting";

    [JsonPropertyName("bet_amount")]
    public int BetAmount { get; set; } = 1000;

    public bool IsOfflinePractice { get; set; } = false;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFull => GuestUserId.HasValue;
    public int PlayerCount => IsFull ? 2 : 1;
}
