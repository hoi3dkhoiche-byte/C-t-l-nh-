using System.Text.RegularExpressions;

namespace ClientAPP;

public partial class RegisterForm : BaseForm
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
            var result = await AuthService.RegisterAsync(username, password, email);

            if (result.Success)
            {
                MessageBox.Show(result.Message, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenLoginForm();
            }
            else
            {
                lblErrorMessage.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            lblErrorMessage.Text = $"Lỗi: {ex.Message}";
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
