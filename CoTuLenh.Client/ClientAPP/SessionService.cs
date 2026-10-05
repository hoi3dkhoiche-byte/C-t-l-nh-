namespace ClientAPP;

public sealed class SessionService
{
    private static readonly Lazy<SessionService> _instance = new(() => new SessionService());
    public static SessionService Instance => _instance.Value;

    public UserModel? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    public event Action? OnUserDataChanged;

    private SessionService() { }

    public void SetCurrentUser(UserModel user, string? token = null)
    {
        CurrentUser = user;
        if (!string.IsNullOrEmpty(token))
        {
            Token = token;
        }
        OnUserDataChanged?.Invoke();
    }

    public void UpdateCoins(int newCoins)
    {
        if (CurrentUser != null)
        {
            CurrentUser.Coins = newCoins;
            OnUserDataChanged?.Invoke();
        }
    }

    public void AddCoins(int amount)
    {
        if (CurrentUser != null)
        {
            CurrentUser.Coins += amount;
            OnUserDataChanged?.Invoke();
        }
    }

    public void UpdateElo(int eloDelta, bool isWin)
    {
        if (CurrentUser != null)
        {
            CurrentUser.EloRating += eloDelta;
            if (isWin) CurrentUser.Wins++;
            else CurrentUser.Losses++;
            OnUserDataChanged?.Invoke();
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        Token = null;
        OnUserDataChanged?.Invoke();
    }
}
