using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClientAPP;

public class RoomCard : UserControl
{
    private readonly RoomModel _room;
    public event Action<RoomModel>? OnJoinClicked;

    private readonly Label lblRoomCode = new();
    private readonly Label lblHost = new();
    private readonly Label lblBet = new();
    private readonly Label lblPlayers = new();
    private readonly Button btnJoin = new();

    public RoomCard(RoomModel room)
    {
        _room = room;
        InitializeCard();
    }

    private void InitializeCard()
    {
        this.Size = new Size(240, 140);
        this.BackColor = Color.White;
        this.Margin = new Padding(10);
        this.Padding = new Padding(12);

        // Mã phòng
        lblRoomCode.Text = _room.RoomCode;
        lblRoomCode.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblRoomCode.ForeColor = Color.FromArgb(31, 58, 95); // Navy
        lblRoomCode.Location = new Point(12, 10);
        lblRoomCode.AutoSize = true;

        // Trạng thái người chơi
        lblPlayers.Text = $"{_room.PlayerCount}/2 người";
        lblPlayers.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblPlayers.ForeColor = _room.IsFull ? Color.FromArgb(192, 57, 43) : Color.FromArgb(39, 174, 96);
        lblPlayers.Location = new Point(160, 12);
        lblPlayers.AutoSize = true;

        // Chủ phòng
        lblHost.Text = $"Chủ phòng: {_room.HostUsername}";
        lblHost.Font = new Font("Segoe UI", 9.5F);
        lblHost.ForeColor = Color.FromArgb(80, 90, 100);
        lblHost.Location = new Point(12, 40);
        lblHost.Size = new Size(216, 22);

        // Mức cược
        lblBet.Text = $"Cược: {_room.BetAmount:N0} Xu";
        lblBet.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblBet.ForeColor = Color.FromArgb(201, 162, 39); // Vàng đồng
        lblBet.Location = new Point(12, 65);
        lblBet.AutoSize = true;

        // Nút vào phòng
        btnJoin.Text = _room.IsFull ? "Đang đấu" : "Vào phòng";
        btnJoin.Enabled = !_room.IsFull && _room.Status != "playing";
        btnJoin.Location = new Point(12, 95);
        btnJoin.Size = new Size(216, 32);
        btnJoin.FlatStyle = FlatStyle.Flat;
        btnJoin.BackColor = btnJoin.Enabled ? Color.FromArgb(31, 58, 95) : Color.LightGray;
        btnJoin.ForeColor = Color.White;
        btnJoin.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnJoin.Cursor = btnJoin.Enabled ? Cursors.Hand : Cursors.Default;
        btnJoin.FlatAppearance.BorderSize = 0;
        btnJoin.Click += (s, e) => OnJoinClicked?.Invoke(_room);

        this.Controls.Add(lblRoomCode);
        this.Controls.Add(lblPlayers);
        this.Controls.Add(lblHost);
        this.Controls.Add(lblBet);
        this.Controls.Add(btnJoin);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Vẽ viền bo nhẹ cho thẻ phòng
        using var pen = new Pen(Color.FromArgb(220, 225, 230), 1.5f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
