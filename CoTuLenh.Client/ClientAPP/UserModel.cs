using System.Text.Json.Serialization;

namespace ClientAPP;

public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public int Coins { get; set; } = 0;

    [JsonPropertyName("elo_rating")]
    public int EloRating { get; set; } = 1200;

    public string? AvatarUrl { get; set; }
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int Draws { get; set; } = 0;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int TotalGames => Wins + Losses + Draws;

    [JsonPropertyName("win_rate")]
    public double WinRate
    {
        get => TotalGames == 0 ? 0 : Math.Round((double)Wins / TotalGames * 100, 1);
        set { }
    }
}
