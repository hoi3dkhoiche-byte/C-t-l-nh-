namespace ClientAPP;

partial class LeaderboardForm
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
    private ComboBox cmbSort;
    private DataGridView dgvLeaderboard;
    private Label lblMyRank;
    private Button btnRefresh;
    private Button btnClose;

    private void InitializeComponent()
    {
        lblTitle = new Label();
        cmbSort = new ComboBox();
        dgvLeaderboard = new DataGridView();
        lblMyRank = new Label();
        btnRefresh = new Button();
        btnClose = new Button();

        ((System.ComponentModel.ISupportInitialize)dgvLeaderboard).BeginInit();
        SuspendLayout();

        // lblTitle
        lblTitle.Text = "🏆 BẢNG XẾP HẠNG CAO THỦ 🏆";
        lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(31, 58, 95);
        lblTitle.Location = new Point(25, 20);
        lblTitle.AutoSize = true;

        // cmbSort
        cmbSort.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbSort.Font = new Font("Segoe UI", 9.5F);
        cmbSort.Items.AddRange(new object[] { "Sắp xếp theo Điểm ELO", "Sắp xếp theo Số trận thắng" });
        cmbSort.SelectedIndex = 0;
        cmbSort.Location = new Point(480, 20);
        cmbSort.Size = new Size(200, 28);
        cmbSort.SelectedIndexChanged += cmbSort_SelectedIndexChanged;

        // dgvLeaderboard
        dgvLeaderboard.Location = new Point(25, 65);
        dgvLeaderboard.Size = new Size(655, 340);
        dgvLeaderboard.BackgroundColor = Color.White;
        dgvLeaderboard.BorderStyle = BorderStyle.Fixed3D;
        dgvLeaderboard.RowHeadersVisible = false;
        dgvLeaderboard.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvLeaderboard.MultiSelect = false;
        dgvLeaderboard.AllowUserToAddRows = false;
        dgvLeaderboard.AllowUserToDeleteRows = false;
        dgvLeaderboard.ReadOnly = true;
        dgvLeaderboard.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // lblMyRank
        lblMyRank.Text = "Vị trí của bạn: Hạng 12 | ELO: 1000";
        lblMyRank.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblMyRank.ForeColor = Color.FromArgb(201, 162, 39);
        lblMyRank.Location = new Point(25, 425);
        lblMyRank.AutoSize = true;

        // btnRefresh
        btnRefresh.Text = "↻ Làm mới";
        btnRefresh.Font = new Font("Segoe UI", 9.5F);
        btnRefresh.Location = new Point(460, 420);
        btnRefresh.Size = new Size(100, 35);
        btnRefresh.Click += btnRefresh_Click;

        // btnClose
        btnClose.Text = "Đóng";
        btnClose.Font = new Font("Segoe UI", 9.5F);
        btnClose.Location = new Point(575, 420);
        btnClose.Size = new Size(105, 35);
        btnClose.Click += (s, e) => this.Close();

        // LeaderboardForm
        ClientSize = new Size(705, 475);
        Controls.Add(btnClose);
        Controls.Add(btnRefresh);
        Controls.Add(lblMyRank);
        Controls.Add(dgvLeaderboard);
        Controls.Add(cmbSort);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Bảng Xếp Hạng - Cờ Tư Lệnh Online";

        ((System.ComponentModel.ISupportInitialize)dgvLeaderboard).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
