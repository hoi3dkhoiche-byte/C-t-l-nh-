using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClientAPP;

public partial class LoginForm : Form
{
    private readonly string rememberFilePath = "remember.json";

    public LoginForm()
    {
        InitializeComponent();
    }

    // 1. Tự động đọc thông tin tài khoản đã ghi nhớ khi form vừa mở lên
    private void Form1_Load(object sender, EventArgs e)
    {
        try
        {
            if (File.Exists(rememberFilePath))
            {
                string json = File.ReadAllText(rememberFilePath);
                var data = JsonSerializer.Deserialize<RememberData>(json);

                if (data != null && data.IsRemember)
                {
                    txtUsername.Text = data.Username;
                    txtPassword.Text = data.Password;
                    chkRememberMe.Checked = true;
                }
            }
        }
        catch
        {
            // Bỏ qua nếu file hỏng
        }
    }

    // 2. Thêm từ khóa 'async' vào đây để dùng được await
    private async void btnLogin_Click(object sender, EventArgs e)
    {
        lblErrorMessage.Text = "";

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username))
        {
            lblErrorMessage.Text = "Vui lòng nhập tên đăng nhập hay email của bạn!";
            txtUsername.Focus();
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            lblErrorMessage.Text = "Vui lòng nhập mật khẩu!";
            txtPassword.Focus();
            return;
        }

        btnLogin.Enabled = false;
        btnLogin.Text = "Đang kết nối...";

        try
        {
            using var tcpClient = new TcpClient();

            // Kết nối đến Server IP & Port (127.0.0.1:8888)
            await tcpClient.ConnectAsync("127.0.0.1", 8888);
            using NetworkStream stream = tcpClient.GetStream();

            // Gửi: LOGIN|user|pass\n
            string message = $"LOGIN|{username}|{password}\n";
            byte[] dataToSend = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

            // Đọc phản hồi từ Server
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (response == "LOGIN_SUCCESS")
            {
                // === XỬ LÝ GHI NHỚ ĐĂNG NHẬP ===
                if (chkRememberMe.Checked)
                {
                    var data = new RememberData
                    {
                        Username = username,
                        Password = password,
                        IsRemember = true
                    };
                    string json = JsonSerializer.Serialize(data);
                    File.WriteAllText(rememberFilePath, json);
                }
                else
                {
                    if (File.Exists(rememberFilePath))
                    {
                        File.Delete(rememberFilePath);
                    }
                }

                MessageBox.Show("Đăng nhập thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // TODO: Chuyển màn hình vào sảnh game (Lobby / MainForm)
            }
            else
            {
                lblErrorMessage.Text = "Sai tài khoản hoặc mật khẩu!";
            }
        }
        catch (Exception)
        {
            lblErrorMessage.Text = "Không thể kết nối tới Server!";
        }
        finally
        {
            btnLogin.Enabled = true;
            btnLogin.Text = "Đăng nhập";
        }
    }

    private void txtUsername_TextChanged(object sender, EventArgs e)
    {
        lblErrorMessage.Text = "";
    }

    private void txtPassword_TextChanged(object sender, EventArgs e)
    {
        lblErrorMessage.Text = "";
    }

    private void btnRegister_Click(object sender, EventArgs e)
    {
        RegisterForm registerForm = new RegisterForm();
        registerForm.FormClosed += (s, args) => this.Show();
        registerForm.Show();
        this.Hide();
    }

    private void lnkForgotPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        ForgotPasswordForm forgotForm = new ForgotPasswordForm();
        forgotForm.FormClosed += (s, args) => this.Show();
        forgotForm.Show();
        this.Hide();
    }

    private void label2_Click(object sender, EventArgs e) { }
    private void label1_Click(object sender, EventArgs e) { }
    private void chkRememberMe_CheckedChanged(object sender, EventArgs e) { }
    private void lblErrorMessage_Click(object sender, EventArgs e) { }

    private void lblTitle_Click(object sender, EventArgs e)
    {

    }
}

// 3. Class lưu dữ liệu ghi nhớ đăng nhập
public class RememberData
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool IsRemember { get; set; }
}
