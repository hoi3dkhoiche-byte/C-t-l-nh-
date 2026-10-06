using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace ClientAPP;

public class GameApiService
{
    private static readonly Lazy<GameApiService> _instance = new(() => new GameApiService());
    public static GameApiService Instance => _instance.Value;

    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private void SetAuthHeader()
{
    var token = SessionService.Instance.Token;

    _client.DefaultRequestHeaders.Authorization =
        string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
}

    private GameApiService()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000/api/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<List<RoomModel>> GetRoomsAsync()
    {
        try
        {
            SetAuthHeader();
            var res = await _client.GetAsync("rooms");
            
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("rooms", out var roomsElement))
                {
                    var rooms = roomsElement.Deserialize<List<RoomModel>>(jsonOptions);
                    if (rooms != null) return rooms;
                }
            }
        }
        catch
        {
            // Dự phòng nếu server chưa bật endpoint
        }

        // Mock data nếu offline
        return new List<RoomModel>
        {
            new() { Id = 1, RoomCode = "CTL-101", HostUsername = "TuLenhBinhDoan1", BetAmount = 1000, Status = "waiting" },
            new() { Id = 2, RoomCode = "CTL-204", HostUsername = "ThietGiapVang", GuestUsername = "ThuyQuanLucChien", BetAmount = 5000, Status = "playing" },
            new() { Id = 3, RoomCode = "CTL-308", HostUsername = "PhapThuatPhuongDong", BetAmount = 2000, Status = "waiting" },
            new() { Id = 4, RoomCode = "CTL-512", HostUsername = "KhongQuanHuyenThoai", BetAmount = 10000, Status = "waiting" }
        };
    }

    public async Task<RoomModel?> CreateRoomAsync(int betAmount)
    {
        try
        {
            var payload = new { bet_amount = betAmount };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            SetAuthHeader();
            var res = await _client.PostAsync("rooms", content);

            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("room", out var roomElement))
                {
                    return roomElement.Deserialize<RoomModel>(jsonOptions);
                }
            }
        }
        catch
        {
            // Mock room
        }

        var randomCode = "CTL-" + new Random().Next(100, 999);
        var host = SessionService.Instance.CurrentUser?.Username ?? "Tôi";
        return new RoomModel
        {
            Id = new Random().Next(10, 999),
            RoomCode = randomCode,
            HostUsername = host,
            BetAmount = betAmount,
            Status = "waiting"
        };
    }

    public async Task<List<UserModel>> GetLeaderboardAsync()
    {
        try
        {
            var res = await _client.GetAsync("leaderboard");
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<UserModel>>(body, jsonOptions);
                if (list != null) return list;
            }
        }
        catch { }

        return new List<UserModel>
        {
            new() { Id = 1, Username = "DaiTuLenh_VN", EloRating = 1850, Wins = 142, Losses = 28, Coins = 500000 },
            new() { Id = 2, Username = "ThietGiapChien", EloRating = 1720, Wins = 110, Losses = 35, Coins = 320000 },
            new() { Id = 3, Username = "SaoDo_Hanoi", EloRating = 1680, Wins = 98, Losses = 41, Coins = 240000 },
            new() { Id = 4, Username = "PhuongHoangBay", EloRating = 1550, Wins = 85, Losses = 52, Coins = 150000 },
            new() { Id = 5, Username = "ChienBinhRungSau", EloRating = 1490, Wins = 70, Losses = 45, Coins = 110000 },
            new() { Id = 6, Username = "ThuyThuBienDong", EloRating = 1420, Wins = 62, Losses = 50, Coins = 85000 }
        };
    }

    public async Task<List<MatchModel>> GetMatchHistoryAsync()
    {
        try
        {
            var res = await _client.GetAsync("matches/history");
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<MatchModel>>(body, jsonOptions);
                if (list != null) return list;
            }
        }
        catch { }

        return new List<MatchModel>
        {
            new() { Id = 1, RoomCode = "CTL-881", OpponentName = "ThietGiapChien", Side = "red", Result = "Thắng", EloChange = 18, CoinChange = 2000, PlayedAt = DateTime.Now.AddHours(-2) },
            new() { Id = 2, RoomCode = "CTL-762", OpponentName = "DaiTuLenh_VN", Side = "blue", Result = "Thua", EloChange = -15, CoinChange = -5000, PlayedAt = DateTime.Now.AddHours(-5) },
            new() { Id = 3, RoomCode = "CTL-604", OpponentName = "SaoDo_Hanoi", Side = "red", Result = "Thắng", EloChange = 14, CoinChange = 1000, PlayedAt = DateTime.Now.AddDays(-1) },
            new() { Id = 4, RoomCode = "CTL-501", OpponentName = "ThuyThuBienDong", Side = "blue", Result = "Hòa", EloChange = 2, CoinChange = 0, PlayedAt = DateTime.Now.AddDays(-2) }
        };
    }

    public async Task<bool> DepositCoinsAsync(int amount, string method)
    {
        try
        {
            var payload = new { amount, method };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _client.PostAsync("payment/deposit", content);
            if (res.IsSuccessStatusCode)
            {
                SessionService.Instance.AddCoins(amount);
                return true;
            }
        }
        catch { }

        // Mô phỏng nạp tiền thành công
        SessionService.Instance.AddCoins(amount);
        return true;
    }
}
