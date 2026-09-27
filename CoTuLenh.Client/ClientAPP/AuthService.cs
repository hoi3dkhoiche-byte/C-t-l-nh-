using System.Text;
using System.Text.Json;

namespace ClientAPP;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
}

/// <summary>
/// Quản lý gọi REST API (HTTP) lên Server
/// </summary>
public static class AuthService
{
    // Địa chỉ gốc của Web API Server (dễ dàng đổi IP/Port ở đây khi deploy)
    private static readonly HttpClient client = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5000/api/auth/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    // 1. ĐĂNG NHẬP (POST /api/auth/login)
    public static async Task<ApiResponse> LoginAsync(string username, string password)
    {
        try
        {
            var payload = new { username, password };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("login", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Đăng nhập thành công!" : "Tài khoản hoặc mật khẩu không chính xác!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = "Không thể kết nối đến máy chủ Web API!" };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse { Success = false, Message = "Hết thời gian chờ phản hồi từ máy chủ!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }

    // 2. ĐĂNG KÝ (POST /api/auth/register)
    public static async Task<ApiResponse> RegisterAsync(string username, string password, string email)
    {
        try
        {
            var payload = new { username, password, email };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("register", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Đăng ký thành công!" : "Đăng ký thất bại!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = "Không thể kết nối đến máy chủ Web API!" };
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

            HttpResponseMessage response = await client.PostAsync("send-otp", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Mã OTP đã được gửi về email của bạn!" : "Không tìm thấy email trên hệ thống!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = "Không thể kết nối đến máy chủ Web API!" };
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

            HttpResponseMessage response = await client.PostAsync("reset-password", content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var result = JsonSerializer.Deserialize<ApiResponse>(responseBody, jsonOptions);
                if (result != null) return result;
            }

            return new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode ? "Đổi mật khẩu thành công!" : "Mã OTP sai hoặc đã hết hạn!"
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse { Success = false, Message = "Không thể kết nối đến máy chủ Web API!" };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
        }
    }
}
