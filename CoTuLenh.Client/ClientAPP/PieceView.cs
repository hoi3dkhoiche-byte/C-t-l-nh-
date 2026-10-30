using System.Drawing;
using System.Drawing.Drawing2D;

namespace ClientAPP;

public enum PieceKind
{
    Commander,    // Tư lệnh (*)
    Infantry,     // Bộ binh (B)
    Tank,         // Xe tăng (T)
    Militia,      // Dân quân tóc dài (D)
    Engineer,     // Công binh (C)
    Artillery,    // Pháo binh (P)
    AntiAir,      // Cao xạ (F)
    Rocket,       // Tên lửa (L)
    AirForce,     // Không quân (K)
    Warship       // Hải quân (H)
}

public class PieceView
{
    public string PieceId { get; set; } = "";
    public PieceKind Kind { get; set; } = PieceKind.Infantry;
    public string Side { get; set; } = "red"; // "red" or "blue"
    public int X { get; set; }
    public int Y { get; set; }
    public int Score { get; set; } = 10;
    public bool IsAlive { get; set; } = true;
    public string DisplayName { get; set; } = "Bộ Binh";
    public string Notation { get; set; } = "B";
    public bool IsHero { get; set; } = false; // Quân anh hùng (được cộng thêm 1 nấc đi/ăn và 10đ)
    public Point? AirportLocation { get; set; } // Vị trí sân bay ban đầu (đối với Không quân)

    public Color PrimaryColor => Side == "red" ? Color.FromArgb(192, 57, 43) : Color.FromArgb(41, 128, 185);
    public Color SecondaryColor => Side == "red" ? Color.FromArgb(146, 43, 33) : Color.FromArgb(27, 79, 114);
    public Color AccentColor => Side == "red" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(52, 152, 219);

    public virtual void Draw(Graphics g, Rectangle rect)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 1. Đổ bóng quân cờ
        using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
        {
            g.FillEllipse(shadowBrush, rect.X + 2, rect.Y + 3, rect.Width - 4, rect.Height - 4);
        }

        // 2. Thân quân cờ gradient tròn
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            using (var gradientBrush = new LinearGradientBrush(rect, AccentColor, SecondaryColor, LinearGradientMode.ForwardDiagonal))
            {
                g.FillPath(gradientBrush, path);
            }

            // Viền kim loại ngoài
            Color rimColor = IsHero ? Color.FromArgb(241, 196, 15) : (Side == "red" ? Color.FromArgb(218, 165, 32) : Color.FromArgb(189, 195, 199));
            using (var borderPen = new Pen(rimColor, IsHero ? 2.5f : 2.0f))
            {
                g.DrawPath(borderPen, path);
            }
        }

        // 3. Vòng tròn viền trong
        using (var innerPen = new Pen(Color.FromArgb(190, 255, 255, 255), 1.2f))
        {
            g.DrawEllipse(innerPen, rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
        }

        // 4. Vẽ biểu tượng quân cờ (Logo/Icon vector ở chính giữa quân)
        DrawPieceIcon(g, rect);

        // 5. Huy hiệu Quân Anh Hùng (nếu có)
        if (IsHero)
        {
            using (var heroBrush = new SolidBrush(Color.FromArgb(241, 196, 15)))
            using (var heroFont = new Font("Segoe UI", 7F, FontStyle.Bold))
            {
                g.FillEllipse(heroBrush, rect.Right - 12, rect.Y, 12, 12);
                g.DrawString("★", heroFont, Brushes.Black, rect.Right - 12, rect.Y - 1);
            }
        }
    }

    private void DrawPieceIcon(Graphics g, Rectangle rect)
    {
        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2; // Căn chính xác giữa tâm quân cờ

        using var iconPen = new Pen(Color.White, 2.0f);
        using var iconBrush = new SolidBrush(Color.White);

        switch (Kind)
        {
            case PieceKind.Commander: // Ngôi sao Tư Lệnh lớn nổi bật
                DrawStar(g, cx, cy, 13, 6.5f, Color.FromArgb(255, 215, 0));
                break;

            case PieceKind.Infantry: // Mũ sắt Bộ Binh
                g.FillPie(iconBrush, cx - 10, cy - 8, 20, 16, 180, 180);
                g.DrawLine(iconPen, cx - 12, cy, cx + 12, cy);
                g.DrawLine(iconPen, cx - 5, cy + 3, cx + 5, cy + 3);
                break;

            case PieceKind.Tank: // Xe Tăng
                g.FillRectangle(iconBrush, cx - 11, cy - 1, 22, 7);
                g.FillEllipse(iconBrush, cx - 6, cy - 7, 12, 8);
                g.DrawLine(new Pen(Color.White, 2.5f), cx, cy - 3, cx + 13, cy - 5);
                // Bánh xích
                g.DrawEllipse(new Pen(Color.FromArgb(120, 0, 0, 0), 1.3f), cx - 9, cy, 5, 5);
                g.DrawEllipse(new Pen(Color.FromArgb(120, 0, 0, 0), 1.3f), cx - 2.5f, cy, 5, 5);
                g.DrawEllipse(new Pen(Color.FromArgb(120, 0, 0, 0), 1.3f), cx + 4, cy, 5, 5);
                break;

            case PieceKind.Militia: // Dân Quân Tóc Dài (Nón lá & dải tóc)
                Point[] hat = { new Point(cx, cy - 10), new Point(cx - 11, cy), new Point(cx + 11, cy) };
                g.FillPolygon(iconBrush, hat);
                // Dải tóc dài
                using (var hairPen = new Pen(Color.White, 2.2f))
                {
                    g.DrawArc(hairPen, cx + 2, cy, 7, 10, 270, 180);
                }
                break;

            case PieceKind.Engineer: // Công Binh (Cầu phao & sóng nước)
                g.DrawLine(new Pen(Color.White, 2.5f), cx - 11, cy - 3, cx + 11, cy - 3);
                g.DrawLine(iconPen, cx - 8, cy - 3, cx - 8, cy + 3);
                g.DrawLine(iconPen, cx, cy - 3, cx, cy + 3);
                g.DrawLine(iconPen, cx + 8, cy - 3, cx + 8, cy + 3);
                g.DrawArc(iconPen, cx - 10, cy + 2, 10, 5, 0, 180);
                g.DrawArc(iconPen, cx, cy + 2, 10, 5, 0, 180);
                break;

            case PieceKind.Artillery: // Pháo Binh (Nòng pháo dã chiến)
                g.FillEllipse(iconBrush, cx - 8, cy - 1, 8, 8); // Bánh xe
                g.DrawLine(new Pen(Color.White, 3f), cx - 6, cy + 1, cx + 11, cy - 8); // Nòng pháo
                g.DrawLine(iconPen, cx - 7, cy + 3, cx - 2, cy + 6); // Chân pháo
                break;

            case PieceKind.AntiAir: // Cao Xạ (Nòng pháo chĩa lên trời 55 độ)
                g.FillRectangle(iconBrush, cx - 8, cy + 2, 16, 5);
                g.DrawLine(new Pen(Color.White, 2.4f), cx - 3, cy + 2, cx + 6, cy - 9);
                g.DrawLine(new Pen(Color.White, 2.4f), cx + 1, cy + 2, cx + 10, cy - 9);
                break;

            case PieceKind.Rocket: // Dàn Tên Lửa Phòng Không
                g.FillRectangle(iconBrush, cx - 8, cy + 3, 16, 5);
                g.DrawLine(new Pen(Color.White, 3.5f), cx - 4, cy + 3, cx + 7, cy - 7);
                g.FillPolygon(iconBrush, new[] { new Point(cx + 9, cy - 9), new Point(cx + 4, cy - 6), new Point(cx + 6, cy - 11) });
                break;

            case PieceKind.AirForce: // Không Quân (Máy bay phản lực)
                Point[] plane = {
                    new Point(cx, cy - 10),
                    new Point(cx + 2, cy - 4),
                    new Point(cx + 11, cy + 1),
                    new Point(cx + 3, cy + 2),
                    new Point(cx + 4, cy + 7),
                    new Point(cx, cy + 5),
                    new Point(cx - 4, cy + 7),
                    new Point(cx - 3, cy + 2),
                    new Point(cx - 11, cy + 1),
                    new Point(cx - 2, cy - 4)
                };
                g.FillPolygon(iconBrush, plane);
                break;

            case PieceKind.Warship: // Tàu Chiến (Chiến hạm & sóng nước)
                Point[] ship = {
                    new Point(cx - 11, cy + 2),
                    new Point(cx + 9, cy + 2),
                    new Point(cx + 11, cy - 2),
                    new Point(cx - 9, cy - 2)
                };
                g.FillPolygon(iconBrush, ship);
                g.FillRectangle(iconBrush, cx - 4, cy - 7, 8, 5); // Đài chỉ huy
                g.DrawLine(iconPen, cx, cy - 7, cx, cy - 11); // Cột cờ/radar
                // Sóng nước
                g.DrawArc(iconPen, cx - 10, cy + 3, 10, 5, 0, 180);
                g.DrawArc(iconPen, cx, cy + 3, 10, 5, 0, 180);
                break;
        }
    }

    private static void DrawStar(Graphics g, float cx, float cy, float rOuter, float rInner, Color starColor)
    {
        PointF[] pts = new PointF[10];
        double step = Math.PI / 5;
        double startAngle = -Math.PI / 2;

        for (int i = 0; i < 10; i++)
        {
            double r = (i % 2 == 0) ? rOuter : rInner;
            double angle = startAngle + i * step;
            pts[i] = new PointF((float)(cx + r * Math.Cos(angle)), (float)(cy + r * Math.Sin(angle)));
        }

        using var brush = new SolidBrush(starColor);
        using var pen = new Pen(Color.FromArgb(200, 140, 0), 1.2f);
        g.FillPolygon(brush, pts);
        g.DrawPolygon(pen, pts);
    }
}
