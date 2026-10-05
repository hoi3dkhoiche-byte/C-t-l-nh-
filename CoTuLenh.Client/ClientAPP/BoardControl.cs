using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClientAPP;

public class BoardControl : UserControl
{
    public const int COLS = 11;
    public const int ROWS = 12;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public List<PieceView> Pieces { get; private set; } = new();

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public PieceView? SelectedPiece { get; private set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string CurrentTurn { get; set; } = "red";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string PlayerSide { get; set; } = "red"; // Phe của client hiện tại

    public event Action<PieceView, int, int>? OnPieceMoved;
    public event Action<PieceView?>? OnPieceSelected;

    private Point? hoverPoint = null;
    private int cellWidth = 50;
    private int cellHeight = 50;
    private int marginX = 40;
    private int marginY = 40;

    public BoardControl()
    {
        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(235, 230, 218); // Màu giấy quân sự
        this.Size = new Size(600, 680);
        InitializeStandardPieces();
    }

    public void InitializeStandardPieces()
    {
        Pieces.Clear();
        SelectedPiece = null;

        // Quân Đỏ (Phía dưới)
        Pieces.Add(new CommanderView { PieceId = "r_cmd", Side = "red", X = 5, Y = 11 });
        Pieces.Add(new TankView { PieceId = "r_tank1", Side = "red", X = 3, Y = 10 });
        Pieces.Add(new TankView { PieceId = "r_tank2", Side = "red", X = 7, Y = 10 });
        Pieces.Add(new ArtilleryView { PieceId = "r_art1", Side = "red", X = 2, Y = 9 });
        Pieces.Add(new ArtilleryView { PieceId = "r_art2", Side = "red", X = 8, Y = 9 });
        Pieces.Add(new AirUnitView { PieceId = "r_air1", Side = "red", X = 1, Y = 11 });
        Pieces.Add(new AirUnitView { PieceId = "r_air2", Side = "red", X = 9, Y = 11 });
        Pieces.Add(new WarshipView { PieceId = "r_ship1", Side = "red", X = 0, Y = 8 });
        Pieces.Add(new PieceView { PieceId = "r_inf1", Side = "red", X = 3, Y = 8, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "r_inf2", Side = "red", X = 5, Y = 8, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "r_inf3", Side = "red", X = 7, Y = 8, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "r_eng1", Side = "red", X = 4, Y = 9, DisplayName = "Công Binh" });
        Pieces.Add(new PieceView { PieceId = "r_aa1", Side = "red", X = 6, Y = 9, DisplayName = "Cao Xạ" });

        // Quân Xanh (Phía trên)
        Pieces.Add(new CommanderView { PieceId = "b_cmd", Side = "blue", X = 5, Y = 0 });
        Pieces.Add(new TankView { PieceId = "b_tank1", Side = "blue", X = 3, Y = 1 });
        Pieces.Add(new TankView { PieceId = "b_tank2", Side = "blue", X = 7, Y = 1 });
        Pieces.Add(new ArtilleryView { PieceId = "b_art1", Side = "blue", X = 2, Y = 2 });
        Pieces.Add(new ArtilleryView { PieceId = "b_art2", Side = "blue", X = 8, Y = 2 });
        Pieces.Add(new AirUnitView { PieceId = "b_air1", Side = "blue", X = 1, Y = 0 });
        Pieces.Add(new AirUnitView { PieceId = "b_air2", Side = "blue", X = 9, Y = 0 });
        Pieces.Add(new WarshipView { PieceId = "b_ship1", Side = "blue", X = 0, Y = 3 });
        Pieces.Add(new PieceView { PieceId = "b_inf1", Side = "blue", X = 3, Y = 3, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "b_inf2", Side = "blue", X = 5, Y = 3, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "b_inf3", Side = "blue", X = 7, Y = 3, DisplayName = "Bộ Binh" });
        Pieces.Add(new PieceView { PieceId = "b_eng1", Side = "blue", X = 4, Y = 2, DisplayName = "Công Binh" });
        Pieces.Add(new PieceView { PieceId = "b_aa1", Side = "blue", X = 6, Y = 2, DisplayName = "Cao Xạ" });

        Invalidate();
    }

    public void MovePiece(string pieceId, int toX, int toY)
    {
        var targetPiece = Pieces.FirstOrDefault(p => p.X == toX && p.Y == toY && p.IsAlive);
        if (targetPiece != null)
        {
            targetPiece.IsAlive = false;
        }

        var piece = Pieces.FirstOrDefault(p => p.PieceId == pieceId);
        if (piece != null)
        {
            piece.X = toX;
            piece.Y = toY;
            CurrentTurn = CurrentTurn == "red" ? "blue" : "red";
            SelectedPiece = null;
            Invalidate();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        cellWidth = Math.Max(30, (Width - marginX * 2) / (COLS - 1));
        cellHeight = Math.Max(30, (Height - marginY * 2) / (ROWS - 1));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 1. Vẽ Vùng Biển Hải quân (Cột 0 và Cột 10)
        using (var seaBrush = new SolidBrush(Color.FromArgb(50, 41, 128, 185)))
        {
            g.FillRectangle(seaBrush, marginX - cellWidth / 2, marginY, cellWidth, (ROWS - 1) * cellHeight);
            g.FillRectangle(seaBrush, marginX + (COLS - 2) * cellWidth + cellWidth / 2, marginY, cellWidth, (ROWS - 1) * cellHeight);
        }

        // 2. Vẽ Sông ranh giới (Giữa Hàng 5 và 6)
        using (var riverBrush = new SolidBrush(Color.FromArgb(70, 52, 152, 219)))
        {
            int riverY = marginY + 5 * cellHeight;
            g.FillRectangle(riverBrush, marginX, riverY, (COLS - 1) * cellWidth, cellHeight);

            using var font = new Font("Segoe UI", 11F, FontStyle.Bold);
            using var riverTextBrush = new SolidBrush(Color.FromArgb(41, 128, 185));
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("— — — — — S Ô N G   T R I Ề U   D Â N G — — — — —", font, riverTextBrush,
                new RectangleF(marginX, riverY, (COLS - 1) * cellWidth, cellHeight), sf);
        }

        // 3. Vẽ lưới toạ độ bàn cờ
        using (var linePen = new Pen(Color.FromArgb(120, 100, 80), 1.5f))
        {
            // Các đường dọc
            for (int col = 0; col < COLS; col++)
            {
                int x = marginX + col * cellWidth;
                g.DrawLine(linePen, x, marginY, x, marginY + (ROWS - 1) * cellHeight);
            }

            // Các đường ngang
            for (int row = 0; row < ROWS; row++)
            {
                int y = marginY + row * cellHeight;
                g.DrawLine(linePen, marginX, y, marginX + (COLS - 1) * cellWidth, y);
            }
        }

        // 4. Highlight ô đang hover
        if (hoverPoint.HasValue)
        {
            var p = hoverPoint.Value;
            var rect = GetCellRect(p.X, p.Y);
            using var hoverBrush = new SolidBrush(Color.FromArgb(60, 46, 204, 113));
            g.FillEllipse(hoverBrush, rect);
        }

        // 5. Highlight ô quân đang được chọn
        if (SelectedPiece != null && SelectedPiece.IsAlive)
        {
            var rect = GetCellRect(SelectedPiece.X, SelectedPiece.Y);
            using var selectPen = new Pen(Color.FromArgb(241, 196, 15), 3);
            selectPen.DashStyle = DashStyle.Dash;
            g.DrawEllipse(selectPen, rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8);
        }

        // 6. Vẽ tất cả các quân cờ còn sống
        foreach (var piece in Pieces.Where(p => p.IsAlive))
        {
            var rect = GetCellRect(piece.X, piece.Y);
            piece.Draw(g, rect);
        }
    }

    private Rectangle GetCellRect(int col, int row)
    {
        int x = marginX + col * cellWidth;
        int y = marginY + row * cellHeight;
        int size = (int)(Math.Min(cellWidth, cellHeight) * 0.85);
        return new Rectangle(x - size / 2, y - size / 2, size, size);
    }

    private Point? ScreenToGrid(int mouseX, int mouseY)
    {
        for (int col = 0; col < COLS; col++)
        {
            for (int row = 0; row < ROWS; row++)
            {
                int cx = marginX + col * cellWidth;
                int cy = marginY + row * cellHeight;
                int distSq = (mouseX - cx) * (mouseX - cx) + (mouseY - cy) * (mouseY - cy);
                int radius = (int)(Math.Min(cellWidth, cellHeight) * 0.45);
                if (distSq <= radius * radius)
                {
                    return new Point(col, row);
                }
            }
        }
        return null;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pt = ScreenToGrid(e.X, e.Y);
        if (pt != hoverPoint)
        {
            hoverPoint = pt;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoverPoint = null;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var pt = ScreenToGrid(e.X, e.Y);
        if (!pt.HasValue) return;

        int gx = pt.Value.X;
        int gy = pt.Value.Y;

        var clickedPiece = Pieces.FirstOrDefault(p => p.X == gx && p.Y == gy && p.IsAlive);

        if (SelectedPiece == null)
        {
            // Chọn quân của phe mình
            if (clickedPiece != null && clickedPiece.Side == CurrentTurn)
            {
                SelectedPiece = clickedPiece;
                OnPieceSelected?.Invoke(SelectedPiece);
                Invalidate();
            }
        }
        else
        {
            // Đã chọn quân trước đó:
            if (clickedPiece != null && clickedPiece.Side == SelectedPiece.Side)
            {
                // Chọn quân khác cùng phe
                SelectedPiece = clickedPiece;
                OnPieceSelected?.Invoke(SelectedPiece);
                Invalidate();
            }
            else
            {
                // Di chuyển quân tới ô gx, gy (hoặc ăn quân địch)
                var movingPiece = SelectedPiece;
                MovePiece(movingPiece.PieceId, gx, gy);
                OnPieceMoved?.Invoke(movingPiece, gx, gy);
            }
        }
    }
}
