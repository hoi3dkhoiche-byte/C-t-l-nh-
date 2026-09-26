using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ClientAPP;

public partial class ForgotPasswordForm : Form
{
    private string savedEmail = "";
    private int countdownSeconds = 60;

    public ForgotPasswordForm()
    {
        InitializeComponent();
    }

    // 1. GỬI MÃ OTP
    private async void btnSendOtp_Click(object sender, EventArgs e)
    {
        lblOtpError.Text = "";
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblOtpError.Text = "Vui lòng nhập địa chỉ email của bạn!";
            txtEmail.Focus();
            return;
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            lblOtpError.Text = "Định dạng email không hợp lệ!";
            txtEmail.Focus();
            return;
        }

        btnSendOtp.Enabled = false;
        btnSendOtp.Text = "Đang gửi...";

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            using NetworkStream stream = tcpClient.GetStream();

            // Gửi: FORGOT_PASSWORD|SEND_OTP|email\n
            string message = $"FORGOT_PASSWORD|SEND_OTP|{email}\n";
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "OTP_SENT")
            {
                savedEmail = email;
                MessageBox.Show("Mã OTP đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Chuyển sang giao diện Nhập OTP & Đặt lại mật khẩu
                pnlSendOtp.Visible = false;
                pnlResetPassword.Visible = true;

                // Bắt đầu đếm ngược 60 giây gửi lại OTP
                StartCountdown();
            }
            else
            {
                lblOtpError.Text = string.IsNullOrEmpty(response) ? "Không tìm thấy email trên hệ thống!" : response;
            }
        }
        catch (Exception)
        {
            lblOtpError.Text = "Không thể kết nối đến máy chủ!";
        }
        finally
        {
            btnSendOtp.Enabled = true;
            btnSendOtp.Text = "Gửi mã OTP";
        }
    }

    // 2. ĐẾM NGƯỢC THỜI GIAN GỬI LẠI OTP
    private void StartCountdown()
    {
        countdownSeconds = 60;
        lblCountdown.Text = $"Gửi lại sau: {countdownSeconds}s";
        lblCountdown.Visible = true;
        lnkResendOtp.Visible = false;
        lnkResendOtp.Enabled = false;
        timerCountdown.Start();
    }

    private void timerCountdown_Tick(object sender, EventArgs e)
    {
        countdownSeconds--;
        if (countdownSeconds > 0)
        {
            lblCountdown.Text = $"Gửi lại sau: {countdownSeconds}s";
        }
        else
        {
            timerCountdown.Stop();
            lblCountdown.Visible = false;
            lnkResendOtp.Visible = true;
            lnkResendOtp.Enabled = true;
        }
    }

    // 3. GỬI LẠI MÃ OTP
    private async void lnkResendOtp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            using NetworkStream stream = tcpClient.GetStream();

            string message = $"FORGOT_PASSWORD|SEND_OTP|{savedEmail}\n";
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "OTP_SENT")
            {
                MessageBox.Show("Đã gửi lại mã OTP mới về email!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartCountdown();
            }
            else
            {
                lblResetError.Text = "Không thể gửi lại mã OTP!";
            }
        }
        catch (Exception)
        {
            lblResetError.Text = "Không thể kết nối đến máy chủ!";
        }
    }

    // 4. XÁC NHẬN ĐỔI MẬT KHẨU
    private async void btnResetPassword_Click(object sender, EventArgs e)
    {
        lblResetError.Text = "";

        string otp = txtOtp.Text.Trim();
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmNewPassword.Text;

        if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            lblResetError.Text = "Vui lòng nhập đầy đủ mã OTP và mật khẩu mới!";
            return;
        }

        if (otp.Length < 6)
        {
            lblResetError.Text = "Mã OTP phải có đúng 6 chữ số!";
            txtOtp.Focus();
            return;
        }

        // Kiểm tra độ mạnh mật khẩu mới
        bool hasLetter = Regex.IsMatch(newPassword, "[a-zA-Z]");
        bool hasDigit = Regex.IsMatch(newPassword, "[0-9]");
        bool hasSpecial = Regex.IsMatch(newPassword, "[^a-zA-Z0-9]");

        if (newPassword.Length < 6 || !hasLetter || !hasDigit || !hasSpecial)
        {
            lblResetError.Text = "Mật khẩu mới phải từ 6 ký tự, gồm chữ, số và ký tự đặc biệt!";
            txtNewPassword.Focus();
            return;
        }

        if (newPassword != confirmPassword)
        {
            lblResetError.Text = "Mật khẩu xác nhận không khớp!";
            txtConfirmNewPassword.Focus();
            return;
        }

        btnResetPassword.Enabled = false;
        btnResetPassword.Text = "Đang đổi...";

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            using NetworkStream stream = tcpClient.GetStream();

            // Gửi: FORGOT_PASSWORD|RESET|email|otp|newPassword\n
            string message = $"FORGOT_PASSWORD|RESET|{savedEmail}|{otp}|{newPassword}\n";
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "RESET_SUCCESS")
            {
                timerCountdown.Stop();
                MessageBox.Show("Đổi mật khẩu thành công! Bạn có thể đăng nhập ngay bây giờ.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenLoginForm();
            }
            else
            {
                lblResetError.Text = string.IsNullOrEmpty(response) ? "Mã OTP sai hoặc đã hết hạn!" : response;
            }
        }
        catch (Exception)
        {
            lblResetError.Text = "Không thể kết nối đến máy chủ!";
        }
        finally
        {
            btnResetPassword.Enabled = true;
            btnResetPassword.Text = "Đổi mật khẩu";
        }
    }

    // 5. QUAY LẠI MÀN HÌNH ĐĂNG NHẬP
    private void lnkBack_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        timerCountdown.Stop();
        OpenLoginForm();
    }

    private void OpenLoginForm()
    {
        this.Close();
    }

    private void lblTitleReset_Click(object sender, EventArgs e)
    {

    }

    private void pnlResetPassword_Paint(object sender, PaintEventArgs e)
    {

    }
}
