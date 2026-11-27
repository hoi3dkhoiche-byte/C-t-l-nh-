using System.Text.Json;
using System.Text.Json.Serialization;
using SocketIOClient;
using SocketIOClient.Common;

namespace ClientAPP;

#region Socket.IO Payload DTOs (Khớp 1-1 với cotulenh_server/main.py & game_engine.py)

public class ServerPlayersDto
{
    [JsonPropertyName("red")]
    public int? Red { get; set; }

    [JsonPropertyName("blue")]
    public int? Blue { get; set; }
}

public class ServerPieceDto
{
    [JsonPropertyName("piece_id")]
    public string PieceId { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("side")]
    public string Side { get; set; } = "red";

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("alive")]
    public bool Alive { get; set; } = true;

    [JsonPropertyName("hero")]
    public bool Hero { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }
}

public class ResyncGameDto
{
    [JsonPropertyName("turn")]
    public string Turn { get; set; } = "red";

    [JsonPropertyName("move_count")]
    public int MoveCount { get; set; }

    [JsonPropertyName("board")]
    public List<ServerPieceDto> Board { get; set; } = new();
}

public class RoomJoinedEvent
{
    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";

    [JsonPropertyName("players")]
    public ServerPlayersDto Players { get; set; } = new();

    [JsonPropertyName("your_color")]
    public string YourColor { get; set; } = "red";

    [JsonPropertyName("game")]
    public ResyncGameDto? Game { get; set; }
}

public class PlayerJoinedEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "blue";
}

public class PlayerLeftEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("room_closed")]
    public bool RoomClosed { get; set; }
}

public class JoinPendingEvent
{
    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";

    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 20;
}

public class JoinRequestEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("reputation")]
    public int Reputation { get; set; }

    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 20;
}

public class UserIdEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
}

public class GameStartedEvent
{
    [JsonPropertyName("room_code")]
    public string RoomCode { get; set; } = "";

    [JsonPropertyName("players")]
    public ServerPlayersDto Players { get; set; } = new();

    [JsonPropertyName("first_turn")]
    public string FirstTurn { get; set; } = "red";

    [JsonPropertyName("turn_seconds")]
    public int TurnSeconds { get; set; } = 45;

    [JsonPropertyName("board")]
    public List<ServerPieceDto> Board { get; set; } = new();
}

public class ServerMoveDetailDto
{
    [JsonPropertyName("no")]
    public int No { get; set; }

    [JsonPropertyName("by")]
    public string By { get; set; } = "";

    [JsonPropertyName("piece_id")]
    public string PieceId { get; set; } = "";

    [JsonPropertyName("from")]
    public List<int> From { get; set; } = new();

    [JsonPropertyName("to")]
    public List<int> To { get; set; } = new();

    [JsonPropertyName("captured")]
    public List<string> Captured { get; set; } = new();

    [JsonPropertyName("stand_shoot")]
    public bool StandShoot { get; set; }

    [JsonPropertyName("moved")]
    public List<string> Moved { get; set; } = new();

    [JsonPropertyName("attacker_destroyed")]
    public bool AttackerDestroyed { get; set; }
}

public class MoveMadeEvent
{
    [JsonPropertyName("move")]
    public ServerMoveDetailDto Move { get; set; } = new();

    [JsonPropertyName("next_turn")]
    public string? NextTurn { get; set; }
}

public class MoveRejectedEvent
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}

public class TurnTimeoutEvent
{
    [JsonPropertyName("skipped")]
    public string Skipped { get; set; } = "";

    [JsonPropertyName("next_turn")]
    public string NextTurn { get; set; } = "";
}

public class OpponentDisconnectedEvent
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 50;
}

public class EloChangePairDto
{
    [JsonPropertyName("red")]
    public int Red { get; set; }

    [JsonPropertyName("blue")]
    public int Blue { get; set; }
}

public class GameOverEvent
{
    [JsonPropertyName("result")]
    public string Result { get; set; } = "";

    [JsonPropertyName("winner_id")]
    public int? WinnerId { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";

    [JsonPropertyName("elo_change")]
    public EloChangePairDto? EloChange { get; set; }
}

public class SocketErrorEvent
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("banned_until")]
    public string? BannedUntil { get; set; }
}

#endregion

public sealed class RealtimeService
{
    private static readonly Lazy<RealtimeService> _instance = new(() => new RealtimeService());
    public static RealtimeService Instance => _instance.Value;

    private SocketIO? _socket;
    private string? _connectedBaseUrl;
    private string? _connectedToken;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsConnected => _socket?.Connected ?? false;

    /// <summary>
    /// Mã phòng hiện tại đang tham gia (dùng để tự động vào lại và khôi phục bàn cờ nếu rớt mạng tạm thời).
    /// </summary>
    public string? ActiveRoomCode { get; set; }

    // Sự kiện chung & Bảng xếp hạng
    public event Action<List<UserModel>>? OnLeaderboardUpdated;
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<SocketErrorEvent>? OnSocketError;

    // Sự kiện Chat trong phòng / trong trận (từ nhánh match-chat)
    public event Action<ChatMessageModel>? OnChatMessage;
    public event Action<string>? OnChatRejected;
    public event Action<string>? OnRoomNotice;

    // Sự kiện Phòng chờ (RoomForm)
    public event Action<RoomJoinedEvent>? OnRoomJoined;
    public event Action<PlayerJoinedEvent>? OnPlayerJoined;
    public event Action<PlayerLeftEvent>? OnPlayerLeft;
    public event Action<JoinPendingEvent>? OnJoinPending;
    public event Action<JoinRequestEvent>? OnJoinRequest;
    public event Action<UserIdEvent>? OnJoinRequestExpired;
    public event Action<UserIdEvent>? OnJoinRequestCancelled;
    public event Action<UserIdEvent>? OnPlayerReady;
    public event Action<GameStartedEvent>? OnGameStarted;

    // Sự kiện Trong trận đấu (GamePlayForm)
    public event Action<MoveMadeEvent>? OnMoveMade;
    public event Action<MoveRejectedEvent>? OnMoveRejected;
    public event Action<TurnTimeoutEvent>? OnTurnTimeout;
    public event Action<OpponentDisconnectedEvent>? OnOpponentDisconnected;
    public event Action<UserIdEvent>? OnPlayerReconnected;
    public event Action<GameOverEvent>? OnGameOver;

    private RealtimeService() { }

    public async Task<bool> ConnectAsync(bool forceReconnect = false)
    {
        string targetUrl = ServerConfig.BaseUrl;
        string? token = SessionService.Instance.Token;

        await _connectLock.WaitAsync();
        try
        {
            bool sameConfig = _socket != null &&
                              _socket.Connected &&
                              string.Equals(_connectedBaseUrl, targetUrl, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(_connectedToken, token, StringComparison.Ordinal);

            if (!forceReconnect && sameConfig)
            {
                return true;
            }

            if (_socket != null)
            {
                try
                {
                    if (_socket.Connected)
                    {
                        await _socket.DisconnectAsync();
                    }
                    _socket.Dispose();
                }
                catch { }
                _socket = null;
            }

            var options = new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 15,
                ReconnectionDelayMax = 4000,
                ConnectionTimeout = TimeSpan.FromSeconds(8),
                // Gửi JWT để server xác thực người dùng
                Auth = string.IsNullOrWhiteSpace(token)
                    ? null
                    : new Dictionary<string, string> { ["token"] = token },
                // Nối thẳng WebSocket để tương thích ổn định với Flask-SocketIO (simple-websocket)
                Transport = TransportProtocol.WebSocket
            };

            _socket = new SocketIO(new Uri(targetUrl), options);
            _connectedBaseUrl = targetUrl;
            _connectedToken = token;

            RegisterEventHandlers(_socket);

            await _socket.ConnectAsync();
            return _socket.Connected;
        }
        catch
        {
            return false;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private void RegisterEventHandlers(SocketIO socket)
    {
        socket.OnConnected += async (sender, e) =>
        {
            OnConnected?.Invoke();

            // Tự động vào lại phòng để Server resync bàn cờ nếu mất mạng tạm thời giữa ván
            string? roomToRejoin = ActiveRoomCode;
            if (!string.IsNullOrWhiteSpace(roomToRejoin))
            {
                try
                {
                    await socket.EmitAsync("join_room", [new { room_code = roomToRejoin }]);
                }
                catch { }
            }
        };

        socket.OnDisconnected += (sender, e) =>
        {
            OnDisconnected?.Invoke();
        };

        // --- CHAT EVENTS ---
        socket.On("chat_message", ctx =>
        {
            var msg = TryGetPayload<ChatMessageModel>(ctx);
            if (msg != null)
            {
                GameApiService.Instance.CacheUser(msg.UserId, msg.Username);
                OnChatMessage?.Invoke(msg);
            }
            return Task.CompletedTask;
        });

        socket.On("chat_rejected", ctx =>
        {
            string reason = ReadString(ctx, "reason");
            OnChatRejected?.Invoke(reason switch
            {
                "NOT_IN_ROOM" => "Bạn chưa vào phòng nên chưa chat được",
                "EMPTY" => "Tin nhắn trống",
                "TOO_LONG" => "Tin nhắn quá dài (tối đa 200 ký tự)",
                "TOO_FAST" => "Bạn gửi nhanh quá, đợi 1 giây",
                _ => "Không gửi được tin nhắn"
            });
            return Task.CompletedTask;
        });

        // --- ROOM & GAMEPLAY EVENTS ---
        socket.On("error", ctx =>
        {
            var err = TryGetPayload<SocketErrorEvent>(ctx);
            if (err != null)
            {
                OnSocketError?.Invoke(err);
                if (!string.IsNullOrWhiteSpace(err.Message))
                {
                    OnRoomNotice?.Invoke("Lỗi: " + err.Message);
                }
            }
            return Task.CompletedTask;
        });

        socket.On("room_joined", ctx =>
        {
            var ev = TryGetPayload<RoomJoinedEvent>(ctx);
            if (ev != null)
            {
                ActiveRoomCode = ev.RoomCode;
                OnRoomJoined?.Invoke(ev);
                string colorVi = ev.YourColor == "red" ? "Đỏ" : "Xanh";
                OnRoomNotice?.Invoke($"Đã kết nối phòng {ev.RoomCode}, bạn cầm phe {colorVi}");
            }
            return Task.CompletedTask;
        });

        socket.On("player_joined", ctx =>
        {
            var ev = TryGetPayload<PlayerJoinedEvent>(ctx);
            if (ev != null)
            {
                OnPlayerJoined?.Invoke(ev);
                OnRoomNotice?.Invoke("Đối thủ đã vào phòng");
            }
            return Task.CompletedTask;
        });

        socket.On("player_left", ctx =>
        {
            var ev = TryGetPayload<PlayerLeftEvent>(ctx);
            if (ev != null)
            {
                if (ev.RoomClosed) ActiveRoomCode = null;
                OnPlayerLeft?.Invoke(ev);
                OnRoomNotice?.Invoke("Đối thủ đã rời phòng");
            }
            return Task.CompletedTask;
        });

        socket.On("join_pending", ctx =>
        {
            var ev = TryGetPayload<JoinPendingEvent>(ctx);
            if (ev != null)
            {
                OnJoinPending?.Invoke(ev);
                OnRoomNotice?.Invoke("Đang chờ chủ phòng duyệt...");
            }
            return Task.CompletedTask;
        });

        socket.On("join_request", ctx =>
        {
            var ev = TryGetPayload<JoinRequestEvent>(ctx);
            if (ev != null)
            {
                GameApiService.Instance.CacheUser(ev.UserId, ev.Username);
                OnJoinRequest?.Invoke(ev);
            }
            return Task.CompletedTask;
        });

        socket.On("join_request_expired", ctx =>
        {
            var ev = TryGetPayload<UserIdEvent>(ctx);
            if (ev != null) OnJoinRequestExpired?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("join_request_cancelled", ctx =>
        {
            var ev = TryGetPayload<UserIdEvent>(ctx);
            if (ev != null) OnJoinRequestCancelled?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("player_ready", ctx =>
        {
            var ev = TryGetPayload<UserIdEvent>(ctx);
            if (ev != null) OnPlayerReady?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("game_started", ctx =>
        {
            var ev = TryGetPayload<GameStartedEvent>(ctx);
            if (ev != null)
            {
                ActiveRoomCode = ev.RoomCode;
                OnGameStarted?.Invoke(ev);
            }
            return Task.CompletedTask;
        });

        socket.On("move_made", ctx =>
        {
            var ev = TryGetPayload<MoveMadeEvent>(ctx);
            if (ev != null) OnMoveMade?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("move_rejected", ctx =>
        {
            var ev = TryGetPayload<MoveRejectedEvent>(ctx);
            if (ev != null) OnMoveRejected?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("turn_timeout", ctx =>
        {
            var ev = TryGetPayload<TurnTimeoutEvent>(ctx);
            if (ev != null) OnTurnTimeout?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("opponent_disconnected", ctx =>
        {
            var ev = TryGetPayload<OpponentDisconnectedEvent>(ctx);
            if (ev != null) OnOpponentDisconnected?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("player_reconnected", ctx =>
        {
            var ev = TryGetPayload<UserIdEvent>(ctx);
            if (ev != null) OnPlayerReconnected?.Invoke(ev);
            return Task.CompletedTask;
        });

        socket.On("game_over", ctx =>
        {
            var ev = TryGetPayload<GameOverEvent>(ctx);
            if (ev != null)
            {
                ActiveRoomCode = null;
                OnGameOver?.Invoke(ev);
            }
            return Task.CompletedTask;
        });

        socket.On("room_left", _ =>
        {
            ActiveRoomCode = null;
            return Task.CompletedTask;
        });

        socket.On("leaderboard_updated", ctx =>
        {
            try
            {
                var elem = TryGetJsonElement(ctx);
                if (elem.HasValue)
                {
                    JsonElement arrayElem = elem.Value;
                    if (arrayElem.ValueKind == JsonValueKind.Object &&
                        arrayElem.TryGetProperty("leaderboard", out var inner))
                    {
                        arrayElem = inner;
                    }

                    var list = arrayElem.Deserialize<List<UserModel>>(jsonOptions);
                    if (list != null)
                    {
                        foreach (var u in list)
                        {
                            GameApiService.Instance.CacheUser(u.Id, u.Username);
                        }
                        OnLeaderboardUpdated?.Invoke(list);

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
            }
            catch { }
            return Task.CompletedTask;
        });
    }

    private static string ReadString(IEventContext ctx, string name)
    {
        try
        {
            var elem = TryGetJsonElement(ctx);
            if (elem.HasValue && elem.Value.ValueKind == JsonValueKind.Object &&
                elem.Value.TryGetProperty(name, out var val))
            {
                return val.ToString();
            }
        }
        catch { }
        return "";
    }

    private static JsonElement? TryGetJsonElement(IEventContext ctx)
    {
        try
        {
            return ctx.GetValue<JsonElement>(0);
        }
        catch
        {
            try
            {
                string raw = ctx.RawText ?? "";
                if (string.IsNullOrWhiteSpace(raw)) return null;
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 1)
                {
                    return root[1].Clone();
                }
                return root.Clone();
            }
            catch
            {
                return null;
            }
        }
    }

    private static T? TryGetPayload<T>(IEventContext ctx)
    {
        try
        {
            var elem = TryGetJsonElement(ctx);
            if (!elem.HasValue) return default;
            return elem.Value.Deserialize<T>(jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        if (!await ConnectAsync()) return false;
        if (_socket == null || !_socket.Connected) return false;

        await _socket.EmitAsync("join_room", [new { room_code = roomCode }]);
        return true;
    }

    public async Task AnswerJoinAsync(bool accept)
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("answer_join", [new { accept }]);
        }
    }

    public async Task SetPlayerReadyAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("player_ready", [new { }]);
        }
    }

    public async Task MakeMoveAsync(
        string pieceId,
        int fromX,
        int fromY,
        int toX,
        int toY,
        CaptureMode captureMode,
        bool moveStack)
    {
        if (_socket != null && _socket.Connected)
        {
            string modeStr = captureMode == CaptureMode.StandStill ? "stand" : "occupy";
            await _socket.EmitAsync("make_move", [new
            {
                piece_id = pieceId,
                from_x = fromX,
                from_y = fromY,
                to_x = toX,
                to_y = toY,
                capture_mode = modeStr,
                move_stack = moveStack
            }]);
        }
    }

    public async Task<bool> SendChatAsync(string message)
    {
        if (_socket == null || !_socket.Connected) return false;

        await _socket.EmitAsync("chat", [new { message }]);
        return true;
    }

    public async Task LeaveRoomAsync(string? roomCode = null)
    {
        string? code = roomCode ?? ActiveRoomCode;
        ActiveRoomCode = null;
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("leave_room", [new { room_code = code }]);
        }
    }

    public async Task RequestLeaderboardAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("request_leaderboard", []);
        }
    }

    public async Task DisconnectAsync()
    {
        ActiveRoomCode = null;
        if (_socket != null && _socket.Connected)
        {
            await _socket.DisconnectAsync();
        }
    }
}
