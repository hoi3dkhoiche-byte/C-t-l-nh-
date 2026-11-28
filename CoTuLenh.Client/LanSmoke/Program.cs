using System.Text;
using System.Text.Json;
using ClientAPP;

internal static class Program
{
    private static readonly object OutputLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [STAThread]
    private static int Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = new UTF8Encoding(false);
        var realtime = RealtimeService.Instance;
        realtime.OnGameEvent += (name, data) => Write(new { @event = name, data });
        realtime.OnConnected += () => Write(new { @event = "connected", data = new { } });
        realtime.OnDisconnected += () => Write(new { @event = "disconnected", data = new { } });
        realtime.OnLeaderboardUpdated += data => Write(new { @event = "leaderboard_updated", data });

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            int id = 0;
            try
            {
                using var document = JsonDocument.Parse(line);
                var command = document.RootElement;
                id = command.GetProperty("id").GetInt32();
                var name = command.GetProperty("command").GetString();
                var result = ExecuteAsync(name, command).GetAwaiter().GetResult();
                Write(new { id, ok = true, data = result });
                if (name == "shutdown") return 0;
            }
            catch (Exception ex)
            {
                Write(new { id, ok = false, error = ex.Message });
            }
        }
        return 0;
    }

    private static async Task<object?> ExecuteAsync(string? name, JsonElement command)
    {
        var realtime = RealtimeService.Instance;
        switch (name)
        {
            case "register":
                var registration = await AuthService.RegisterAsync(Text(command, "username"), Text(command, "password"), Text(command, "email"));
                if (!registration.Success) throw new InvalidOperationException(registration.Message);
                return new { registration.Message };
            case "login":
                var login = await AuthService.LoginAsync(Text(command, "username"), Text(command, "password"));
                if (!login.Success || login.User == null) throw new InvalidOperationException(login.Message);
                SessionService.Instance.SetCurrentUser(login.User, login.Token);
                return login.User;
            case "connect":
                await realtime.ConnectAsync();
                if (!realtime.IsConnected) throw new InvalidOperationException("Socket.IO did not connect.");
                return new { connected = true };
            case "create_room":
                return await GameApiService.Instance.CreateRoomAsync(command.TryGetProperty("bet_amount", out var bet) ? bet.GetInt32() : 0);
            case "rooms":
                return await GameApiService.Instance.GetRoomsAsync();
            case "join":
                await realtime.JoinRoomAsync(Text(command, "room_code"));
                return null;
            case "ready":
                await realtime.ReadyAsync();
                return null;
            case "move":
                var move = command.GetProperty("move").Deserialize<MoveRequest>(JsonOptions)
                    ?? throw new ArgumentException("Move is required.");
                await realtime.MakeMoveAsync(move);
                return null;
            case "leave":
                await realtime.LeaveRoomAsync();
                return null;
            case "disconnect":
            case "shutdown":
                await realtime.DisconnectAsync();
                return null;
            case "state":
                return new { room_code = realtime.CurrentRoomCode, your_color = realtime.YourColor, game = realtime.CurrentGame, user = SessionService.Instance.CurrentUser };
            case "board_assert":
                return AssertBoard(command);
            default:
                throw new ArgumentException("Unknown command: " + name);
        }
    }

    private static object AssertBoard(JsonElement command)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var board = new BoardControl { NetworkMode = true, PlayerSide = "red" };
                board.LoadServerBoard(command.GetProperty("board"), "red");
                int requested = 0;
                MoveRequest? request = null;
                board.OnMoveRequested += move => { requested++; request = move; };
                board.MovePiece("b_inf2", 10, 6);
                Check(requested == 0, "Opponent pieces can be moved.");
                board.MovePiece("r_air1", 4, 5);
                Check(requested == 1 && request != null, "Own legal move was not submitted.");
                Check(request!.FromX == 4 && request.FromY == 1 && request.ToX == 4 && request.ToY == 5, "Incorrect wire move coordinates.");
                var aircraft = board.Pieces.Single(piece => piece.PieceId == "r_air1");
                Check(aircraft.X == 4 && aircraft.Y == 1 && board.CurrentTurn == "red", "Board mutated before server accepted move.");
                Check(board.IsMovePending, "Pending move was not locked.");
                board.MovePiece("r_air1", 4, 5);
                Check(requested == 1, "Duplicate move was submitted while waiting.");
                board.ApplyServerMove(command.GetProperty("accepted_move"), "blue");
                Check(aircraft.X == 4 && aircraft.Y == 5 && board.CurrentTurn == "blue" && !board.IsMovePending, "Server move did not update board and turn.");
                board.MovePiece("r_air1", 4, 9);
                Check(requested == 1, "Move was submitted outside own turn.");
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(10))) throw new TimeoutException("Board assertion thread timed out.");
        if (failure != null) throw failure;
        return new { authoritative_board = true, ownership_and_turn_guards = true };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Text(JsonElement command, string key) => command.GetProperty(key).GetString() ?? "";

    private static void Write(object data)
    {
        var json = JsonSerializer.Serialize(data);
        lock (OutputLock) Console.WriteLine(json);
    }
}
