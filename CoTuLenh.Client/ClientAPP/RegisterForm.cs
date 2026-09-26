using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ClientAPP;

public partial class RegisterForm : Form
{
    public RegisterForm()
    {
        InitializeComponent();
    }

    private async void btnRegister_Click(object sender, EventArgs e)
    {
        lblErrorMessage.Text = "";

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;
        string email = txtEmail.Text.Trim();

        // 1. Kiểm tra rỗng
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(email))
        {
            lblErrorMessage.Text = "Vui lòng điền đầy đủ các thông tin có dấu (*)!";
            return;
        }

        // 2. Kiểm tra định dạng tên đăng nhập (chỉ gồm chữ và số)
        if (!Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
        {
            lblErrorMessage.Text = "Tên đăng nhập chỉ được chứa chữ cái và chữ số!";
            txtUsername.Focus();
            return;
        }

        // 3. Kiểm tra độ mạnh của mật khẩu (ký tự, số và ký tự đặc biệt)
        bool hasLetter = Regex.IsMatch(password, "[a-zA-Z]");
        bool hasDigit = Regex.IsMatch(password, "[0-9]");
        bool hasSpecial = Regex.IsMatch(password, "[^a-zA-Z0-9]");

        if (password.Length < 6 || !hasLetter || !hasDigit || !hasSpecial)
        {
            lblErrorMessage.Text = "Mật khẩu phải từ 6 ký tự, gồm cả chữ, số và ký tự đặc biệt!";
            txtPassword.Focus();
            return;
        }

        // 4. Kiểm tra nhập lại mật khẩu
        if (password != confirmPassword)
        {
            lblErrorMessage.Text = "Mật khẩu nhập lại không khớp!";
            txtConfirmPassword.Focus();
            return;
        }

        // 5. Kiểm tra định dạng email
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            lblErrorMessage.Text = "Định dạng email không hợp lệ!";
            txtEmail.Focus();
            return;
        }

        btnRegister.Enabled = false;
        btnRegister.Text = "Đang xử lý...";

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            using NetworkStream stream = tcpClient.GetStream();

            // Gửi dữ liệu đăng ký sang Server: REGISTER|username|password|email\n
            string message = $"REGISTER|{username}|{password}|{email}\n";
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            // Đọc phản hồi từ Server
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "REGISTER_SUCCESS")
            {
                MessageBox.Show("Đăng ký tài khoản thành công! Vui lòng đăng nhập.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Mở lại LoginForm
                OpenLoginForm();
            }
            else
            {
                // Server trả về chi tiết lỗi (ví dụ: "Tên đăng nhập đã tồn tại", "Email đã được sử dụng")
                lblErrorMessage.Text = string.IsNullOrEmpty(response) ? "Đăng ký thất bại!" : response;
            }
        }
        catch (Exception)
        {
            lblErrorMessage.Text = "Không thể kết nối đến máy chủ!";
        }
        finally
        {
            btnRegister.Enabled = true;
            btnRegister.Text = "Đăng ký";
        }
    }

    private void lnkBackToLogin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenLoginForm();
    }

    private void OpenLoginForm()
    {
        this.Close();
    }

    private void lblTitle_Click(object sender, EventArgs e)
    {

    }
}
