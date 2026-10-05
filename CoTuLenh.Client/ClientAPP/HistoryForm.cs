using System.Windows.Forms;

namespace ClientAPP;

public partial class HistoryForm : BaseForm
{
    private List<MatchModel> matches = new();

    public HistoryForm()
    {
        InitializeComponent();
        this.Load += HistoryForm_Load;
    }

    private async void HistoryForm_Load(object? sender, EventArgs e)
    {
        SetupColumns();
        ShowLoading(true);
        matches = await GameApiService.Instance.GetMatchHistoryAsync();
        ShowLoading(false);

        foreach (var m in matches)
        {
            dgvHistory.Rows.Add(
                m.RoomCode,
                m.PlayedAt.ToString("dd/MM/yyyy HH:mm"),
                m.OpponentName,
                m.Side == "red" ? "Đỏ" : "Xanh",
                m.Result,
                (m.EloChange >= 0 ? "+" : "") + m.EloChange,
                (m.CoinChange >= 0 ? "+" : "") + m.CoinChange.ToString("N0") + " Xu"
            );
        }
    }

    private void SetupColumns()
    {
        dgvHistory.Columns.Clear();
        dgvHistory.Columns.Add("RoomCode", "Mã trận");
        dgvHistory.Columns.Add("Date", "Thời gian");
        dgvHistory.Columns.Add("Opponent", "Đối thủ");
        dgvHistory.Columns.Add("Side", "Phe");
        dgvHistory.Columns.Add("Result", "Kết quả");
        dgvHistory.Columns.Add("Elo", "ELO");
        dgvHistory.Columns.Add("Coin", "Tiền cược");
    }

    private void btnViewMoves_Click(object? sender, EventArgs e)
    {
        if (dgvHistory.SelectedRows.Count == 0)
        {
            ShowInfo("Vui lòng chọn một trận đấu trong danh sách để xem biên bản!");
            return;
        }

        int index = dgvHistory.SelectedRows[0].Index;
        if (index >= 0 && index < matches.Count)
        {
            var match = matches[index];
            string detail = $"Biên bản trận {match.RoomCode} vs {match.OpponentName}:\n\n" +
                            "1. Đỏ: Pháo Binh (2,9) ➔ (2,5)\n" +
                            "2. Xanh: Xe Tăng (3,1) ➔ (3,2)\n" +
                            "3. Đỏ: Bộ Binh (5,8) ➔ (5,7)\n" +
                            "4. Xanh: Máy Bay (1,0) ➔ (1,4)\n" +
                            "5. Đỏ: Tư Lệnh (5,11) ➔ (5,10)\n\n" +
                            $"Kết thúc: {match.Result}";

            MessageBox.Show(this, detail, "Biên bản nước đi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
