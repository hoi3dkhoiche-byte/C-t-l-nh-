using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientAPP;

public class GameApiService
{
    private static readonly Lazy<GameApiService> _instance = new(() => new GameApiService());
    public static GameApiService Instance => _instance.Value;

    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConcurrentDictionary<int, string> _usernameCache = new();

    public string? LastError { get; private set; }

    private GameApiService()
    {
        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
    }

    private static Uri Endpoint(string relativePath) => new(ServerConfig.ApiBaseUri, relativePath);

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, HttpContent? content = null)
    {
        var req = new HttpRequestMessage(method, Endpoint(relativePath));
        var token = SessionService.Instance.Token;
        if (!string.IsNullOrWhiteSpace(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        if (content != null)
        {
            req.Content = content;
        }
        return req;
    }

    public void CacheUser(int userId, string? username)
    {
        if (userId > 0 && !string.IsNullOrWhiteSpace(username))
        {
            _usernameCache[userId] = username;
        }
    }

    public string ResolveCachedUsername(int? userId, string fallbackPrefix = "Kỳ thủ")
    {
        if (!userId.HasValue || userId.Value <= 0) return fallbackPrefix;

        var me = SessionService.Instance.CurrentUser;
        if (me != null && me.Id == userId.Value && !string.IsNullOrWhiteSpace(me.Username))
        {
            _usernameCache[userId.Value] = me.Username;
            return me.Username;
        }

        if (_usernameCache.TryGetValue(userId.Value, out var cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        return $"{fallbackPrefix} #{userId.Value}";
    }

    public async Task<string> ResolveUsernameAsync(int? userId, string fallbackPrefix = "Kỳ thủ")
    {
        if (!userId.HasValue || userId.Value <= 0) return fallbackPrefix;
        if (_usernameCache.TryGetValue(userId.Value, out var cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        await WarmupUsersCacheAsync();
        return ResolveCachedUsername(userId, fallbackPrefix);
    }

    public async Task WarmupUsersCacheAsync()
    {
        var me = SessionService.Instance.CurrentUser;
        if (me != null && me.Id > 0)
        {
            CacheUser(me.Id, me.Username);
        }

        try
        {
            using var req = CreateRequest(HttpMethod.Get, "leaderboard");
            using var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<UserModel>>(body, jsonOptions);
                if (list != null)
                {
                    foreach (var u in list)
                    {
                        CacheUser(u.Id, u.Username);
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua nếu chưa lấy được leaderboard
        }
    }

    public async Task<List<RoomModel>> GetRoomsAsync()
    {
        LastError = null;
        try
        {
            await WarmupUsersCacheAsync();
            using var req = CreateRequest(HttpMethod.Get, "rooms");
            using var res = await _client.SendAsync(req);

            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("rooms", out var roomsElement))
                {
                    var rooms = roomsElement.Deserialize<List<RoomModel>>(jsonOptions) ?? new List<RoomModel>();
                    foreach (var r in rooms)
                    {
                        if (string.IsNullOrWhiteSpace(r.HostUsername) || r.HostUsername == "Chủ phòng")
                        {
                            r.HostUsername = ResolveCachedUsername(r.HostUserId, "Chủ phòng");
                        }
                    }
                    return rooms;
                }
            }
            else
            {
                LastError = $"Máy chủ trả lỗi HTTP {(int)res.StatusCode} khi tải danh sách phòng.";
            }
        }
        catch (Exception ex)
        {
            LastError = $"Không kết nối được máy chủ ({ServerConfig.BaseUrl}): {ex.Message}";
        }

        return new List<RoomModel>();
    }

    public async Task<RoomModel?> CreateRoomAsync(int betAmount)
    {
        LastError = null;
        try
        {
            var payload = new { bet_amount = betAmount };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var req = CreateRequest(HttpMethod.Post, "rooms", content);
            using var res = await _client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("room", out var roomElement))
                {
                    var room = roomElement.Deserialize<RoomModel>(jsonOptions);
                    if (room != null)
                    {
                        room.BetAmount = betAmount;
                        room.HostUsername = SessionService.Instance.CurrentUser?.Username
                                            ?? ResolveCachedUsername(room.HostUserId, "Chủ phòng");
                        room.HostReputation = SessionService.Instance.CurrentUser?.Reputation ?? 10;
                        return room;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("error", out var errElem))
                    {
                        string msg = errElem.GetString() ?? "Không thể tạo phòng.";
                        if (doc.RootElement.TryGetProperty("banned_until", out var banElem) &&
                            banElem.ValueKind == JsonValueKind.String)
                        {
                            msg += $"\nThời hạn mở khóa: {banElem.GetString()}";
                        }
                        LastError = msg;
                        return null;
                    }
                }
                LastError = $"Tạo phòng thất bại (HTTP {(int)res.StatusCode}).";
                return null;
            }
        }
        catch (Exception ex)
        {
            LastError = $"Không kết nối được máy chủ ({ServerConfig.BaseUrl}): {ex.Message}";
        }

        return null;
    }

    public async Task<List<UserModel>> GetLeaderboardAsync()
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, "leaderboard");
            using var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<UserModel>>(body, jsonOptions);
                if (list != null)
                {
                    foreach (var u in list)
                    {
                        CacheUser(u.Id, u.Username);
                    }
                    if (list.Count > 0) return list;
                }
            }
        }
        catch { }

        return new List<UserModel>();
    }

    public async Task<bool> FinishMatchAsync(string roomCode, string result, int betAmount, int? opponentId = null)
    {
        try
        {
            var payload = new
            {
                room_code = roomCode,
                result = result,
                bet_amount = betAmount,
                opponent_id = opponentId
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var req = CreateRequest(HttpMethod.Post, "matches/finish", content);
            using var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("user", out var userElem))
                {
                    var updatedUser = userElem.Deserialize<UserModel>(jsonOptions);
                    if (updatedUser != null)
                    {
                        SessionService.Instance.SetCurrentUser(updatedUser, SessionService.Instance.Token);
                    }
                }
                return true;
            }
        }
        catch { }

        return false;
    }

    public async Task<List<MatchModel>> GetMatchHistoryAsync()
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, "matches/history");
            using var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<MatchModel>>(body, jsonOptions);
                if (list != null) return list;
            }
        }
        catch { }

        return new List<MatchModel>();
    }

    public async Task<bool> DepositCoinsAsync(int amount, string method)
    {
        try
        {
            var payload = new { amount, method };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var req = CreateRequest(HttpMethod.Post, "payment/deposit", content);
            using var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                SessionService.Instance.AddCoins(amount);
                return true;
            }
        }
        catch { }

        SessionService.Instance.AddCoins(amount);
        return true;
    }
}
