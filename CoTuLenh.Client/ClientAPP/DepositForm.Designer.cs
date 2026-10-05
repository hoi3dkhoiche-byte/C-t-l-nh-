namespace ClientAPP;

partial class DepositForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private Label lblTitle;
    private Label lblCurrentBalance;
    private GroupBox grpPackages;
    private RadioButton rad20k;
    private RadioButton rad50k;
    private RadioButton rad100k;
    private RadioButton rad200k;
    private RadioButton rad500k;

    private GroupBox grpMethods;
    private RadioButton radQR;
    private RadioButton radMomo;
    private RadioButton radATM;

    private Button btnConfirm;
    private Button btnClose;
    private Label lblHistoryTitle;
    private ListBox lstDepositHistory;

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblCurrentBalance = new Label();
        grpPackages = new GroupBox();
        rad20k = new RadioButton();
        rad50k = new RadioButton();
        rad100k = new RadioButton();
        rad200k = new RadioButton();
        rad500k = new RadioButton();

        grpMethods = new GroupBox();
        radQR = new RadioButton();
        radMomo = new RadioButton();
        radATM = new RadioButton();

        btnConfirm = new Button();
        btnClose = new Button();
        lblHistoryTitle = new Label();
        lstDepositHistory = new ListBox();

        grpPackages.SuspendLayout();
        grpMethods.SuspendLayout();
        SuspendLayout();

        // lblTitle
        lblTitle.Text = "NẠP XU TÀI KHOẢN (MÔ PHỎNG)";
        lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(31, 58, 95);
        lblTitle.Location = new Point(25, 20);
        lblTitle.AutoSize = true;

        // lblCurrentBalance
        lblCurrentBalance.Text = "Số dư hiện tại: 0 Xu";
        lblCurrentBalance.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblCurrentBalance.ForeColor = Color.FromArgb(201, 162, 39);
        lblCurrentBalance.Location = new Point(25, 55);
        lblCurrentBalance.AutoSize = true;

        // grpPackages
        grpPackages.Text = "Chọn gói nạp";
        grpPackages.Location = new Point(25, 95);
        grpPackages.Size = new Size(340, 160);
        grpPackages.Controls.Add(rad20k);
        grpPackages.Controls.Add(rad50k);
        grpPackages.Controls.Add(rad100k);
        grpPackages.Controls.Add(rad200k);
        grpPackages.Controls.Add(rad500k);

        rad20k.Text = "20,000 Xu (20.000 VNĐ)";
        rad20k.Location = new Point(20, 25);
        rad20k.AutoSize = true;

        rad50k.Text = "50,000 Xu (50.000 VNĐ)";
        rad50k.Location = new Point(20, 50);
        rad50k.AutoSize = true;

        rad100k.Text = "100,000 Xu + tặng 10,000 Xu";
        rad100k.Location = new Point(20, 75);
        rad100k.Checked = true;
        rad100k.AutoSize = true;

        rad200k.Text = "200,000 Xu + tặng 30,000 Xu";
        rad200k.Location = new Point(20, 100);
        rad200k.AutoSize = true;

        rad500k.Text = "500,000 Xu + tặng 100,000 Xu";
        rad500k.Location = new Point(20, 125);
        rad500k.AutoSize = true;

        // grpMethods
        grpMethods.Text = "Phương thức thanh toán";
        grpMethods.Location = new Point(385, 95);
        grpMethods.Size = new Size(240, 160);
        grpMethods.Controls.Add(radQR);
        grpMethods.Controls.Add(radMomo);
        grpMethods.Controls.Add(radATM);

        radQR.Text = "Quét mã QR Ngân hàng";
        radQR.Location = new Point(20, 30);
        radQR.Checked = true;
        radQR.AutoSize = true;

        radMomo.Text = "Ví điện tử MoMo";
        radMomo.Location = new Point(20, 65);
        radMomo.AutoSize = true;

        radATM.Text = "Thẻ ATM Nội địa";
        radATM.Location = new Point(20, 100);
        radATM.AutoSize = true;

        // btnConfirm
        btnConfirm.Text = "Xác nhận nạp ngay";
        btnConfirm.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnConfirm.BackColor = Color.FromArgb(39, 174, 96);
        btnConfirm.ForeColor = Color.White;
        btnConfirm.FlatStyle = FlatStyle.Flat;
        btnConfirm.Location = new Point(25, 270);
        btnConfirm.Size = new Size(340, 42);
        btnConfirm.Click += btnConfirm_Click;

        // btnClose
        btnClose.Text = "Đóng";
        btnClose.Font = new Font("Segoe UI", 10F);
        btnClose.Location = new Point(385, 270);
        btnClose.Size = new Size(240, 42);
        btnClose.Click += (s, e) => this.Close();

        // History
        lblHistoryTitle.Text = "Lịch sử nạp gần đây:";
        lblHistoryTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblHistoryTitle.Location = new Point(25, 330);
        lblHistoryTitle.AutoSize = true;

        lstDepositHistory.Location = new Point(25, 360);
        lstDepositHistory.Size = new Size(600, 120);

        // Form
        ClientSize = new Size(650, 500);
        Controls.Add(lstDepositHistory);
        Controls.Add(lblHistoryTitle);
        Controls.Add(btnClose);
        Controls.Add(btnConfirm);
        Controls.Add(grpMethods);
        Controls.Add(grpPackages);
        Controls.Add(lblCurrentBalance);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Nạp Xu - Cờ Tư Lệnh Online";

        grpPackages.ResumeLayout(false);
        grpPackages.PerformLayout();
        grpMethods.ResumeLayout(false);
        grpMethods.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
