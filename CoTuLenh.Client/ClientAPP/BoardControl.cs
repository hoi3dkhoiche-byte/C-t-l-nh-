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

    public BoardControl()
    {
        this.DoubleBuffered = true;
        this.BackColor = Color.FromArgb(24, 32, 38); // Màu nền quân sự cao cấp bao quanh bản đồ
        this.Size = new Size(620, 700);
        terrainImage = LoadTerrainMap();
        InitializeStandardPieces();
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

        // Y = 1: Hải quân, Không quân
        Pieces.Add(new PieceView { PieceId = "r_ship1", Kind = PieceKind.Warship, Side = "red", X = 1, Y = 1, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "r_air1", Kind = PieceKind.AirForce, Side = "red", X = 4, Y = 1, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(4, 1) });
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

        // Y = 10: Hải quân, Không quân
        Pieces.Add(new PieceView { PieceId = "b_ship1", Kind = PieceKind.Warship, Side = "blue", X = 1, Y = 10, DisplayName = "Tàu Chiến", Notation = "H", Score = 80 });
        Pieces.Add(new PieceView { PieceId = "b_air1", Kind = PieceKind.AirForce, Side = "blue", X = 4, Y = 10, DisplayName = "Không Quân", Notation = "K", Score = 40, AirportLocation = new Point(4, 10) });
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
        if (carrier == PieceKind.Tank || carrier == PieceKind.AirForce)
        {
            return passenger == PieceKind.Commander || passenger == PieceKind.Infantry || passenger == PieceKind.Militia;
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
        if (targetOccupants.Count >= 3) return false; // Tối đa 3 quân trong 1 cụm ghép

        var carrier = targetOccupants.FirstOrDefault(p => IsCarrier(p.Kind));
        if (carrier != null && CanCarry(carrier.Kind, movingPiece.Kind))
        {
            return true;
        }

        if (IsCarrier(movingPiece.Kind) && targetOccupants.All(p => CanCarry(movingPiece.Kind, p.Kind)))
        {
            return true;
        }

        return false;
    }

    public static bool HasMultipleCaptureModes(PieceKind kind)
    {
        return kind == PieceKind.AirForce || kind == PieceKind.Artillery || kind == PieceKind.Warship || kind == PieceKind.Tank;
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
        if (!piece.IsAlive) return;
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
            case PieceKind.Commander: // Tư Lệnh
                // Đi theo trục dọc ngang không hạn chế, miễn không vướng quân chắn
                // Nhưng khi ăn quân đối phương: chỉ được ăn trong phạm vi 1 nấc
                for (int i = 0; i < 4; i++)
                {
                    for (int step = 1; step < Math.Max(COLS, ROWS); step++)
                    {
                        int tx = piece.X + DirX4[i] * step;
                        int ty = piece.Y + DirY4[i] * step;
                        if (!InBounds(tx, ty)) break;
                        if (IsSea(tx)) break; // Tư lệnh không nhảy xuống biển

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
                                if (step <= 1 + bonus) moves.Add(new Point(tx, ty));
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
                // Đi và ăn thẳng dọc ngang từng đoạn 1 (bán kính 1)
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false);
                // Bắn quân trên biển: Đứng tại chỗ bờ biển (X=2) ăn quân trên biển (X=1) cách 1 đoạn
                if (piece.X == 2)
                {
                        var seaEnemies = GetPiecesAt(1, piece.Y).Where(p => p.Side != piece.Side).ToList();
                    if (seaEnemies.Count > 0)
                        moves.Add(new Point(1, piece.Y));
                }
                break;

            case PieceKind.Militia: // Dân Quân Tóc Dài
                // Đi tiến, lùi, ngang VÀ đi/ăn chéo 45 độ từng đoạn 1 (tất cả 8 hướng)
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: true, canEnterSea: false);
                if (piece.X == 2)
                {
                        var seaEnemies = GetPiecesAt(1, piece.Y).Where(p => p.Side != piece.Side).ToList();
                    if (seaEnemies.Count > 0)
                        moves.Add(new Point(1, piece.Y));
                }
                break;

            case PieceKind.Engineer: // Công Binh
                // Đi tiến, lùi, ngang từng đoạn 1; có thể bắc cầu vượt sông ở bất kỳ vị trí nào
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false, isEngineer: true);
                break;

            case PieceKind.AntiAir: // Cao Xạ
                // Đi và ăn thẳng dọc ngang từng đoạn 1
                AddStepMoves(piece, moves, 1 + bonus, allowDiagonal: piece.IsHero, canEnterSea: false);
                break;

            case PieceKind.Tank: // Xe Tăng
                // Đi và ăn thẳng dọc ngang từ 1 đến 2 đoạn (không vượt quân cản)
                int maxTankRange = 2 + bonus;
                for (int i = 0; i < 4; i++)
                {
                    for (int step = 1; step <= maxTankRange; step++)
                    {
                        int tx = piece.X + DirX4[i] * step;
                        int ty = piece.Y + DirY4[i] * step;
                        if (!InBounds(tx, ty)) break;

                        // Nếu là mục tiêu trên biển: Xe tăng được đứng tại chỗ bắn ăn mục tiêu trong tầm
                        if (IsSea(tx))
                        {
                                var target = GetPiecesAt(tx, ty).FirstOrDefault(p => p.Side != piece.Side);
                            if (target != null)
                                moves.Add(new Point(tx, ty));
                            break; // Không thể lái xe xuống biển
                        }

                        // Vượt sông: Chỉ được qua ở ngầm cạn (X=5 hoặc X=7)
                        if (IsCrossingRiver(piece.Y, ty) && !IsShallowFord(piece.X)) break;

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
                                if (!IsHeadquarter(tx, ty)) moves.Add(new Point(tx, ty));
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
                                    if (!IsSea(tx) && !IsHeadquarter(tx, ty))
                                        moves.Add(new Point(tx, ty));
                                }
                                else
                                {
                                    var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                    if (enemy != null && !IsHeadquarter(tx, ty))
                                    {
                                        moves.Add(new Point(tx, ty)); // Bắn trực tiếp
                                    }
                                    else if (CanStack(piece, occs))
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
                                    if (enemy != null && !IsHeadquarter(tx, ty))
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
                        if (!InBounds(tx, ty)) continue;

                            var occs = GetPiecesAt(tx, ty);
                        if (occs.Count == 0)
                        {
                            if (!IsSea(tx) && !IsHeadquarter(tx, ty))
                                moves.Add(new Point(tx, ty));
                        }
                        else
                                {
                                    var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                    if (enemy != null)
                                    {
                                        if (!IsHeadquarter(tx, ty)) moves.Add(new Point(tx, ty));
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
                                if (!IsHeadquarter(tx, ty)) moves.Add(new Point(tx, ty));
                            }
                            else if (CanStack(piece, occs))
                            {
                                moves.Add(new Point(tx, ty));
                            }
                        }
                    }
                }
                break;

            case PieceKind.Warship: // Tàu Chiến
                // Tàu chiến hoạt động ở vùng biển (X <= 2) và đoạn sông sâu
                // Tên lửa hải đối hải: đi và ăn thẳng/chéo 1-4 đoạn trên biển
                for (int dir = 0; dir < 8; dir++)
                {
                    for (int step = 1; step <= 4; step++)
                    {
                        int tx = piece.X + DirX8[dir] * step;
                        int ty = piece.Y + DirY8[dir] * step;
                        if (!InBounds(tx, ty)) break;

                        bool isNavigable = (tx <= 2) || (IsDeepRiver(tx, ty));
                        var occs = GetPiecesAt(tx, ty);

                        if (isNavigable)
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
                                    break;
                                }
                        }
                        else
                        {
                            // Pháo hạm bắn mục tiêu trên đất liền (1-3 đoạn) đứng tại chỗ
                                if (step <= 3 && occs.Count > 0)
                                {
                                    var enemy = occs.FirstOrDefault(p => p.Side != piece.Side);
                                    if (enemy != null && !IsHeadquarter(tx, ty))
                                    {
                                        moves.Add(new Point(tx, ty));
                                    }
                                }
                            break;
                        }
                    }
                }
                break;
        }

        return moves.Distinct().ToList();
    }

    private void AddStepMoves(PieceView piece, List<Point> moves, int maxSteps, bool allowDiagonal, bool canEnterSea, bool isEngineer = false)
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
                if (IsHeadquarter(tx, ty)) break; // Chỉ tư lệnh mới vào được sở chỉ huy

                // Kiểm tra vượt sông (giữa hàng 5 và 6):
                if (IsCrossingRiver(piece.Y, ty) && !isEngineer && !IsShallowFord(piece.X))
                {
                    break; // Không có ngầm cạn hoặc công binh thì không vượt được sông
                }

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
                        moves.Add(new Point(tx, ty));
                    }
                    else if (CanStack(piece, occs))
                    {
                        moves.Add(new Point(tx, ty));
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

    /// <summary>
    /// Thực hiện di chuyển hoặc ăn quân kèm theo xử lý luật và xác nhận Client-Server
    /// </summary>
    public void MovePiece(string pieceId, int toX, int toY)
    {
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

        bool isStandShoot = false;

            if (targetEnemies.Count > 0)
            {
                // Kiểm tra chế độ ăn quân: Bắn tại chỗ / Oanh tạc quay về
                if (CurrentCaptureMode == CaptureMode.StandStill && HasMultipleCaptureModes(piece.Kind))
                {
                    isStandShoot = true;
                }
                else
                {
                    // Quy tắc đứng tại chỗ ăn quân bắt buộc theo địa hình
                    if (piece.Kind == PieceKind.Tank && IsSea(toX)) isStandShoot = true;
                    if (piece.Kind == PieceKind.Artillery && IsSea(toX)) isStandShoot = true;
                    if (piece.Kind == PieceKind.Warship && !IsSea(toX) && !IsDeepRiver(toX, toY)) isStandShoot = true;
                }

                // Tiêu diệt toàn bộ quân địch tại ô đích (bao gồm cụm quân đang ghép)
                foreach (var te in targetEnemies)
                {
                    te.IsAlive = false;
                    if (te.Kind == PieceKind.Commander)
                    {
                        piece.IsHero = true;
                        piece.Score += 10;
                    }
                }

                    // Quy tắc máy bay bay vào vùng hỏa lực phòng không
                    if (piece.Kind == PieceKind.AirForce && primaryTarget != null && primaryTarget.Kind != PieceKind.AirForce)
                    {
                        bool inEnemyAirDefense = Pieces.Any(p => p.Side != piece.Side && p.IsAlive &&
                            (p.Kind == PieceKind.AntiAir || p.Kind == PieceKind.Rocket || p.Kind == PieceKind.Warship) &&
                            (Math.Abs(p.X - toX) <= (p.Kind == PieceKind.Rocket ? 2 : 1) && Math.Abs(p.Y - toY) <= (p.Kind == PieceKind.Rocket ? 2 : 1)));

                        if (inEnemyAirDefense && !piece.IsHero)
                        {
                            piece.IsAlive = false;
                        }
                    }
            }

            // Di chuyển vị trí quân
            if (!isStandShoot)
            {
                    // Nếu quân di chuyển là quân cõng (Xe tăng, Máy bay, Tàu chiến) và đang bật cõng cả cụm
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

        // Đổi lượt đi
        CurrentTurn = CurrentTurn == "red" ? "blue" : "red";
        SelectedPiece = null;
        ValidMoves.Clear();
        Invalidate();

        OnPieceMoved?.Invoke(piece, toX, toY, primaryTarget);
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

        // Khi người chơi click chuột phải vào bất kỳ quân có vành đai lửa (cả 2 bên đều xem được)
        if (InspectedFirePiece != null && InspectedFirePiece.IsAlive && InspectedFirePiece != SelectedPiece)
        {
            DrawFireThreatCircles(g, InspectedFirePiece, isInspected: true);
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

    private void DrawFireThreatCircles(Graphics g, PieceView piece, bool isInspected = false)
    {
        var pt = GridToScreen(piece.X, piece.Y);
        float ringRadiusCells = GetFireRingRadius(piece.Kind);
        if (ringRadiusCells <= 0) return;

        float radius = ringRadiusCells * cellWidth;

            Color baseColor = isInspected
                ? Color.FromArgb(243, 156, 18) // Hổ phách/vàng rực khi soi bằng chuột phải
                : (piece.Side == "red" ? Color.FromArgb(231, 76, 60) : Color.FromArgb(52, 152, 219));

        // 1. Vùng bóng lửa mờ bao phủ
        using (var zoneBrush = new SolidBrush(Color.FromArgb(30, baseColor)))
        {
            g.FillEllipse(zoneBrush, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }

        // 2. Viền đứt đoạn vành đai lửa
        using (var ringPen = new Pen(Color.FromArgb(210, baseColor), isInspected ? 2.5f : 2.0f))
        {
            ringPen.DashStyle = DashStyle.Dash;
            g.DrawEllipse(ringPen, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }

        // 3. Vòng tròn phụ bên trong
        using (var innerPen = new Pen(Color.FromArgb(90, baseColor), 1.0f))
        {
            g.DrawEllipse(innerPen, pt.X - radius * 0.7f, pt.Y - radius * 0.7f, radius * 1.4f, radius * 1.4f);
        }

        // 4. Nhãn vành đai lửa
        string label = isInspected
            ? $"🔥 [SOI CHUỘT PHẢI] {piece.DisplayName} ({(piece.Side == "red" ? "Đỏ" : "Xanh")}) • R={ringRadiusCells}"
            : $"🔥 VÀNH ĐAI LỬA: {piece.DisplayName} • R={ringRadiusCells}";

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
