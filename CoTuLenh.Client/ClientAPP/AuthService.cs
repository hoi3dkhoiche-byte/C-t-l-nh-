using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientAPP;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
    public UserModel? User { get; set; }
}

/// <summary>
/// Quản lý gọi REST API xác thực (HTTP) lên Server theo địa chỉ động trong ServerConfig.
/// </summary>
public static class AuthService
{
    private static readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static Uri Endpoint(string relativePath) => new(ServerConfig.AuthApiBaseUri, relativePath);

    private static string ExtractMessage(JsonElement root, string fallback)
    {
        if (root.TryGetProperty("message", out var messageElem) && messageElem.ValueKind == JsonValueKind.String)
            return messageElem.GetString() ?? fallback;
        if (root.TryGetProperty("error", out var errorElem) && errorElem.ValueKind == JsonValueKind.String)
            return errorElem.GetString() ?? fallback;
        if (root.TryGetProperty("msg", out var msgElem) && msgElem.ValueKind == JsonValueKind.String)
            return msgElem.GetString() ?? fallback;
        return fallback;
    }

    // 1. ĐĂNG NHẬP (POST /api/auth/login)
    public static async Task<ApiResponse> LoginAsync(string username, string password)
    {
        try
        {
            var payload = new { identifier = username, password };
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response = await client.PostAsync(Endpoint("login"), content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Máy chủ không trả về dữ liệu."
                };
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (response.IsSuccessStatusCode)
            {
                string? token = root.TryGetProperty("access_token", out var tokenElement)
                    ? tokenElement.GetString()
                    : null;

                UserModel? user = null;
                if (root.TryGetProperty("user", out var userElement))
                {
                    user = userElement.Deserialize<UserModel>(jsonOptions);
                }

                // Lấy thêm thông tin đầy đủ từ /api/auth/me (bao gồm uy tín và trạng thái cấm chơi)
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var meUser = await GetMeAsync(token);
                    if (meUser != null)
                    {
                        user = meUser;
                    }
                }

                return new ApiResponse
                {
                    Success = token != null,
                    Message = token != null
                        ? "Đăng nhập thành công!"
                        : "Đăng nhập được nhưng máy chủ không trả về token.",
                    Token = token,
                    User = user
                };
            }

            return new ApiResponse
            {
                Success = false,
                Message = ExtractMessage(root, "Tài khoản hoặc mật khẩu không chính xác!")
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = $"Không thể kết nối đến máy chủ ({ServerConfig.BaseUrl})!" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse { Success = false, Message = $"Hết thời gian chờ phản hồi từ máy chủ ({ServerConfig.BaseUrl})!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }

    // 1b. ĐỒNG BỘ THÔNG TIN NGƯỜI DÙNG HIỆN TẠI (GET /api/auth/me)
    public static async Task<UserModel?> GetMeAsync(string? token = null)
    {
        try
        {
            string? jwt = token ?? SessionService.Instance.Token;
            if (string.IsNullOrWhiteSpace(jwt)) return null;

            using var req = new HttpRequestMessage(HttpMethod.Get, Endpoint("me"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            using var res = await client.SendAsync(req);
            if (!res.IsSuccessStatusCode) return null;

            string body = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("user", out var userElem))
            {
                return userElem.Deserialize<UserModel>(jsonOptions);
            }
        }
        catch
        {
            // Bỏ qua nếu mạng chập chờn
        }
        return null;
    }

    // 2. ĐĂNG KÝ (POST /api/auth/register)
    public static async Task<ApiResponse> RegisterAsync(string username, string password, string email)
    {
        try
        {
            var payload = new { username, password, email };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(Endpoint("register"), content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Máy chủ không trả về dữ liệu."
                };
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            string fallback = response.IsSuccessStatusCode ? "Đăng ký thành công!" : "Đăng ký thất bại!";
            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = ExtractMessage(root, fallback)
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = $"Không thể kết nối đến máy chủ ({ServerConfig.BaseUrl})!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }

    // 3. GỬI MÃ OTP (POST /api/auth/send-otp)
    public static async Task<ApiResponse> SendOtpAsync(string email)
    {
        try
        {
            var payload = new { email };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(Endpoint("send-otp"), content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null && !string.IsNullOrWhiteSpace(result.Message)) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Mã OTP đã được gửi về email của bạn!" : "Không tìm thấy email trên hệ thống!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = $"Không thể kết nối đến máy chủ ({ServerConfig.BaseUrl})!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }

    // 4. ĐẶT LẠI MẬT KHẨU (POST /api/auth/reset-password)
    public static async Task<ApiResponse> ResetPasswordAsync(string email, string otp, string newPassword)
    {
        try
        {
            var payload = new { email, otp, newPassword };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(Endpoint("reset-password"), content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null && !string.IsNullOrWhiteSpace(result.Message)) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Đổi mật khẩu thành công!" : "Mã OTP sai hoặc đã hết hạn!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = $"Không thể kết nối đến máy chủ ({ServerConfig.BaseUrl})!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }
}
