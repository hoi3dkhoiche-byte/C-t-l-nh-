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
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                // Gửi JWT để server biết socket này là user nào
                Auth = new { token = SessionService.Instance.Token },
                // Nối thẳng websocket: bước nâng cấp từ polling của thư viện không khớp server Python
                Transport = SocketIOClient.Common.TransportProtocol.WebSocket
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
                    // SocketIOClient 4.x: ToString() không ra JSON, phải lấy phần tử dữ liệu đầu tiên
                    string rawJson = response.GetValue<JsonElement>(0).GetRawText();
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

            RegisterRoomAndChatEvents(_socket);

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

    // =====================================================================
    // PHÒNG + CHAT
    // =====================================================================

    public event Action<ChatMessageModel>? OnChatMessage;   // có tin chat mới (kể cả tin của mình)
    public event Action<string>? OnChatRejected;            // tin bị từ chối, kèm lý do
    public event Action<string>? OnRoomNotice;              // thông báo về phòng

    // Handler chạy trên luồng socket, form nhận event phải Invoke về luồng UI
    private void RegisterRoomAndChatEvents(SocketIO socket)
    {
        socket.On("chat_message", async response =>
        {
            try
            {
                var msg = response.GetValue<ChatMessageModel>(0);
                if (msg != null) OnChatMessage?.Invoke(msg);
            }
            catch
            {
                // Bỏ qua gói tin sai định dạng
            }
            await Task.CompletedTask;
        });

        socket.On("chat_rejected", async response =>
        {
            string reason = ReadString(response, "reason");
            OnChatRejected?.Invoke(reason switch
            {
                "NOT_IN_ROOM" => "Bạn chưa vào phòng nên chưa chat được",
                "EMPTY" => "Tin nhắn trống",
                "TOO_LONG" => "Tin nhắn quá dài (tối đa 200 ký tự)",
                "TOO_FAST" => "Bạn gửi nhanh quá, đợi 1 giây",
                _ => "Không gửi được tin nhắn",
            });
            await Task.CompletedTask;
        });

        socket.On("room_joined", async response =>
        {
            string color = ReadString(response, "your_color") == "red" ? "Đỏ" : "Xanh";
            OnRoomNotice?.Invoke($"Đã kết nối phòng {ReadString(response, "room_code")}, bạn cầm phe {color}");
            await Task.CompletedTask;
        });

        socket.On("join_pending", async response =>
        {
            OnRoomNotice?.Invoke("Đang chờ chủ phòng duyệt...");
            await Task.CompletedTask;
        });

        socket.On("player_joined", async response =>
        {
            OnRoomNotice?.Invoke("Đối thủ đã vào phòng");
            await Task.CompletedTask;
        });

        socket.On("player_left", async response =>
        {
            OnRoomNotice?.Invoke("Đối thủ đã rời phòng");
            await Task.CompletedTask;
        });

        socket.On("error", async response =>
        {
            OnRoomNotice?.Invoke("Lỗi: " + ReadString(response, "message"));
            await Task.CompletedTask;
        });
    }

    // Đọc một trường chuỗi trong dữ liệu event, không có thì trả ""
    private static string ReadString(IEventContext response, string name)
    {
        try
        {
            var root = response.GetValue<JsonElement>(0);
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value))
            {
                return value.ToString();
            }
        }
        catch
        {
            // Event không có dữ liệu
        }
        return "";
    }

    public async Task<bool> JoinRoomAsync(string roomCode)
    {
        // Kết nối lúc đăng nhập có thể chưa xong hoặc đã rớt
        if (_socket == null || !_socket.Connected)
        {
            await ConnectAsync();
        }
        if (_socket == null || !_socket.Connected) return false;

        await _socket.EmitAsync("join_room", new object[] { new { room_code = roomCode } });
        return true;
    }

    // Đang trong trận thì server tính là bỏ ván
    public async Task LeaveRoomAsync()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.EmitAsync("leave_room");
        }
    }

    // Không tự hiện tin, đợi server phát lại qua "chat_message"
    public async Task<bool> SendChatAsync(string message)
    {
        if (_socket == null || !_socket.Connected) return false;

        await _socket.EmitAsync("chat", new object[] { new { message } });
        return true;
    }
}