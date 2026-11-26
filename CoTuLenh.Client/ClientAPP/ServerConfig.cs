using System.Text.Json;

namespace ClientAPP;

/// <summary>
/// Quản lý địa chỉ máy chủ dùng chung cho toàn bộ REST API và Socket.IO (hỗ trợ LAN / Cloud).
/// Tự động lưu vào server_config.json để không cần nhập lại mỗi lần mở game.
/// </summary>
public static class ServerConfig
{
    private const string DefaultServerUrl = "http://localhost:5000";
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_config.json");

    private static string _baseUrl = LoadSavedUrl();

    public static string BaseUrl => _baseUrl;
    public static Uri BaseUri => new(_baseUrl);
    public static Uri AuthApiBaseUri => new($"{_baseUrl}/api/auth/");
    public static Uri ApiBaseUri => new($"{_baseUrl}/api/");

    public static event Action<string>? OnServerUrlChanged;

    public static bool TrySetBaseUrl(string? rawInput, out string errorMessage)
    {
        errorMessage = "";
        string normalized = NormalizeUrl(rawInput);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "Địa chỉ máy chủ không hợp lệ (Ví dụ: http://192.168.1.15:5000)";
            return false;
        }

        string clean = $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
        bool changed = !string.Equals(_baseUrl, clean, StringComparison.OrdinalIgnoreCase);
        _baseUrl = clean;
        SaveUrl(_baseUrl);

        if (changed)
        {
            OnServerUrlChanged?.Invoke(_baseUrl);
        }
        return true;
    }

    public static string NormalizeUrl(string? rawInput)
    {
        string text = (rawInput ?? "").Trim();
        if (string.IsNullOrEmpty(text))
        {
            return DefaultServerUrl;
        }

        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            text = "http://" + text;
        }

        // Nếu người dùng chỉ gõ IP LAN (ví dụ http://192.168.1.15) chưa kèm port thì mặc định thêm :5000
        if (Uri.TryCreate(text, UriKind.Absolute, out var parsed) &&
            parsed.IsDefaultPort &&
            !text.Contains(":80", StringComparison.Ordinal) &&
            !text.Contains(":443", StringComparison.Ordinal) &&
            parsed.Scheme == Uri.UriSchemeHttp)
        {
            return $"{parsed.Scheme}://{parsed.Host}:5000";
        }

        return text.TrimEnd('/');
    }

    private static string LoadSavedUrl()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                var data = JsonSerializer.Deserialize<SavedServerConfig>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.ServerUrl))
                {
                    string normalized = NormalizeUrl(data.ServerUrl);
                    if (Uri.TryCreate(normalized, UriKind.Absolute, out _))
                    {
                        return normalized;
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua nếu file cấu hình lỗi
        }
        return DefaultServerUrl;
    }

    private static void SaveUrl(string url)
    {
        try
        {
            string json = JsonSerializer.Serialize(new SavedServerConfig { ServerUrl = url });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
            // Bỏ qua nếu không ghi được file
        }
    }

    private sealed class SavedServerConfig
    {
        public string ServerUrl { get; set; } = DefaultServerUrl;
    }
}
