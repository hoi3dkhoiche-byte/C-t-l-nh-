using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClientAPP;

public enum CaptureMode
{
    Occupy,     // Thế chỗ mục tiêu (Chiếm cứ vị trí quân địch)
    StandStill  // Bắn tại chỗ / Oanh tạc bay về (Tiêu diệt mục tiêu và giữ nguyên vị trí cũ)
}

public class BoardControl : UserControl
{
    public const int COLS = 11; // X: 0 đến 10
    public const int ROWS = 12; // Y: 0 đến 11

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public List<PieceView> Pieces { get; private set; } = new();

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public PieceView? SelectedPiece { get; private set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public List<Point> ValidMoves { get; private set; } = new();

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string CurrentTurn { get; set; } = "red";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string PlayerSide { get; set; } = "red"; // Phe của người chơi trên máy này

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public CaptureMode CurrentCaptureMode { get; set; } = CaptureMode.Occupy;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public bool MoveStackTogether { get; set; } = true; // Khi quân cõng di chuyển, có cõng cả cụm đi cùng

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public PieceView? InspectedFirePiece { get; private set; } // Quân đang soi vành đai lửa bằng chuột phải

    public event Action<PieceView, int, int, PieceView?>? OnPieceMoved;
    public event Action<PieceView?>? OnPieceSelected;
    public event Action<CaptureMode>? OnCaptureModeChanged;
    public event Action<List<PieceView>>? OnStackedGroupSelected;
    public event Action<PieceView?>? OnFireRingInspected;

    private Point? hoverGrid = null;
    private int cellWidth = 50;
    private int cellHeight = 50;
    private int marginX = 45;
    private int marginY = 35;
    private Image? terrainImage = null;

    // Hiệu ứng nổ 2D & nhấp nháy vành đai lửa trong 2 giây
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 40 };
    private bool isAnimatingExplosion = false;
    private DateTime animationStartTime;
    private const int ExplosionDurationMs = 2000;
    private Point? explosionGridPos = null;
    private List<PieceView> blinkingFireRingPieces = new();
    private Action? pendingPostExplosionAction = null;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public bool IsAnimating => isAnimatingExplosion;

    public BoardControl()
    {
        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(24, 32, 38); // Màu nền quân sự cao cấp bao quanh bản đồ
        this.Size = new Size(620, 700);
        terrainImage = LoadTerrainMap();
        InitializeStandardPieces();

        animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        double elapsedMs = (DateTime.Now - animationStartTime).TotalMilliseconds;
        if (elapsedMs >= ExplosionDurationMs)
        {
            animationTimer.Stop();
            isAnimatingExplosion = false;
            explosionGridPos = null;
            blinkingFireRingPieces.Clear();

            var callback = pendingPostExplosionAction;
            pendingPostExplosionAction = null;
            Invalidate();
            callback?.Invoke();
            return;
        }

        Invalidate();
    }

    private static Image? LoadTerrainMap()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string fileName = "cotulenh_board_11x12.png";

        try
        {
            string rootFile = @"d:\clone\C-t-l-nh-\cotulenh_board_11x12.png";
            string localFile = Path.Combine(baseDir, fileName);
            if (!File.Exists(localFile) && File.Exists(rootFile))
            {
                File.Copy(rootFile, localFile, true);
            }
        }
        catch { }

        string[] searchPaths = new[]
        {
            Path.Combine(baseDir, fileName),
            Path.Combine(baseDir, "..", "..", "..", fileName),
            Path.Combine(baseDir, "..", "..", "..", "..", fileName),
            fileName,
            @"d:\clone\C-t-l-nh-\cotulenh_board_11x12.png",
            @"C:\Users\bonho\.gemini\antigravity\brain\cec71c46-79ec-44dc-8d06-d75f2d3f99a6\.user_uploaded\media_1793298595885_ba0e1dae.jpg"
        };

        foreach (var p in searchPaths)
        {
            try
            {
                if (File.Exists(p))
                {
                    return Image.FromFile(p);
                }
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Bố trí 19 quân cờ mỗi bên chuẩn xác theo sơ đồ chiến thuật Cờ Tư Lệnh
    /// </summary>
    public void InitializeStandardPieces()
    {
        Pieces.Clear();
        SelectedPiece = null;
        ValidMoves.Clear();

        // =========================================================================
        // 1. PHE ĐỎ (RED) - Nửa dưới bàn cờ (Y: 0 -> 4)
        // =========================================================================
        // Y = 0: Tư Lệnh
        Pieces.Add(new PieceView { PieceId = "r_cmd", Kind = PieceKind.Commander, Side = "red", X = 6, Y = 0, DisplayName = "Tư Lệnh", Notation = "★", Score = 100 });

        // Y = 1: Hải quân, Không quân, Sở chỉ huy
        Pieces.Add(new PieceView { PieceId = "r_ship1", Kind = PieceKind.Warship, Side = "red", X = 1, Y = 1, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "r_air1", Kind = PieceKind.AirForce, Side = "red", X = 4, Y = 1, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(4, 1) });
        Pieces.Add(new PieceView { PieceId = "r_hq1", Kind = PieceKind.Headquarter, Side = "red", X = 5, Y = 1, DisplayName = "Sở Chỉ Huy", Notation = "S", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_hq2", Kind = PieceKind.Headquarter, Side = "red", X = 7, Y = 1, DisplayName = "Sở Chỉ Huy", Notation = "S", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_air2", Kind = PieceKind.AirForce, Side = "red", X = 8, Y = 1, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(8, 1) });

        // Y = 2: Cao xạ, Tên lửa
        Pieces.Add(new PieceView { PieceId = "r_aa1", Kind = PieceKind.AntiAir, Side = "red", X = 3, Y = 2, DisplayName = "Cao Xạ", Notation = "F", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_rkt", Kind = PieceKind.Rocket, Side = "red", X = 6, Y = 2, DisplayName = "Tên Lửa", Notation = "L", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "r_aa2", Kind = PieceKind.AntiAir, Side = "red", X = 9, Y = 2, DisplayName = "Cao Xạ", Notation = "F", Score = 10 });

        // Y = 3: Hải quân, Pháo binh, Xe tăng
        Pieces.Add(new PieceView { PieceId = "r_ship2", Kind = PieceKind.Warship, Side = "red", X = 2, Y = 3, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "r_art1", Kind = PieceKind.Artillery, Side = "red", X = 4, Y = 3, DisplayName = "Pháo Binh", Notation = "P", Score = 30 });
        Pieces.Add(new PieceView { PieceId = "r_tank1", Kind = PieceKind.Tank, Side = "red", X = 5, Y = 3, DisplayName = "Xe Tăng", Notation = "T", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "r_tank2", Kind = PieceKind.Tank, Side = "red", X = 7, Y = 3, DisplayName = "Xe Tăng", Notation = "T", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "r_art2", Kind = PieceKind.Artillery, Side = "red", X = 8, Y = 3, DisplayName = "Pháo Binh", Notation = "P", Score = 30 });

        // Y = 4: Bộ binh, Công binh, Dân quân tóc dài
        Pieces.Add(new PieceView { PieceId = "r_inf1", Kind = PieceKind.Infantry, Side = "red", X = 2, Y = 4, DisplayName = "Bộ Binh", Notation = "B", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_eng1", Kind = PieceKind.Engineer, Side = "red", X = 3, Y = 4, DisplayName = "Công Binh", Notation = "C", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_mil", Kind = PieceKind.Militia, Side = "red", X = 6, Y = 4, DisplayName = "Dân Quân", Notation = "D", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_eng2", Kind = PieceKind.Engineer, Side = "red", X = 9, Y = 4, DisplayName = "Công Binh", Notation = "C", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "r_inf2", Kind = PieceKind.Infantry, Side = "red", X = 10, Y = 4, DisplayName = "Bộ Binh", Notation = "B", Score = 10 });

        // =========================================================================
        // 2. PHE XANH (BLUE) - Nửa trên bàn cờ (Y: 7 -> 11, đối xứng trục qua Y=5.5)
        // =========================================================================
        // Y = 11: Tư Lệnh
        Pieces.Add(new PieceView { PieceId = "b_cmd", Kind = PieceKind.Commander, Side = "blue", X = 6, Y = 11, DisplayName = "Tư Lệnh", Notation = "★", Score = 100 });

        // Y = 10: Hải quân, Không quân, Sở chỉ huy
        Pieces.Add(new PieceView { PieceId = "b_ship1", Kind = PieceKind.Warship, Side = "blue", X = 1, Y = 10, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "b_air1", Kind = PieceKind.AirForce, Side = "blue", X = 4, Y = 10, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(4, 10) });
        Pieces.Add(new PieceView { PieceId = "b_hq1", Kind = PieceKind.Headquarter, Side = "blue", X = 5, Y = 10, DisplayName = "Sở Chỉ Huy", Notation = "S", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_hq2", Kind = PieceKind.Headquarter, Side = "blue", X = 7, Y = 10, DisplayName = "Sở Chỉ Huy", Notation = "S", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_air2", Kind = PieceKind.AirForce, Side = "blue", X = 8, Y = 10, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(8, 10) });

        // Y = 9: Cao xạ, Tên lửa
        Pieces.Add(new PieceView { PieceId = "b_aa1", Kind = PieceKind.AntiAir, Side = "blue", X = 3, Y = 9, DisplayName = "Cao Xạ", Notation = "F", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_rkt", Kind = PieceKind.Rocket, Side = "blue", X = 6, Y = 9, DisplayName = "Tên Lửa", Notation = "L", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "b_aa2", Kind = PieceKind.AntiAir, Side = "blue", X = 9, Y = 9, DisplayName = "Cao Xạ", Notation = "F", Score = 10 });

        // Y = 8: Hải quân, Pháo binh, Xe tăng
        Pieces.Add(new PieceView { PieceId = "b_ship2", Kind = PieceKind.Warship, Side = "blue", X = 2, Y = 8, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "b_art1", Kind = PieceKind.Artillery, Side = "blue", X = 4, Y = 8, DisplayName = "Pháo Binh", Notation = "P", Score = 30 });
        Pieces.Add(new PieceView { PieceId = "b_tank1", Kind = PieceKind.Tank, Side = "blue", X = 5, Y = 8, DisplayName = "Xe Tăng", Notation = "T", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "b_tank2", Kind = PieceKind.Tank, Side = "blue", X = 7, Y = 8, DisplayName = "Xe Tăng", Notation = "T", Score = 20 });
        Pieces.Add(new PieceView { PieceId = "b_art2", Kind = PieceKind.Artillery, Side = "blue", X = 8, Y = 8, DisplayName = "Pháo Binh", Notation = "P", Score = 30 });

        // Y = 7: Bộ binh, Công binh, Dân quân tóc dài
        Pieces.Add(new PieceView { PieceId = "b_inf1", Kind = PieceKind.Infantry, Side = "blue", X = 2, Y = 7, DisplayName = "Bộ Binh", Notation = "B", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_eng1", Kind = PieceKind.Engineer, Side = "blue", X = 3, Y = 7, DisplayName = "Công Binh", Notation = "C", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_mil", Kind = PieceKind.Militia, Side = "blue", X = 6, Y = 7, DisplayName = "Dân Quân", Notation = "D", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_eng2", Kind = PieceKind.Engineer, Side = "blue", X = 9, Y = 7, DisplayName = "Công Binh", Notation = "C", Score = 10 });
        Pieces.Add(new PieceView { PieceId = "b_inf2", Kind = PieceKind.Infantry, Side = "blue", X = 10, Y = 7, DisplayName = "Bộ Binh", Notation = "B", Score = 10 });

        Invalidate();
    }

    /// <summary>
    /// Kiểm tra ô có phải là Sở chỉ huy (Headquarter)
    /// </summary>
    public static bool IsHeadquarter(int x, int y)
    {
        return (x == 5 && y == 1) || (x == 7 && y == 1) ||
               (x == 5 && y == 10) || (x == 7 && y == 10);
    }

    /// <summary>
    /// Kiểm tra ô có phải là vùng biển (Hải phận)
    /// </summary>
    public static bool IsSea(int x)
    {
        return x < 2; // Cột 0 và 1 là biển thuần túy; cột 2 là duyên hải / ranh giới bờ
    }

    /// <summary>
    /// Kiểm tra ô có phải là ngầm cạn qua sông tại X=5 hoặc X=7
    /// </summary>
    public static bool IsShallowFord(int x)
    {
        return x == 5 || x == 7;
    }

    public List<PieceView> GetPiecesAt(int x, int y) => Pieces.Where(p => p.X == x && p.Y == y && p.IsAlive).ToList();

    public static bool IsCarrier(PieceKind kind)
    {
        return kind == PieceKind.Tank || kind == PieceKind.AirForce || kind == PieceKind.Warship;
    }

    public static bool CanCarry(PieceKind carrier, PieceKind passenger)
    {
        if (carrier == PieceKind.Headquarter)
        {
            return passenger == PieceKind.Commander;
        }
        if (carrier == PieceKind.Tank)
        {
            return passenger == PieceKind.Commander || passenger == PieceKind.Infantry || passenger == PieceKind.Militia;
        }
        if (carrier == PieceKind.AirForce)
        {
            return passenger == PieceKind.Commander || passenger == PieceKind.Infantry || passenger == PieceKind.Militia ||
                   passenger == PieceKind.Tank;
        }
        if (carrier == PieceKind.Warship)
        {
            return passenger == PieceKind.Commander || passenger == PieceKind.Infantry || passenger == PieceKind.Militia ||
                   passenger == PieceKind.Tank || passenger == PieceKind.AirForce;
        }
        return false;
    }

    public static bool CanStack(PieceView movingPiece, List<PieceView> targetOccupants)
    {
        if (targetOccupants == null || targetOccupants.Count == 0) return false;
        if (targetOccupants.Any(p => p.Side != movingPiece.Side)) return false;

        // Sở chỉ huy chỉ cho Tư lệnh ẩn nấp (tối đa 2 quân)
        if (targetOccupants.Any(p => p.Kind == PieceKind.Headquarter))
        {
            return targetOccupants.Count == 1 && movingPiece.Kind == PieceKind.Commander;
        }

        // Xe tăng chỉ chở tối đa 1 quân (Tổng 2 quân)
        var carrier = targetOccupants.FirstOrDefault(p => IsCarrier(p.Kind));
        if (carrier != null)
        {
            int maxTotal = carrier.Kind == PieceKind.Tank ? 2 : 3;
            if (targetOccupants.Count >= maxTotal) return false;
            return CanCarry(carrier.Kind, movingPiece.Kind);
        }

        if (IsCarrier(movingPiece.Kind))
        {
            int maxTotal = movingPiece.Kind == PieceKind.Tank ? 2 : 3;
            if (targetOccupants.Count >= maxTotal) return false;
            return targetOccupants.All(p => CanCarry(movingPiece.Kind, p.Kind));
        }

        return false;
    }

    public static bool HasMultipleCaptureModes(PieceKind kind)
    {
        // Không quân tự động chọn chế độ ăn theo tình huống (Thế chỗ / Cảm tử / Ném bom), không bật nút đổi thủ công
        return kind == PieceKind.Artillery || kind == PieceKind.Warship || kind == PieceKind.Tank;
    }

    public static bool HasFireRing(PieceKind kind)
    {
        return kind == PieceKind.AntiAir || kind == PieceKind.Rocket || kind == PieceKind.Warship || kind == PieceKind.Artillery;
    }

    public static float GetFireRingRadius(PieceKind kind)
    {
        return kind switch
        {
            PieceKind.AntiAir => 1.0f,   // Cao xạ: 1 nấc
            PieceKind.Rocket => 2.0f,    // Tên lửa: 2 nấc
            PieceKind.Warship => 1.0f,   // Hải quân: 1 nấc
            PieceKind.Artillery => 3.0f, // Pháo binh: 3 nấc
            _ => 0f
        };
    }

    public void ToggleCaptureMode()
    {
        CurrentCaptureMode = (CurrentCaptureMode == CaptureMode.Occupy) ? CaptureMode.StandStill : CaptureMode.Occupy;
        OnCaptureModeChanged?.Invoke(CurrentCaptureMode);
        Invalidate();
    }

    public void SelectSpecificPiece(PieceView piece)
    {
        if (isAnimatingExplosion || !piece.IsAlive) return;
        SelectedPiece = piece;
        ValidMoves = GetValidMoves(piece);
        OnPieceSelected?.Invoke(piece);
        Invalidate();
    }

    private static readonly int[] DirX4 = { 0, 0, -1, 1 };
    private static readonly int[] DirY4 = { 1, -1, 0, 0 };
    private static readonly int[] DirX8 = { 0, 0, -1, 1, -1, 1, -1, 1 };
    private static readonly int[] DirY8 = { 1, -1, 0, 0, 1, 1, -1, -1 };

    /// <summary>
    /// Tính toán tất cả các nước đi hợp lệ cho quân cờ dựa trên Luật Cờ Tư Lệnh
    /// </summary>
    public List<Point> GetValidMoves(PieceView piece)
    {
        var moves = new List<Point>();
        if (!piece.IsAlive) return moves;

        int bonus = piece.IsHero ? 1 : 0; // Quân anh hùng được cộng thêm 1 nấc di chuyển/ăn quân

        switch (piece.Kind)
        {
            case PieceKind.Headquarter: // Sở Chỉ Huy
                // Nguyên tắc bất động; nếu là quân cuối cùng bảo vệ Tư lệnh (được phong Anh hùng) -> cơ động như Bộ binh anh hùng
                if (piece.IsHero)
                {
                    AddStepMoves(piece, moves, 2, allowDiagonal: true, canEnterSea: false, allowDeepRiver: true);
                }
                break;

            case PieceKind.Commander: // Tư Lệnh
                // Đi theo trục dọc ngang không hạn chế (nếu là anh hùng đi cả chéo 45 độ), miễn không vướng quân chắn
                // Ăn quân đối phương: phạm vi 1 nấc (+1 nếu anh hùng), hoặc ăn Tư lệnh đối phương nếu mặt đối mặt không vật cản
                int cmdDirs = piece.IsHero ? 8 : 4;
                for (int i = 0; i < cmdDirs; i++)
                {
                    for (int step = 1; step < Math.Max(COLS, ROWS); step++)
                    {
                        int tx = piece.X + DirX8[i] * step;
                        int ty = piece.Y + DirY8[i] * step;
                        if (!InBounds(tx, ty)) break;
                        if (IsSea(tx)) break; // Tư lệnh không tự đi xuống biển (chỉ lên tàu chiến)

                        var occs = GetPiecesAt(tx, ty);
                        if (occs.Count == 0)
                        {
                            moves.Add(new Point(tx, ty));
                        }
                        else
                        {
                            var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                            if (enemy != null)
                            {
                                bool isExposedEnemyCmd = (i < 4) && enemy.Kind == PieceKind.Commander &&
                                    !occs.Any(p => p.Side == enemy.Side && IsCarrier(p.Kind));
                                if (step <= 1 + bonus || isExposedEnemyCmd)
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                            }
                            else if (CanStack(piece, occs))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            break;
                        }
                    }
                }
                break;

            case PieceKind.Infantry: // Bộ Binh
                // Đi và ăn thẳng dọc ngang từng đoạn 1 (được vượt sông ở mọi đoạn)
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false, allowDeepRiver: true);
                if (piece.X == 2)
                {
                    var seaEnemies = GetPiecesAt(1, piece.Y).Where(p => p.Side != piece.Side).ToList();
                    if (seaEnemies.Count > 0) moves.Add(new Point(1, piece.Y));
                }
                break;

            case PieceKind.Militia: // Dân Quân Tóc Dài
                // Đi tiến, lùi, ngang VÀ đi/ăn chéo 45 độ từng đoạn 1 (được vượt sông ở mọi đoạn)
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: true, canEnterSea: false, allowDeepRiver: true);
                if (piece.X == 2)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ty = piece.Y + dy;
                        if (!InBounds(1, ty)) continue;
                        var seaEnemies = GetPiecesAt(1, ty).Where(p => p.Side != piece.Side).ToList();
                        if (seaEnemies.Count > 0) moves.Add(new Point(1, ty));
                    }
                }
                break;

            case PieceKind.Engineer: // Công Binh
                // Đi tiến, lùi, ngang từng đoạn 1; vượt sông mọi vị trí và bắc cầu cho hỏa khí nặng
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false, allowDeepRiver: true);
                if (piece.X == 2)
                {
                    var seaEnemies = GetPiecesAt(1, piece.Y).Where(p => p.Side != piece.Side).ToList();
                    if (seaEnemies.Count > 0) moves.Add(new Point(1, piece.Y));
                }
                break;

            case PieceKind.AntiAir: // Cao Xạ
                // Hỏa khí nặng: qua sông chỉ ở ngầm cạn, có công binh bắc cầu, hoặc khi ăn quân bên kia sông
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false, allowDeepRiver: false);
                if (piece.X == 2)
                {
                    var seaEnemies = GetPiecesAt(1, piece.Y).Where(p => p.Side != piece.Side).ToList();
                    if (seaEnemies.Count > 0) moves.Add(new Point(1, piece.Y));
                }
                break;

            case PieceKind.Tank: // Xe Tăng
                // Đi và ăn thẳng dọc ngang từ 1 đến 2 đoạn (vượt sông tự do); nếu anh hùng đi cả chéo 45 độ
                int maxTankRange = 2 + bonus;
                int tankDirs = piece.IsHero ? 8 : 4;
                for (int i = 0; i < tankDirs; i++)
                {
                    for (int step = 1; step <= maxTankRange; step++)
                    {
                        int tx = piece.X + DirX8[i] * step;
                        int ty = piece.Y + DirY8[i] * step;
                        if (!InBounds(tx, ty)) break;

                        // Nếu là mục tiêu trên biển: Xe tăng được đứng tại chỗ bắn ăn mục tiêu trong tầm
                        if (IsSea(tx))
                        {
                            var target = GetPiecesAt(tx, ty).FirstOrDefault(p => p.Side != piece.Side);
                            if (target != null)
                                moves.Add(new Point(tx, ty));
                            break; // Không thể lái xe xuống biển
                        }

                        var occs = GetPiecesAt(tx, ty);
                        if (occs.Count == 0)
                        {
                            if (!IsHeadquarter(tx, ty)) moves.Add(new Point(tx, ty));
                        }
                        else
                        {
                            var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                            if (enemy != null)
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            else if (CanStack(piece, occs))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            break; // Bị cản
                        }
                    }
                }
                break;

            case PieceKind.Artillery: // Pháo Binh
                // Đi và ăn thẳng dọc ngang, đi/ăn chéo 45 độ từ 1 đến 3 đoạn
                // Được ăn vượt qua khối chắn (nhảy qua quân cản như pháo cờ tướng) hoặc bắn trực tiếp
                int maxArtRange = 3 + bonus;
                for (int dir = 0; dir < 8; dir++)
                {
                    int screenCount = 0; // Đếm số quân chắn trên đường
                    for (int step = 1; step <= maxArtRange; step++)
                    {
                        int tx = piece.X + DirX8[dir] * step;
                        int ty = piece.Y + DirY8[dir] * step;
                        if (!InBounds(tx, ty)) break;

                        var occs = GetPiecesAt(tx, ty);
                        if (screenCount == 0)
                        {
                            if (occs.Count == 0)
                            {
                                if (!IsSea(tx) && !IsHeadquarter(tx, ty) &&
                                    CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                            }
                            else
                            {
                                var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                if (enemy != null)
                                {
                                    moves.Add(new Point(tx, ty)); // Bắn trực tiếp (kể cả qua sông/ra biển/phá sở chỉ huy)
                                }
                                else if (CanStack(piece, occs) && CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                                screenCount = 1; // Làm khối chắn
                            }
                        }
                        else if (screenCount == 1)
                        {
                            if (occs.Count > 0)
                            {
                                var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                if (enemy != null)
                                {
                                    moves.Add(new Point(tx, ty)); // Bắn vượt khối chắn
                                }
                                break;
                            }
                        }
                    }
                }
                break;

            case PieceKind.Rocket: // Tên Lửa Phòng Không
                // Vành đai hỏa lực: 2 đoạn theo trục và 1 đoạn chéo 45 độ
                for (int dir = 0; dir < 8; dir++)
                {
                    bool isDiag = dir >= 4;
                    int maxR = isDiag ? (1 + bonus) : (2 + bonus);
                    for (int step = 1; step <= maxR; step++)
                    {
                        int tx = piece.X + DirX8[dir] * step;
                        int ty = piece.Y + DirY8[dir] * step;
                        if (!InBounds(tx, ty)) break;

                        var occs = GetPiecesAt(tx, ty);
                        if (occs.Count == 0)
                        {
                            if (!IsSea(tx) && !IsHeadquarter(tx, ty) &&
                                CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                        }
                        else
                        {
                            var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                            if (enemy != null)
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            else if (CanStack(piece, occs) && CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            break;
                        }
                    }
                }
                break;

            case PieceKind.AirForce: // Không Quân (Máy Bay)
                // Đi và ăn thẳng/chéo 45 độ từ 1 đến 4 đoạn; BAY VƯỢT KHỐI CHẮN (không bị cản)
                int maxAirRange = 4 + bonus;
                for (int dir = 0; dir < 8; dir++)
                {
                    for (int step = 1; step <= maxAirRange; step++)
                    {
                        int tx = piece.X + DirX8[dir] * step;
                        int ty = piece.Y + DirY8[dir] * step;
                        if (!InBounds(tx, ty)) break;

                            var occs = GetPiecesAt(tx, ty);
                        if (occs.Count == 0)
                        {
                            if (!IsHeadquarter(tx, ty) && !IsSea(tx)) moves.Add(new Point(tx, ty));
                        }
                        else
                        {
                            var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                            if (enemy != null)
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            else if (CanStack(piece, occs))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                        }
                    }
                }
                break;

            case PieceKind.Warship: // Tàu Chiến (Hải Quân)
                // Di chuyển: Ngang, dọc và chéo 45 độ (1-4 đoạn) trên các ô quy định (biển X <= 2 hoặc sông sâu), KHÔNG bị chặn bởi quân khác.
                // Ăn thế chỗ: tầm 4 ô ngang/dọc/chéo 45 độ khi không vướng đất liền (mục tiêu là Hải quân địch hoặc mục tiêu bất kỳ dọc bờ biển/bờ sông).
                // Xạ kích: Chéo 45 độ 4 ô với Hải quân địch (không vướng đất liền); Ngang/Dọc/Chéo 45 độ 3 ô với các quân khác ngoài Hải quân trên đất liền.
                int maxShipRange = 4 + bonus;
                for (int dir = 0; dir < 8; dir++)
                {
                    bool hitLand = false;

                    for (int step = 1; step <= maxShipRange; step++)
                    {
                        int tx = piece.X + DirX8[dir] * step;
                        int ty = piece.Y + DirY8[dir] * step;
                        if (!InBounds(tx, ty)) break;

                        bool isNavigable = (tx <= 2) || IsDeepRiver(tx, ty);
                        if (!isNavigable) hitLand = true;

                        var occs = GetPiecesAt(tx, ty);

                        if (!hitLand)
                        {
                            if (occs.Count == 0)
                            {
                                moves.Add(new Point(tx, ty));
                            }
                            else
                            {
                                var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                if (enemy != null)
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                                else if (CanStack(piece, occs))
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                                // Không bị chặn bởi quân khác -> tiếp tục xét các ô tiếp theo trên hướng này
                            }
                        }
                        else
                        {
                            // Đã vướng đất liền: chỉ được Xạ kích (Ngang/Dọc/Chéo 45 độ tầm 3 ô) với các quân ngoài Hải quân
                            if (step > 3) break;

                            if (occs.Count > 0)
                            {
                                var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                if (enemy != null && enemy.Kind != PieceKind.Warship)
                                {
                                    moves.Add(new Point(tx, ty));
                                }
                            }
                        }
                    }
                }
                break;
        }

        return moves.Distinct().ToList();
    }

    private bool HasEngineerBridge(string side, int x)
    {
        return Pieces.Any(p => p.IsAlive && p.Side == side && p.Kind == PieceKind.Engineer &&
            p.X == x && (p.Y == 5 || p.Y == 6 || p.Y == 4 || p.Y == 7));
    }

    private bool CanHeavyCrossRiver(PieceView piece, int toX, int toY, bool isCaptureMove)
    {
        if (!IsCrossingRiver(piece.Y, toY)) return true;
        if (IsShallowFord(piece.X) || IsShallowFord(toX)) return true;
        if (isCaptureMove) return true; // Luật: Khi ăn quân đối phương bên kia sông được phép qua thế chỗ
        return HasEngineerBridge(piece.Side, piece.X) || HasEngineerBridge(piece.Side, toX);
    }

    private void AddStepMoves(PieceView piece, List<Point> moves, int maxSteps, bool allowDiagonal, bool canEnterSea, bool allowDeepRiver = true)
    {
        int[] dx = allowDiagonal ? new[] { 0, 0, -1, 1, -1, 1, -1, 1 } : new[] { 0, 0, -1, 1 };
        int[] dy = allowDiagonal ? new[] { 1, -1, 0, 0, 1, 1, -1, -1 } : new[] { 1, -1, 0, 0 };

        for (int i = 0; i < dx.Length; i++)
        {
            for (int step = 1; step <= maxSteps; step++)
            {
                int tx = piece.X + dx[i] * step;
                int ty = piece.Y + dy[i] * step;
                if (!InBounds(tx, ty)) break;

                if (!canEnterSea && IsSea(tx)) break; // Không vào vùng biển

                var occs = GetPiecesAt(tx, ty);
                if (occs.Count == 0)
                {
                    if (!allowDeepRiver && !CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                        break;
                    if (!IsHeadquarter(tx, ty)) moves.Add(new Point(tx, ty));
                }
                else
                {
                    var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                    if (enemy != null)
                    {
                        if (allowDeepRiver || CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: true))
                        {
                            moves.Add(new Point(tx, ty));
                        }
                    }
                    else if (CanStack(piece, occs))
                    {
                        if (allowDeepRiver || CanHeavyCrossRiver(piece, tx, ty, isCaptureMove: false))
                        {
                            moves.Add(new Point(tx, ty));
                        }
                    }
                    break;
                }
            }
        }
    }

    private static bool InBounds(int x, int y) => x >= 0 && x < COLS && y >= 0 && y < ROWS;
    private static bool IsCrossingRiver(int fromY, int toY) => (fromY <= 5 && toY >= 6) || (fromY >= 6 && toY <= 5);
    private static bool IsDeepRiver(int x, int y) => (y == 5 || y == 6) && (x != 5 && x != 7);

    public PieceView? GetPieceAt(int x, int y) => Pieces.FirstOrDefault(p => p.X == x && p.Y == y && p.IsAlive);

    private List<PieceView> GetEnemyAntiAirDefendersAt(string friendlySide, int x, int y)
    {
        return Pieces.Where(p => p.Side != friendlySide && p.IsAlive &&
            (p.Kind == PieceKind.AntiAir || p.Kind == PieceKind.Rocket || p.Kind == PieceKind.Warship) &&
            IsWithinFireRing(p, x, y)).ToList();
    }

    private static bool IsWithinFireRing(PieceView defender, int x, int y)
    {
        int r = defender.Kind == PieceKind.Rocket ? 2 : 1;
        return Math.Abs(defender.X - x) <= r && Math.Abs(defender.Y - y) <= r;
    }

    /// <summary>
    /// Tìm ô rìa vành đai lửa đầu tiên mà Không quân bay qua trên đường từ (fromX, fromY) tới (toX, toY).
    /// </summary>
    private (Point? EdgeCell, List<PieceView> Defenders) FindFirstFireRingIntersectionOnFlight(PieceView airPiece, int fromX, int fromY, int toX, int toY)
    {
        int dx = Math.Sign(toX - fromX);
        int dy = Math.Sign(toY - fromY);
        int steps = Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY));

        for (int step = 1; step <= steps; step++)
        {
            int cx = fromX + dx * step;
            int cy = fromY + dy * step;
            var defenders = GetEnemyAntiAirDefendersAt(airPiece.Side, cx, cy);
            if (defenders.Count > 0)
            {
                return (new Point(cx, cy), defenders);
            }
        }

        return (null, new List<PieceView>());
    }

    /// <summary>
    /// Thực hiện di chuyển hoặc ăn quân kèm theo xử lý luật và xác nhận Client-Server
    /// </summary>
    public void MovePiece(string pieceId, int toX, int toY)
    {
        if (isAnimatingExplosion) return;

        var piece = Pieces.FirstOrDefault(p => p.PieceId == pieceId && p.IsAlive);
        if (piece == null) return;

        bool isMoveValid = true;

        if (!isMoveValid)
        {
            MessageBox.Show("Nước đi không được máy chủ chấp thuận!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int oldX = piece.X;
        int oldY = piece.Y;

        // Lấy danh sách quân địch tại ô đích
        var targetEnemies = GetPiecesAt(toX, toY).Where(p => p.Side != piece.Side).ToList();
        PieceView? primaryTarget = targetEnemies.FirstOrDefault();

        // =========================================================================
        // XỬ LÝ RIÊNG CHO KHÔNG QUÂN (AIRFORCE)
        // =========================================================================
        if (piece.Kind == PieceKind.AirForce)
        {
            var (edgeCell, edgeDefenders) = FindFirstFireRingIntersectionOnFlight(piece, oldX, oldY, toX, toY);

            // Trường hợp 1: Bay qua vành đai lửa TRƯỚC khi tới ô đích -> Phát nổ ngay ô rìa vành đai lửa
            if (edgeCell.HasValue && (edgeCell.Value.X != toX || edgeCell.Value.Y != toY) && !piece.IsHero)
            {
                piece.IsAlive = false;
                piece.X = edgeCell.Value.X;
                piece.Y = edgeCell.Value.Y;

                StartExplosionAnimation(edgeCell.Value, edgeDefenders, () =>
                {
                    FinishTurnAfterMove(piece, edgeCell.Value.X, edgeCell.Value.Y, null);
                });
                return;
            }

            // Trường hợp 2: Tới được ô đích (toX, toY)
            if (targetEnemies.Count > 0 && primaryTarget != null)
            {
                if (primaryTarget.Kind == PieceKind.AirForce)
                {
                    // 2a. Địch là Không quân -> Ăn thế chỗ
                    KillTargetEnemies(piece, targetEnemies);
                    MovePieceWithStack(piece, oldX, oldY, toX, toY);
                    FinishTurnAfterMove(piece, toX, toY, primaryTarget);
                    return;
                }

                var defendersAtTarget = GetEnemyAntiAirDefendersAt(piece.Side, toX, toY);
                if (defendersAtTarget.Count > 0)
                {
                    // 2b. Ăn mục tiêu trong vành đai lửa -> Cảm tử
                    KillTargetEnemies(piece, targetEnemies);
                    if (!piece.IsHero)
                    {
                        piece.IsAlive = false;
                    }
                    piece.X = toX;
                    piece.Y = toY;

                    StartExplosionAnimation(new Point(toX, toY), defendersAtTarget, () =>
                    {
                        FinishTurnAfterMove(piece, toX, toY, primaryTarget);
                    });
                    return;
                }

                // 2c. Không nằm trong 2 trường hợp trên -> Ném bom từ xa (đứng tại chỗ cũ)
                KillTargetEnemies(piece, targetEnemies);
                FinishTurnAfterMove(piece, toX, toY, primaryTarget);
                return;
            }

            // Trường hợp 3: Di chuyển tới ô trống / ghép quân nhưng ô đích nằm ngay rìa vành đai lửa
            if (edgeCell.HasValue && !piece.IsHero)
            {
                piece.IsAlive = false;
                piece.X = edgeCell.Value.X;
                piece.Y = edgeCell.Value.Y;

                StartExplosionAnimation(edgeCell.Value, edgeDefenders, () =>
                {
                    FinishTurnAfterMove(piece, edgeCell.Value.X, edgeCell.Value.Y, null);
                });
                return;
            }

            // Di chuyển bình thường an toàn
            MovePieceWithStack(piece, oldX, oldY, toX, toY);
            FinishTurnAfterMove(piece, toX, toY, null);
            return;
        }

        // =========================================================================
        // XỬ LÝ CÁC QUÂN CÒN LẠI
        // =========================================================================
        bool isStandShoot = false;

        if (targetEnemies.Count > 0)
        {
            if (piece.Kind == PieceKind.Warship && primaryTarget != null)
            {
                bool isTargetNavigable = (toX <= 2) || IsDeepRiver(toX, toY);
                bool isDiagonalAttack = (oldX != toX) && (oldY != toY);

                if (primaryTarget.Kind == PieceKind.Warship)
                {
                    // Hải quân địch: chéo 45 độ có thể Xạ kích nếu chọn StandStill; ngang dọc luôn Thế chỗ
                    isStandShoot = isDiagonalAttack && CurrentCaptureMode == CaptureMode.StandStill;
                }
                else if (isTargetNavigable && !HasLandOnPath(oldX, oldY, toX, toY))
                {
                    // Mục tiêu dọc bờ biển hoặc bờ sông (không vướng đất liền) -> Bắt buộc Thế chỗ
                    isStandShoot = false;
                }
                else
                {
                    // Mục tiêu trên đất liền hoặc bị vướng đất liền -> Xạ kích (đứng tại chỗ)
                    isStandShoot = true;
                }
            }
            // Kiểm tra chế độ ăn quân: Bắn tại chỗ / Oanh tạc quay về
            else if (CurrentCaptureMode == CaptureMode.StandStill && HasMultipleCaptureModes(piece.Kind))
            {
                isStandShoot = true;
            }
            else
            {
                // Quy tắc đứng tại chỗ ăn quân bắt buộc theo địa hình: bất kỳ quân bộ nào bắn ra biển đều đứng tại chỗ
                if (IsSea(toX)) isStandShoot = true;
            }

            KillTargetEnemies(piece, targetEnemies);
        }

        // Di chuyển vị trí quân
        if (!isStandShoot)
        {
            MovePieceWithStack(piece, oldX, oldY, toX, toY);
        }

        FinishTurnAfterMove(piece, toX, toY, primaryTarget);
    }

    private static bool HasLandOnPath(int fromX, int fromY, int toX, int toY)
    {
        int dx = Math.Sign(toX - fromX);
        int dy = Math.Sign(toY - fromY);
        int steps = Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY));
        for (int step = 1; step <= steps; step++)
        {
            int cx = fromX + dx * step;
            int cy = fromY + dy * step;
            if (cx > 2 && !IsDeepRiver(cx, cy))
            {
                return true;
            }
        }
        return false;
    }

    private void KillTargetEnemies(PieceView attacker, List<PieceView> targetEnemies)
    {
        foreach (var te in targetEnemies)
        {
            te.IsAlive = false;
            if (te.Kind == PieceKind.Commander)
            {
                attacker.IsHero = true;
                attacker.Score += 10;
            }
        }
    }

    private void MovePieceWithStack(PieceView piece, int oldX, int oldY, int toX, int toY)
    {
        var passengers = new List<PieceView>();
        if (MoveStackTogether && IsCarrier(piece.Kind))
        {
            passengers = Pieces.Where(p => p.X == oldX && p.Y == oldY && p.IsAlive && p != piece && p.Side == piece.Side).ToList();
        }

        piece.X = toX;
        piece.Y = toY;

        foreach (var pass in passengers)
        {
            pass.X = toX;
            pass.Y = toY;
        }
    }

    private void StartExplosionAnimation(Point gridPos, List<PieceView> defenders, Action onComplete)
    {
        SelectedPiece = null;
        ValidMoves.Clear();
        isAnimatingExplosion = true;
        animationStartTime = DateTime.Now;
        explosionGridPos = gridPos;
        blinkingFireRingPieces = defenders.Distinct().ToList();
        pendingPostExplosionAction = onComplete;
        animationTimer.Start();
        Invalidate();
    }

    private void FinishTurnAfterMove(PieceView piece, int toX, int toY, PieceView? primaryTarget)
    {
        EvaluateHeroPromotions(piece);
        CurrentTurn = CurrentTurn == "red" ? "blue" : "red";
        SelectedPiece = null;
        ValidMoves.Clear();
        Invalidate();

        OnPieceMoved?.Invoke(piece, toX, toY, primaryTarget);
    }

    /// <summary>
    /// Quy tắc Quân Anh Hùng theo Rule.pdf:
    /// 1) Quân trực tiếp chiếu Tư lệnh đối phương được phong Anh hùng (+1 tầm, +10đ, đi chéo 45°, máy bay tàng hình)
    /// 2) Quân cuối cùng còn lại bảo vệ Tư lệnh của mỗi bên cũng được phong Anh hùng (kể cả Sở chỉ huy)
    /// </summary>
    private void EvaluateHeroPromotions(PieceView movedPiece)
    {
        if (movedPiece.IsAlive && !movedPiece.IsHero && movedPiece.Kind != PieceKind.Commander && movedPiece.Kind != PieceKind.Headquarter)
        {
            string enemySide = movedPiece.Side == "red" ? "blue" : "red";
            var enemyCmd = Pieces.FirstOrDefault(p => p.Side == enemySide && p.Kind == PieceKind.Commander && p.IsAlive);
            if (enemyCmd != null && GetValidMoves(movedPiece).Any(m => m.X == enemyCmd.X && m.Y == enemyCmd.Y))
            {
                movedPiece.IsHero = true;
                movedPiece.Score += 10;
            }
        }

        foreach (var side in new[] { "red", "blue" })
        {
            var cmd = Pieces.FirstOrDefault(p => p.Side == side && p.Kind == PieceKind.Commander && p.IsAlive);
            if (cmd == null) continue;
            var defenders = Pieces.Where(p => p.Side == side && p.IsAlive && p.Kind != PieceKind.Commander).ToList();
            if (defenders.Count == 1 && !defenders[0].IsHero)
            {
                defenders[0].IsHero = true;
                defenders[0].Score += 10;
            }
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

        int gridW = (COLS - 1) * cellWidth;
        int gridH = (ROWS - 1) * cellHeight;
        var destRect = new Rectangle(marginX, marginY, gridW, gridH);

        // 1. VẼ HÌNH NỀN BẢN ĐỒ CHIẾN TRƯỜNG (TERRAIN MAP)
        if (terrainImage != null)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            if (terrainImage.Width == 1024 && terrainImage.Height >= 1050)
            {
                // Cắt đúng vùng lưới 11x12 từ bản đồ đã chuẩn hóa (x: 44->1004, y: 10->1034)
                var srcRect = new Rectangle(44, 10, 960, 1024);
                g.DrawImage(terrainImage, destRect, srcRect, GraphicsUnit.Pixel);
            }
            else
            {
                var srcRect = new Rectangle(44, 10, 960, terrainImage.Height - 35);
                g.DrawImage(terrainImage, destRect, srcRect, GraphicsUnit.Pixel);
            }

            // Khung viền bản đồ mạ vàng quân sự tinh tế
            using var borderPen = new Pen(Color.FromArgb(200, 218, 165, 32), 2.5f);
            g.DrawRectangle(borderPen, destRect);
        }
        else
        {
            // Vẽ nền đồ họa mặc định nếu chưa tải được ảnh
            DrawDefaultTerrain(g);
        }

        // 2. VẼ LƯỚI TỌA ĐỘ BÀN CỜ (Đường kẻ bán trong suốt nổi bật trên địa hình)
        using (var gridPen = new Pen(Color.FromArgb(135, 255, 255, 255), 1.2f))
        {
            // Các đường dọc (0 đến 10)
            for (int col = 0; col < COLS; col++)
            {
                int x = marginX + col * cellWidth;
                g.DrawLine(gridPen, x, marginY, x, marginY + gridH);
            }

            // Các đường ngang (0 đến 11)
            for (int row = 0; row < ROWS; row++)
            {
                int y = marginY + row * cellHeight;
                g.DrawLine(gridPen, marginX, y, marginX + gridW, y);
            }
        }

        // 3. VẼ SỞ CHỈ HUY (Headquarters)
        DrawHeadquarterMark(g, 5, 1, "red");
        DrawHeadquarterMark(g, 7, 1, "red");
        DrawHeadquarterMark(g, 5, 10, "blue");
        DrawHeadquarterMark(g, 7, 10, "blue");

        // 4. VẼ SỐ HIỆU TỌA ĐỘ TRỤC (0-10 cạnh dưới, 0-11 cạnh trái)
        using (var axisFont = new Font("Segoe UI", 9F, FontStyle.Bold))
        using (var axisBrush = new SolidBrush(Color.FromArgb(241, 196, 15))) // Màu vàng ánh kim nổi bật
        {
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // Trục X (0 -> 10) ở phía dưới
            for (int col = 0; col < COLS; col++)
            {
                int x = marginX + col * cellWidth;
                int y = marginY + gridH + 16;
                g.DrawString(col.ToString(), axisFont, axisBrush, x, y, sfCenter);
            }

            // Trục Y (0 -> 11) ở phía bên trái (Y=0 ở dưới cùng, Y=11 ở trên cùng)
            for (int row = 0; row < ROWS; row++)
            {
                int y = marginY + (ROWS - 1 - row) * cellHeight;
                int x = marginX - 18;
                g.DrawString(row.ToString(), axisFont, axisBrush, x, y, sfCenter);
            }
        }

            // 6. Vẽ VÀNH ĐAI HỎA LỰC
            // Khi chọn quân để đi: nếu quân đó có vành đai lửa thì hiển thị
            if (SelectedPiece != null && SelectedPiece.IsAlive && HasFireRing(SelectedPiece.Kind))
            {
                DrawFireThreatCircles(g, SelectedPiece, isInspected: false);
            }

            // Khi chọn Không quân: hiển thị tất cả vành đai lửa phòng không của đối phương (Cao xạ, Tên lửa, Tàu chiến)
            if (SelectedPiece != null && SelectedPiece.IsAlive && SelectedPiece.Kind == PieceKind.AirForce)
            {
                var enemyAirDefenders = Pieces.Where(p => p.IsAlive && p.Side != SelectedPiece.Side &&
                    (p.Kind == PieceKind.AntiAir || p.Kind == PieceKind.Rocket || p.Kind == PieceKind.Warship));
                foreach (var defender in enemyAirDefenders)
                {
                    if (defender != InspectedFirePiece)
                    {
                        DrawFireThreatCircles(g, defender, isInspected: false);
                    }
                }
            }

        // Khi người chơi click chuột phải vào bất kỳ quân có vành đai lửa (cả 2 bên đều xem được)
        if (InspectedFirePiece != null && InspectedFirePiece.IsAlive && InspectedFirePiece != SelectedPiece)
        {
            DrawFireThreatCircles(g, InspectedFirePiece, isInspected: true);
        }

        // Hiệu ứng nhấp nháy của (các) vành đai lửa gây ra vụ nổ trong 2 giây
        if (isAnimatingExplosion && blinkingFireRingPieces.Count > 0)
        {
            double elapsedMs = (DateTime.Now - animationStartTime).TotalMilliseconds;
            bool blinkOn = ((int)(elapsedMs / 160) % 2) == 0;
            foreach (var defender in blinkingFireRingPieces)
            {
                if (defender.IsAlive)
                {
                    DrawFireThreatCircles(g, defender, isInspected: false, isBlinking: true, blinkOn: blinkOn);
                }
            }
        }

        // 7. Vẽ CÁC Ô GỢI Ý NƯỚC ĐI HỢP LỆ (HIGHTLIGHT VALID MOVES)
        foreach (var move in ValidMoves)
        {
            var pt = GridToScreen(move.X, move.Y);
            var occupants = GetPiecesAt(move.X, move.Y);
            var enemy = occupants.FirstOrDefault(p => p.Side != SelectedPiece?.Side);
            var friendly = occupants.FirstOrDefault(p => p.Side == SelectedPiece?.Side);

                if (enemy != null)
                {
                    // Ô có quân địch để ăn: vẽ hồng tâm đỏ rực
                    using var attackPen = new Pen(Color.FromArgb(231, 76, 60), 3f);
                    attackPen.DashStyle = DashStyle.Dash;
                    int r = (int)(cellWidth * 0.42);
                    g.DrawEllipse(attackPen, pt.X - r, pt.Y - r, r * 2, r * 2);

                    // Hồng tâm ngắm bắn
                    g.DrawLine(attackPen, pt.X - r - 4, pt.Y, pt.X - r + 4, pt.Y);
                    g.DrawLine(attackPen, pt.X + r - 4, pt.Y, pt.X + r + 4, pt.Y);
                    g.DrawLine(attackPen, pt.X, pt.Y - r - 4, pt.X, pt.Y - r + 4);
                    g.DrawLine(attackPen, pt.X, pt.Y + r - 4, pt.X, pt.Y + r + 4);
                }
            else if (friendly != null)
            {
                // Ô ghép quân đồng minh: vẽ vòng tròn xanh lam với dấu '+'
                using var stackPen = new Pen(Color.FromArgb(52, 152, 219), 2.5f);
                stackPen.DashStyle = DashStyle.Dash;
                int r = (int)(cellWidth * 0.40);
                g.DrawEllipse(stackPen, pt.X - r, pt.Y - r, r * 2, r * 2);

                using var stackBrush = new SolidBrush(Color.FromArgb(90, 52, 152, 219));
                g.FillEllipse(stackBrush, pt.X - 9, pt.Y - 9, 18, 18);
                using var plusPen = new Pen(Color.White, 2f);
                g.DrawLine(plusPen, pt.X - 5, pt.Y, pt.X + 5, pt.Y);
                g.DrawLine(plusPen, pt.X, pt.Y - 5, pt.X, pt.Y + 5);
            }
            else
            {
                // Ô trống: vẽ chấm tròn xanh lá nổi bật
                using var moveBrush = new SolidBrush(Color.FromArgb(180, 46, 204, 113));
                using var movePen = new Pen(Color.FromArgb(39, 174, 96), 2f);
                g.FillEllipse(moveBrush, pt.X - 8, pt.Y - 8, 16, 16);
                g.DrawEllipse(movePen, pt.X - 8, pt.Y - 8, 16, 16);
            }
        }

        // 8. Vẽ Highlight ô đang hover
        if (hoverGrid.HasValue)
        {
            var hpt = GridToScreen(hoverGrid.Value.X, hoverGrid.Value.Y);
            using var hoverBrush = new SolidBrush(Color.FromArgb(40, 52, 152, 219));
            g.FillEllipse(hoverBrush, hpt.X - cellWidth / 3, hpt.Y - cellHeight / 3, cellWidth * 2 / 3, cellHeight * 2 / 3);
        }

        // 9. Vẽ Highlight quân đang được chọn
        if (SelectedPiece != null && SelectedPiece.IsAlive)
        {
            var spt = GridToScreen(SelectedPiece.X, SelectedPiece.Y);
            int size = (int)(Math.Min(cellWidth, cellHeight) * 0.90);
            using var selectPen = new Pen(Color.FromArgb(241, 196, 15), 3f);
            selectPen.DashStyle = DashStyle.Dash;
            g.DrawEllipse(selectPen, spt.X - size / 2 - 3, spt.Y - size / 2 - 3, size + 6, size + 6);
        }

            // 10. Vẽ TẤT CẢ CÁC QUÂN CỜ CÒN SỐNG (Bao gồm hiệu ứng xếp chồng)
            var aliveGroups = Pieces.Where(p => p.IsAlive).GroupBy(p => new Point(p.X, p.Y));
        foreach (var group in aliveGroups)
        {
            var list = group.ToList();
            var rect = GetPieceRect(group.Key.X, group.Key.Y);
            if (list.Count == 1)
            {
                list[0].Draw(g, rect);
            }
            else
            {
                // Có nhóm quân xếp chồng
                var mainPiece = list.FirstOrDefault(p => IsCarrier(p.Kind)) ?? list[0];

                // Viền bóng đôi vàng cam cho khối xếp chồng
                using (var stackUnderPen = new Pen(Color.FromArgb(220, 243, 156, 18), 3f))
                {
                    g.DrawEllipse(stackUnderPen, rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                }

                mainPiece.Draw(g, rect);

                // Huy hiệu số quân xếp chồng ở góc phải
                int badgeSize = 20;
                int bx = rect.Right - badgeSize + 2;
                int by = rect.Bottom - badgeSize + 2;
                using var badgeBrush = new SolidBrush(Color.FromArgb(243, 156, 18));
                using var badgePen = new Pen(Color.White, 1.8f);
                g.FillEllipse(badgeBrush, bx, by, badgeSize, badgeSize);
                g.DrawEllipse(badgePen, bx, by, badgeSize, badgeSize);

                using var badgeFont = new Font("Segoe UI", 8F, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(list.Count.ToString(), badgeFont, textBrush, new RectangleF(bx, by, badgeSize, badgeSize), sf);
            }
        }

        // 10.5. Vẽ HIỆU ỨNG NỔ 2D (khi Không quân bay qua hoặc cảm tử trong vành đai lửa)
        if (isAnimatingExplosion && explosionGridPos.HasValue)
        {
            float progress = Math.Clamp((float)((DateTime.Now - animationStartTime).TotalMilliseconds / ExplosionDurationMs), 0f, 1f);
            DrawExplosion2D(g, explosionGridPos.Value, progress);
        }

        // 11. Vẽ TÊN QUÂN CỜ KHI RÊ CHUỘT (HOVER TOOLTIP BADGE)
        if (hoverGrid.HasValue)
        {
            DrawHoverPieceTooltip(g, hoverGrid.Value);
        }
    }

    private void DrawDefaultTerrain(Graphics g)
    {
        // 1. Vùng biển (Hải phận)
        int seaRightX = marginX + 2 * cellWidth;
        using (var seaBrush = new LinearGradientBrush(
            new Rectangle(marginX - 15, marginY, seaRightX - marginX + 15, (ROWS - 1) * cellHeight),
            Color.FromArgb(174, 214, 241), Color.FromArgb(133, 193, 233), LinearGradientMode.Horizontal))
        {
            g.FillRectangle(seaBrush, marginX - 15, marginY - 10, seaRightX - marginX + 15, (ROWS - 1) * cellHeight + 20);
        }

        // 2. Sông Triều Dương
        int riverTopY = marginY + (ROWS - 1 - 6) * cellHeight;
        int riverHeight = cellHeight;
        int riverLeftX = seaRightX;
        int riverWidth = (COLS - 1 - 2) * cellWidth;

        using (var riverBrush = new SolidBrush(Color.FromArgb(169, 223, 214)))
        {
            g.FillRectangle(riverBrush, riverLeftX, riverTopY, riverWidth, riverHeight);
        }

        DrawShallowFord(g, 5, riverTopY, riverHeight);
        DrawShallowFord(g, 7, riverTopY, riverHeight);
    }

    private void DrawShallowFord(Graphics g, int col, int riverTopY, int riverHeight)
    {
        int x = marginX + col * cellWidth;
        using var fordBrush = new SolidBrush(Color.FromArgb(218, 190, 140));
        g.FillRectangle(fordBrush, x - cellWidth / 2, riverTopY, cellWidth, riverHeight);

        // Vẽ các đốm cát/sỏi
        using var pebbleBrush = new SolidBrush(Color.FromArgb(160, 130, 90));
        g.FillEllipse(pebbleBrush, x - 8, riverTopY + 6, 4, 3);
        g.FillEllipse(pebbleBrush, x + 5, riverTopY + 14, 5, 3);
        g.FillEllipse(pebbleBrush, x - 12, riverTopY + 22, 4, 3);
        g.FillEllipse(pebbleBrush, x + 7, riverTopY + 28, 4, 3);

        using var font = new Font("Segoe UI", 7F, FontStyle.Italic);
        using var brush = new SolidBrush(Color.FromArgb(120, 90, 50));
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
        g.DrawString("Ngầm cạn", font, brush, new RectangleF(x - cellWidth / 2, riverTopY, cellWidth, riverHeight - 2), sf);
    }

    private void DrawHeadquarterMark(Graphics g, int gx, int gy, string side)
    {
        var pt = GridToScreen(gx, gy);
        int r = (int)(cellWidth * 0.40);

        Color c = side == "red" ? Color.FromArgb(192, 57, 43) : Color.FromArgb(41, 128, 185);
        using var hqPen = new Pen(Color.FromArgb(120, c), 1.8f);
        hqPen.DashStyle = DashStyle.DashDot;
        g.DrawEllipse(hqPen, pt.X - r, pt.Y - r, r * 2, r * 2);

        // Biểu tượng công sự / lều chỉ huy
        using var brush = new SolidBrush(Color.FromArgb(40, c));
        g.FillEllipse(brush, pt.X - r, pt.Y - r, r * 2, r * 2);

        using var font = new Font("Segoe UI", 6.8F, FontStyle.Bold);
        using var textBrush = new SolidBrush(c);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("SỞ CHỈ HUY", font, textBrush, pt.X, pt.Y + r + 8, sf);
    }

    private void DrawFireThreatCircles(Graphics g, PieceView piece, bool isInspected = false, bool isBlinking = false, bool blinkOn = true)
    {
        var pt = GridToScreen(piece.X, piece.Y);
        float ringRadiusCells = GetFireRingRadius(piece.Kind);
        if (ringRadiusCells <= 0) return;

        float radius = ringRadiusCells * cellWidth;

        Color baseColor = isBlinking
            ? (blinkOn ? Color.FromArgb(255, 70, 20) : Color.FromArgb(243, 156, 18))
            : (isInspected
                ? Color.FromArgb(243, 156, 18) // Hổ phách/vàng rực khi soi bằng chuột phải
                : (piece.Side == "red" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(52, 152, 219)));

        int fillAlpha = isBlinking ? (blinkOn ? 95 : 18) : 30;
        int penAlpha = isBlinking ? (blinkOn ? 255 : 70) : 210;
        float penWidth = isBlinking ? (blinkOn ? 3.8f : 1.5f) : (isInspected ? 2.5f : 2.0f);

        // 1. Vùng bóng lửa mờ bao phủ
        using (var zoneBrush = new SolidBrush(Color.FromArgb(fillAlpha, baseColor)))
        {
            g.FillEllipse(zoneBrush, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }

        // 2. Viền đứt đoạn vành đai lửa
        using (var ringPen = new Pen(Color.FromArgb(penAlpha, baseColor), penWidth))
        {
            ringPen.DashStyle = isBlinking && blinkOn ? DashStyle.Solid : DashStyle.Dash;
            g.DrawEllipse(ringPen, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }

        // 3. Vòng tròn phụ bên trong
        using (var innerPen = new Pen(Color.FromArgb(isBlinking && blinkOn ? 180 : 90, baseColor), 1.0f))
        {
            g.DrawEllipse(innerPen, pt.X - radius * 0.7f, pt.Y - radius * 0.7f, radius * 1.4f, radius * 1.4f);
        }

        if (isBlinking && blinkOn)
        {
            int pr = (int)(Math.Min(cellWidth, cellHeight) * 0.50);
            using var flashPen = new Pen(Color.FromArgb(240, 255, 220, 50), 3.0f);
            g.DrawEllipse(flashPen, pt.X - pr, pt.Y - pr, pr * 2, pr * 2);
        }

        // 4. Nhãn vành đai lửa
        string label = isBlinking
            ? $"🔥 HỎA LỰC PHÒNG KHÔNG: {piece.DisplayName}!"
            : (isInspected
                ? $"🔥 [SOI CHUỘT PHẢI] {piece.DisplayName} ({(piece.Side == "red" ? "Đỏ" : "Xanh")}) • R={ringRadiusCells}"
                : $"🔥 VÀNH ĐAI LỬA: {piece.DisplayName} • R={ringRadiusCells}");

        using var labelFont = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        var labelSize = g.MeasureString(label, labelFont);
        int lx = pt.X - (int)labelSize.Width / 2;
        int ly = pt.Y - (int)radius - (int)labelSize.Height - 3;
        if (ly < marginY - 10) ly = pt.Y + (int)radius + 3;

        var labelRect = new Rectangle(lx - 4, ly, (int)labelSize.Width + 8, (int)labelSize.Height + 2);
        using (var bgBrush = new SolidBrush(Color.FromArgb(220, 20, 24, 30)))
        using (var borderPen = new Pen(baseColor, 1.2f))
        {
            g.FillRectangle(bgBrush, labelRect);
            g.DrawRectangle(borderPen, labelRect);
        }
        using (var textBrush = new SolidBrush(Color.White))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(label, labelFont, textBrush, labelRect, sf);
        }
    }

    private void DrawExplosion2D(Graphics g, Point gridPos, float progress)
    {
        var pt = GridToScreen(gridPos.X, gridPos.Y);
        float baseSize = Math.Min(cellWidth, cellHeight);
        float fade = 1f - (float)Math.Pow(progress, 1.5);
        int alpha = Math.Clamp((int)(255 * fade), 0, 255);

        // 1. Sóng xung kích lan tỏa (Shockwave ring)
        float shockR = baseSize * (0.25f + progress * 0.95f);
        int shockAlpha = Math.Clamp((int)(220 * (1f - progress)), 0, 255);
        using (var shockPen = new Pen(Color.FromArgb(shockAlpha, 255, 180, 60), 3.0f * (1f - progress * 0.6f)))
        {
            g.DrawEllipse(shockPen, pt.X - shockR, pt.Y - shockR, shockR * 2, shockR * 2);
        }

        // 2. Đám mây khói 2D (nửa sau vụ nổ)
        if (progress > 0.15f)
        {
            float smokeProgress = (progress - 0.15f) / 0.85f;
            int smokeAlpha = Math.Clamp((int)(140 * (1f - smokeProgress)), 0, 255);
            for (int i = 0; i < 6; i++)
            {
                double angle = i * Math.PI / 3.0 + 0.3;
                float dist = baseSize * 0.25f * smokeProgress;
                float sx = pt.X + (float)Math.Cos(angle) * dist;
                float sy = pt.Y + (float)Math.Sin(angle) * dist - smokeProgress * 8f;
                float sr = baseSize * (0.18f + 0.14f * smokeProgress);
                using var smokeBrush = new SolidBrush(Color.FromArgb(smokeAlpha, 70, 70, 75));
                g.FillEllipse(smokeBrush, sx - sr, sy - sr, sr * 2, sr * 2);
            }
        }

        // 3. Ngôi sao vụ nổ 2D đa lớp (Đỏ cam -> Vàng -> Lõi trắng)
        float pulse = progress < 0.2f
            ? (progress / 0.2f)
            : (1f - (progress - 0.2f) * 0.35f) * (0.90f + 0.10f * (float)Math.Sin(progress * 28f));
        float outerR = baseSize * 0.56f * Math.Max(0.2f, pulse);
        float innerR = outerR * 0.45f;

        DrawStarburst(g, pt, 12, outerR, innerR, Color.FromArgb(alpha, 231, 76, 60));
        DrawStarburst(g, pt, 10, outerR * 0.72f, innerR * 0.68f, Color.FromArgb(alpha, 243, 156, 18));
        DrawStarburst(g, pt, 8, outerR * 0.42f, innerR * 0.40f, Color.FromArgb(alpha, 255, 250, 200));

        // 4. Tia lửa bắn văng ra xung quanh
        int sparkCount = 10;
        for (int i = 0; i < sparkCount; i++)
        {
            double angle = i * (2 * Math.PI / sparkCount) + 0.2;
            float dist1 = baseSize * (0.18f + progress * 0.55f);
            float dist2 = dist1 + baseSize * 0.16f * (1f - progress);
            float x1 = pt.X + (float)Math.Cos(angle) * dist1;
            float y1 = pt.Y + (float)Math.Sin(angle) * dist1;
            float x2 = pt.X + (float)Math.Cos(angle) * dist2;
            float y2 = pt.Y + (float)Math.Sin(angle) * dist2;

            using var sparkPen = new Pen(Color.FromArgb(alpha, 255, 225, 90), 2.2f);
            g.DrawLine(sparkPen, x1, y1, x2, y2);
        }

        // 5. Nhãn thông báo nổ ngay trên ô
        using var font = new Font("Segoe UI", 8F, FontStyle.Bold);
        string msg = "💥 PHÁT NỔ!";
        var sz = g.MeasureString(msg, font);
        var rect = new RectangleF(pt.X - sz.Width / 2 - 4, pt.Y - outerR - sz.Height - 4, sz.Width + 8, sz.Height + 2);
        using var bg = new SolidBrush(Color.FromArgb(Math.Clamp((int)(220 * fade), 0, 220), 30, 10, 10));
        using var border = new Pen(Color.FromArgb(alpha, 255, 120, 40), 1.2f);
        using var txt = new SolidBrush(Color.FromArgb(alpha, 255, 245, 200));
        g.FillRectangle(bg, rect);
        g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(msg, font, txt, rect, sf);
    }

    private static void DrawStarburst(Graphics g, Point center, int spikes, float outerR, float innerR, Color color)
    {
        var pts = new PointF[spikes * 2];
        double step = Math.PI / spikes;
        for (int i = 0; i < spikes * 2; i++)
        {
            double r = (i % 2 == 0) ? outerR : innerR;
            double angle = i * step - Math.PI / 2;
            pts[i] = new PointF(center.X + (float)(Math.Cos(angle) * r), center.Y + (float)(Math.Sin(angle) * r));
        }
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, pts);
    }

    public Point GridToScreen(int gx, int gy)
    {
        int sx = marginX + gx * cellWidth;
        int sy = marginY + (ROWS - 1 - gy) * cellHeight; // gy=0 ở đáy màn hình
        return new Point(sx, sy);
    }

    public Point? ScreenToGrid(int mx, int my)
    {
        for (int col = 0; col < COLS; col++)
        {
            for (int row = 0; row < ROWS; row++)
            {
                var pt = GridToScreen(col, row);
                int distSq = (mx - pt.X) * (mx - pt.X) + (my - pt.Y) * (my - pt.Y);
                int radius = (int)(Math.Min(cellWidth, cellHeight) * 0.45);
                if (distSq <= radius * radius)
                {
                    return new Point(col, row);
                }
            }
        }
        return null;
    }

    private Rectangle GetPieceRect(int gx, int gy)
    {
        var pt = GridToScreen(gx, gy);
        int size = (int)(Math.Min(cellWidth, cellHeight) * 0.82);
        return new Rectangle(pt.X - size / 2, pt.Y - size / 2, size, size);
    }

    private void DrawHoverPieceTooltip(Graphics g, Point gridPos)
    {
        var piecesAtCell = GetPiecesAt(gridPos.X, gridPos.Y);
        if (piecesAtCell.Count == 0) return;

        var pt = GridToScreen(gridPos.X, gridPos.Y);

        string sideText = piecesAtCell[0].Side == "red" ? "🔴 ĐỎ" : "🔵 XANH";
        string text;
        if (piecesAtCell.Count == 1)
        {
            var p = piecesAtCell[0];
            string heroTag = p.IsHero ? " ★ ANH HÙNG" : "";
            string fireTag = HasFireRing(p.Kind) ? $" • 🔥 Vành đai R={GetFireRingRadius(p.Kind)}" : "";
            text = $"{sideText} • {p.DisplayName} ({p.Score}đ){heroTag}{fireTag}";
        }
        else
        {
            text = $"{sideText} • NHÓM GHÉP QUÂN ({piecesAtCell.Count} quân):\n" +
                   string.Join("\n", piecesAtCell.Select((p, i) => $"  {i + 1}. {p.DisplayName} ({p.Score}đ)" + (p.IsHero ? " ★" : "") + (HasFireRing(p.Kind) ? " 🔥" : "")));
        }

        using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
        var textSize = g.MeasureString(text, font);

        int padX = 10;
        int padY = 5;
        int tipW = (int)textSize.Width + padX * 2;
        int tipH = (int)textSize.Height + padY * 2;

        int tipX = pt.X - tipW / 2;
        // Nếu ở các hàng sát mép trên (Y >= 10) thì vẽ tooltip bên dưới quân, ngược lại vẽ bên trên
        int tipY = (gridPos.Y >= 10) ? pt.Y + (int)(cellHeight * 0.52) : pt.Y - (int)(cellHeight * 0.52) - tipH;

        // Giữ tooltip không bị tràn ra ngoài biên bàn cờ
        tipX = Math.Max(marginX - 10, Math.Min(Width - marginX - tipW + 10, tipX));

        var tipRect = new Rectangle(tipX, tipY, tipW, tipH);

        // Đổ bóng mềm
        using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
        {
            using var shadowPath = GetRoundedRect(new Rectangle(tipRect.X + 2, tipRect.Y + 2, tipRect.Width, tipRect.Height), 6);
            g.FillPath(shadowBrush, shadowPath);
        }

        // Khung nền đen mờ sang trọng với viền màu phe
        using (var bgBrush = new SolidBrush(Color.FromArgb(235, 26, 36, 43)))
        using (var borderPen = new Pen(piecesAtCell[0].Side == "red" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(52, 152, 219), 1.8f))
        {
            using var path = GetRoundedRect(tipRect, 6);
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        // Chữ tên quân
        using (var textBrush = new SolidBrush(Color.White))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, textBrush, tipRect, sf);
        }
    }

    private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pt = ScreenToGrid(e.X, e.Y);
        if (pt != hoverGrid)
        {
            hoverGrid = pt;
            Invalidate();
        }

        // Đổi con trỏ chuột sang hình bàn tay nếu đang rê vào quân cờ hoặc ô đi hợp lệ
        if (hoverGrid.HasValue)
        {
            var pieces = GetPiecesAt(hoverGrid.Value.X, hoverGrid.Value.Y);
            bool isValidMove = ValidMoves.Any(m => m.X == hoverGrid.Value.X && m.Y == hoverGrid.Value.Y);
            this.Cursor = (pieces.Count > 0 || isValidMove) ? Cursors.Hand : Cursors.Default;
        }
        else
        {
            this.Cursor = Cursors.Default;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hoverGrid = null;
        this.Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        // Chuột phải: Soi vành đai lửa của bất kỳ quân cờ nào có vành đai lửa (cả 2 bên)
        if (e.Button == MouseButtons.Right)
        {
            var grid = ScreenToGrid(e.X, e.Y);
            if (grid.HasValue)
            {
                var pieces = GetPiecesAt(grid.Value.X, grid.Value.Y);
                var firePiece = pieces.FirstOrDefault(p => HasFireRing(p.Kind));
                if (firePiece != null)
                {
                    if (InspectedFirePiece == firePiece)
                    {
                        InspectedFirePiece = null; // Click lại để tắt
                    }
                    else
                    {
                        InspectedFirePiece = firePiece;
                    }
                    OnFireRingInspected?.Invoke(InspectedFirePiece);
                    Invalidate();
                    return;
                }
            }

            // Click ra ô trống hoặc quân không có vành đai lửa -> tắt soi
            InspectedFirePiece = null;
            OnFireRingInspected?.Invoke(null);
            Invalidate();
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (isAnimatingExplosion) return;
        if (e.Button != MouseButtons.Left) return;

        var grid = ScreenToGrid(e.X, e.Y);
        if (!grid.HasValue) return;

        int gx = grid.Value.X;
        int gy = grid.Value.Y;

        var piecesAtTile = GetPiecesAt(gx, gy);
        var friendlyPieces = piecesAtTile.Where(p => p.Side == CurrentTurn).ToList();

        if (SelectedPiece == null)
        {
                if (friendlyPieces.Count > 1)
                {
                    // Có nhóm quân xếp chồng của bên hiện tại!
                    OnStackedGroupSelected?.Invoke(friendlyPieces);
                    // Mặc định chọn quân đầu tiên (ưu tiên quân chở)
                    var pick = friendlyPieces.FirstOrDefault(p => IsCarrier(p.Kind)) ?? friendlyPieces[0];
                    SelectedPiece = pick;
                    ValidMoves = GetValidMoves(SelectedPiece);
                    OnPieceSelected?.Invoke(SelectedPiece);
                    Invalidate();
                }
                else if (friendlyPieces.Count == 1)
                {
                    OnStackedGroupSelected?.Invoke(new List<PieceView>());
                    SelectedPiece = friendlyPieces[0];
                    ValidMoves = GetValidMoves(SelectedPiece);
                    OnPieceSelected?.Invoke(SelectedPiece);
                    Invalidate();
                }
        }
        else
        {
            // Đã chọn một quân trước đó:
                // Nếu click vào quân cùng phe (và ô đó không phải là nước đi ghép quân hợp lệ)
                if (friendlyPieces.Count > 0 && !ValidMoves.Any(m => m.X == gx && m.Y == gy))
                {
                    if (friendlyPieces.Count > 1)
                    {
                        OnStackedGroupSelected?.Invoke(friendlyPieces);
                        var pick = friendlyPieces.FirstOrDefault(p => IsCarrier(p.Kind)) ?? friendlyPieces[0];
                        SelectedPiece = pick;
                    }
                    else
                    {
                        OnStackedGroupSelected?.Invoke(new List<PieceView>());
                        SelectedPiece = friendlyPieces[0];
                    }
                    ValidMoves = GetValidMoves(SelectedPiece);
                    OnPieceSelected?.Invoke(SelectedPiece);
                    Invalidate();
                }
                else if (ValidMoves.Any(m => m.X == gx && m.Y == gy))
                {
                    // Nước đi nằm trong danh sách HỢP LỆ -> Thực hiện đi quân
                    var movingPiece = SelectedPiece;
                    MovePiece(movingPiece.PieceId, gx, gy);
                    OnStackedGroupSelected?.Invoke(new List<PieceView>());
                }
                else
                {
                    // Click vào ô không hợp lệ -> Bỏ chọn
                    SelectedPiece = null;
                    ValidMoves.Clear();
                    OnPieceSelected?.Invoke(null);
                    OnStackedGroupSelected?.Invoke(new List<PieceView>());
                    Invalidate();
                }
        }
    }
}
