namespace ClientAPP;

partial class HistoryForm
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
    private DataGridView dgvHistory;
    private Button btnViewMoves;
    private Button btnClose;

    private void InitializeComponent()
    {
        lblTitle = new Label();
        dgvHistory = new DataGridView();
        btnViewMoves = new Button();
        btnClose = new Button();

        ((System.ComponentModel.ISupportInitialize)dgvHistory).BeginInit();
        SuspendLayout();

        // lblTitle
        lblTitle.Text = "LỊCH SỬ CÁC TRẬN ĐÃ ĐẤU";
        lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(31, 58, 95);
        lblTitle.Location = new Point(25, 20);
        lblTitle.AutoSize = true;

        // dgvHistory
        dgvHistory.Location = new Point(25, 65);
        dgvHistory.Size = new Size(730, 360);
        dgvHistory.BackgroundColor = Color.White;
        dgvHistory.BorderStyle = BorderStyle.Fixed3D;
        dgvHistory.RowHeadersVisible = false;
        dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvHistory.MultiSelect = false;
        dgvHistory.AllowUserToAddRows = false;
        dgvHistory.AllowUserToDeleteRows = false;
        dgvHistory.ReadOnly = true;
        dgvHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // btnViewMoves
        btnViewMoves.Text = "Xem biên bản";
        btnViewMoves.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnViewMoves.BackColor = Color.FromArgb(31, 58, 95);
        btnViewMoves.ForeColor = Color.White;
        btnViewMoves.FlatStyle = FlatStyle.Flat;
        btnViewMoves.Location = new Point(480, 445);
        btnViewMoves.Size = new Size(150, 38);
        btnViewMoves.Click += btnViewMoves_Click;

        // btnClose
        btnClose.Text = "Đóng";
        btnClose.Font = new Font("Segoe UI", 10F);
        btnClose.Location = new Point(645, 445);
        btnClose.Size = new Size(110, 38);
        btnClose.Click += (s, e) => this.Close();

        // HistoryForm
        ClientSize = new Size(780, 505);
        Controls.Add(btnClose);
        Controls.Add(btnViewMoves);
        Controls.Add(dgvHistory);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Lịch Sử Đấu - Cờ Tư Lệnh Online";

        ((System.ComponentModel.ISupportInitialize)dgvHistory).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
