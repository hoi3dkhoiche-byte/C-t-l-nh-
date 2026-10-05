using System.Windows.Forms;

namespace ClientAPP;

public partial class ProfileForm : BaseForm
{
    public ProfileForm()
    {
        InitializeComponent();
        this.Load += ProfileForm_Load;
    }

    private void ProfileForm_Load(object? sender, EventArgs e)
    {
        LoadUserData();
    }

    private void LoadUserData()
    {
        var user = SessionService.Instance.CurrentUser;
        if (user != null)
        {
            lblUsername.Text = $"Tài khoản: {user.Username}";
            lblEmail.Text = $"Email: {user.Email}";
            lblElo.Text = $"Điểm ELO: {user.EloRating}";
            lblCoins.Text = $"Số dư: {user.Coins:N0} Xu";

            lblTotalMatches.Text = $"Tổng trận: {user.TotalGames}";
            lblWins.Text = $"Thắng: {user.Wins}";
            lblLosses.Text = $"Thua: {user.Losses}";
            lblWinRate.Text = $"Tỉ lệ thắng: {user.WinRate}%";
        }
    }

    private void btnChangeAvatar_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                picAvatar.Image = Image.FromFile(ofd.FileName);
                picAvatar.SizeMode = PictureBoxSizeMode.Zoom;
                ShowSuccess("Đã cập nhật ảnh đại diện!");
            }
            catch (Exception ex)
            {
                ShowError($"Không thể tải ảnh: {ex.Message}");
            }
        }
    }

    private void btnUpdatePassword_Click(object? sender, EventArgs e)
    {
        string oldP = txtOldPassword.Text;
        string newP = txtNewPassword.Text;
        string confP = txtConfirmPassword.Text;

        if (string.IsNullOrEmpty(oldP) || string.IsNullOrEmpty(newP))
        {
            ShowError("Vui lòng nhập đầy đủ mật khẩu cũ và mới!");
            return;
        }

        if (newP != confP)
        {
            ShowError("Mật khẩu xác nhận không khớp!");
            return;
        }

        if (newP.Length < 6)
        {
            ShowError("Mật khẩu mới phải có ít nhất 6 ký tự!");
            return;
        }

        ShowSuccess("Đổi mật khẩu thành công!");
        txtOldPassword.Clear();
        txtNewPassword.Clear();
        txtConfirmPassword.Clear();
    }
}
