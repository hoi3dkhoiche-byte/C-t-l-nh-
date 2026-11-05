using System.Text.Json;
using SocketIOClient;

namespace ClientAPP;

public sealed class RealtimeService
{
    private static readonly Lazy<RealtimeService> _instance = new(() => new RealtimeService());
    public static RealtimeService Instance => _instance.Value;

    private SocketIO? _socket;
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool IsConnected => _socket?.Connected ?? false;

    public event Action<List<UserModel>>? OnLeaderboardUpdated;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    private RealtimeService() { }

    public async Task ConnectAsync(string serverUrl = "http://localhost:5000")
    {
        if (_socket != null && _socket.Connected) return;

        try
        {
            _socket = new SocketIO(new Uri(serverUrl), new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 10,
                ConnectionTimeout = TimeSpan.FromSeconds(5)
            });

            _socket.OnConnected += (sender, e) =>
            {
                OnConnected?.Invoke();
            };

            _socket.OnDisconnected += (sender, e) =>
            {
                OnDisconnected?.Invoke();
            };

            // Lắng nghe sự kiện cập nhật bảng xếp hạng Real-time từ Server
            _socket.On("leaderboard_updated", async response =>
            {
                try
                {
                    string rawJson = response.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(rawJson)) return;

                    using var doc = JsonDocument.Parse(rawJson);
                    var root = doc.RootElement;

                    JsonElement arrayElem = root;
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var first = root[0];
                        if (first.TryGetProperty("leaderboard", out var innerArr))
                        {
                            arrayElem = innerArr;
                        }
                        else
                        {
                            arrayElem = first;
                        }
                    }
                    else if (root.TryGetProperty("leaderboard", out var inner))
                    {
                        arrayElem = inner;
                    }

                    var list = arrayElem.Deserialize<List<UserModel>>(jsonOptions);
                    if (list != null)
                    {
                        OnLeaderboardUpdated?.Invoke(list);

                        // Cập nhật lại thông tin user hiện tại nếu có thay đổi ELO
                        var me = SessionService.Instance.CurrentUser;
                        if (me != null)
                        {
                            var myUpdated = list.FirstOrDefault(u => u.Id == me.Id || u.Username == me.Username);
                            if (myUpdated != null)
                            {
                                me.EloRating = myUpdated.EloRating;
                                me.Wins = myUpdated.Wins;
                                me.Losses = myUpdated.Losses;
                                me.Draws = myUpdated.Draws;
                                me.Coins = myUpdated.Coins;
                                SessionService.Instance.SetCurrentUser(me, SessionService.Instance.Token);
                            }
                        }
                    }
                }
                catch
                {
                    // Bỏ qua lỗi parse
                }
                await Task.CompletedTask;
            });

            await _socket.ConnectAsync();
        }
        catch
        {
            // Kết nối socket thất bại (server chưa bật hoặc offline)
        }
    }

    public async Task RequestLeaderboardAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("request_leaderboard");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.DisconnectAsync();
        }
    }
}
