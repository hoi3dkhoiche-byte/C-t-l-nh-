using System.Windows.Forms;

namespace ClientAPP;

public partial class LeaderboardForm : BaseForm
{
    private List<UserModel> users = new();

    public LeaderboardForm()
    {
        InitializeComponent();
        this.Load += LeaderboardForm_Load;
    }

    private async void LeaderboardForm_Load(object? sender, EventArgs e)
    {
        SetupColumns();
        await LoadDataAsync();
    }

    private void SetupColumns()
    {
        dgvLeaderboard.Columns.Clear();
        dgvLeaderboard.Columns.Add("Rank", "Hạng");
        dgvLeaderboard.Columns.Add("Username", "Kỳ thủ");
        dgvLeaderboard.Columns.Add("Elo", "Điểm ELO");
        dgvLeaderboard.Columns.Add("Wins", "Trận thắng");
        dgvLeaderboard.Columns.Add("WinRate", "Tỉ lệ thắng");

        dgvLeaderboard.Columns[0].Width = 70;
    }

    private async Task LoadDataAsync()
    {
        ShowLoading(true);
        users = await GameApiService.Instance.GetLeaderboardAsync();
        ShowLoading(false);

        DisplayList();
    }

    private void DisplayList()
    {
        dgvLeaderboard.Rows.Clear();

        if (cmbSort.SelectedIndex == 0)
        {
            users = users.OrderByDescending(u => u.EloRating).ToList();
        }
        else
        {
            users = users.OrderByDescending(u => u.Wins).ToList();
        }

        int rank = 1;
        foreach (var u in users)
        {
            string rankStr = rank switch
            {
                1 => "🥇 Top 1",
                2 => "🥈 Top 2",
                3 => "🥉 Top 3",
                _ => $"  {rank}"
            };

            dgvLeaderboard.Rows.Add(
                rankStr,
                u.Username,
                u.EloRating,
                $"{u.Wins} trận",
                $"{u.WinRate}%"
            );
            rank++;
        }

        var me = SessionService.Instance.CurrentUser;
        if (me != null)
        {
            lblMyRank.Text = $"Vị trí của bạn ({me.Username}): ELO {me.EloRating} | {me.Wins} Thắng";
        }
    }

    private void cmbSort_SelectedIndexChanged(object? sender, EventArgs e)
    {
        DisplayList();
    }

    private async void btnRefresh_Click(object? sender, EventArgs e)
    {
        await LoadDataAsync();
    }
}
