using System.Windows.Forms;

namespace ClientAPP;

public partial class DepositForm : BaseForm
{
    public DepositForm()
    {
        InitializeComponent();
        this.Load += DepositForm_Load;
    }

    private void DepositForm_Load(object? sender, EventArgs e)
    {
        UpdateBalanceDisplay();
        lstDepositHistory.Items.Add($"[{DateTime.Now.AddDays(-1):dd/MM HH:mm}] Nạp +50,000 Xu qua Momo - Thành công");
        lstDepositHistory.Items.Add($"[{DateTime.Now.AddDays(-3):dd/MM HH:mm}] Nạp +100,000 Xu qua QR Ngân hàng - Thành công");
    }

    private void UpdateBalanceDisplay()
    {
        var user = SessionService.Instance.CurrentUser;
        int coins = user?.Coins ?? 0;
        lblCurrentBalance.Text = $"Số dư hiện tại: {coins:N0} Xu";
    }

    private async void btnConfirm_Click(object? sender, EventArgs e)
    {
        int amount = 100000;
        if (rad20k.Checked) amount = 20000;
        else if (rad50k.Checked) amount = 50000;
        else if (rad100k.Checked) amount = 110000;
        else if (rad200k.Checked) amount = 230000;
        else if (rad500k.Checked) amount = 600000;

        string method = "QR Ngân hàng";
        if (radMomo.Checked) method = "MoMo";
        else if (radATM.Checked) method = "Thẻ ATM";

        ShowLoading(true);
        bool success = await GameApiService.Instance.DepositCoinsAsync(amount, method);
        ShowLoading(false);

        if (success)
        {
            UpdateBalanceDisplay();
            lstDepositHistory.Items.Insert(0, $"[{DateTime.Now:dd/MM HH:mm}] Nạp +{amount:N0} Xu qua {method} - Thành công");
            ShowSuccess($"Nạp thành công {amount:N0} Xu vào tài khoản!");
        }
        else
        {
            ShowError("Nạp tiền không thành công, vui lòng thử lại!");
        }
    }
}
