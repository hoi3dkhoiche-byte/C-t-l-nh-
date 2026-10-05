using System.Drawing;
using System.Drawing.Drawing2D;

namespace ClientAPP;

public class PieceView
{
    public string PieceId { get; set; } = "";
    public string PieceType { get; set; } = "infantry";
    public string Side { get; set; } = "red"; // "red" or "blue"
    public int X { get; set; }
    public int Y { get; set; }
    public int Score { get; set; } = 10;
    public bool IsAlive { get; set; } = true;
    public string DisplayName { get; set; } = "Bộ Binh";

    public Color PrimaryColor => Side == "red" ? Color.FromArgb(192, 57, 43) : Color.FromArgb(41, 128, 185);
    public Color BorderColor => Side == "red" ? Color.FromArgb(146, 43, 33) : Color.FromArgb(27, 79, 114);

    public virtual void Draw(Graphics g, Rectangle rect)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Vẽ nền quân cờ hình tròn có đổ bóng nhẹ
        using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, rect.X + 2, rect.Y + 3, rect.Width - 4, rect.Height - 4);
        }

        // Vẽ thân quân cờ gradient
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            using (var gradientBrush = new LinearGradientBrush(rect, PrimaryColor, ControlPaint.Dark(PrimaryColor, 0.15f), LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(gradientBrush, path);
            }
            using (var borderPen = new Pen(BorderColor, 2))
            {
                g.DrawPath(borderPen, path);
            }
        }

        // Vẽ vòng tròn viền trong
        using (var innerPen = new Pen(Color.FromArgb(180, 255, 255, 255), 1))
        {
            g.DrawEllipse(innerPen, rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12);
        }

        // Vẽ chữ tên quân
        using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
        using (var textBrush = new SolidBrush(Color.White))
        using (var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        })
        {
            g.DrawString(DisplayName, font, textBrush, rect, format);
        }

        DrawExtra(g, rect);
    }

    public virtual void DrawExtra(Graphics g, Rectangle rect)
    {
        // Mặc định không vẽ thêm gì
    }
}

public class CommanderView : PieceView
{
    public CommanderView()
    {
        PieceType = "commander";
        DisplayName = "Tư Lệnh";
        Score = 100;
    }

    public override void DrawExtra(Graphics g, Rectangle rect)
    {
        // Tư Lệnh có viền vàng kim loại nổi bật (Gold frame)
        using (var pen = new Pen(Color.FromArgb(241, 196, 15), 3))
        {
            g.DrawEllipse(pen, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
        }
    }
}

public class WarshipView : PieceView
{
    public WarshipView()
    {
        PieceType = "warship";
        DisplayName = "Tàu Chiến";
        Score = 80;
    }

    public override void DrawExtra(Graphics g, Rectangle rect)
    {
        // Hiệu ứng sóng nước hải quân
        using (var wavePen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.5f))
        {
            int waveY = rect.Y + (int)(rect.Height * 0.75);
            g.DrawArc(wavePen, rect.X + 10, waveY, 12, 6, 0, 180);
            g.DrawArc(wavePen, rect.X + 22, waveY, 12, 6, 0, 180);
        }
    }
}

public class AirUnitView : PieceView
{
    public AirUnitView()
    {
        PieceType = "aircraft";
        DisplayName = "Máy Bay";
        Score = 40;
    }

    public override void DrawExtra(Graphics g, Rectangle rect)
    {
        // Điểm nhấn cánh máy bay
        using (var wingPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
        {
            int centerY = rect.Y + rect.Height / 2;
            g.DrawLine(wingPen, rect.X + 4, centerY - 4, rect.X + 10, centerY);
            g.DrawLine(wingPen, rect.Right - 4, centerY - 4, rect.Right - 10, centerY);
        }
    }
}

public class TankView : PieceView
{
    public TankView()
    {
        PieceType = "tank";
        DisplayName = "Xe Tăng";
        Score = 20;
    }
}

public class ArtilleryView : PieceView
{
    public ArtilleryView()
    {
        PieceType = "artillery";
        DisplayName = "Pháo Binh";
        Score = 30;
    }
}
