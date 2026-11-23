from dataclasses import dataclass

COLS, ROWS = 11, 12

COMMANDER = "commander"
INFANTRY = "infantry"
TANK = "tank"
MILITIA = "militia"
ENGINEER = "engineer"
ARTILLERY = "artillery"
ANTI_AIR = "anti_air"
ROCKET = "rocket"
AIR_FORCE = "air_force"
WARSHIP = "warship"

SCORES = {
    COMMANDER: 100, INFANTRY: 10, MILITIA: 10, ENGINEER: 10, ANTI_AIR: 10,
    TANK: 20, ROCKET: 20, ARTILLERY: 30, AIR_FORCE: 40, WARSHIP: 80,
}

# (mã quân, loại, x, y) của phe đỏ; phe xanh đối xứng qua giữa bàn (y -> 11 - y)
RED_LAYOUT = [
    ("cmd", COMMANDER, 6, 0),
    ("ship1", WARSHIP, 1, 1), ("air1", AIR_FORCE, 4, 1), ("air2", AIR_FORCE, 8, 1),
    ("aa1", ANTI_AIR, 3, 2), ("rkt", ROCKET, 6, 2), ("aa2", ANTI_AIR, 9, 2),
    ("ship2", WARSHIP, 2, 3), ("art1", ARTILLERY, 4, 3), ("tank1", TANK, 5, 3),
    ("tank2", TANK, 7, 3), ("art2", ARTILLERY, 8, 3),
    ("inf1", INFANTRY, 2, 4), ("eng1", ENGINEER, 3, 4), ("mil", MILITIA, 6, 4),
    ("eng2", ENGINEER, 9, 4), ("inf2", INFANTRY, 10, 4),
]


@dataclass
class Piece:
    piece_id: str
    kind: str
    side: str
    x: int
    y: int
    alive: bool = True
    hero: bool = False
    score: int = 0


def initial_pieces():
    pieces = []
    for code, kind, x, y in RED_LAYOUT:
        pieces.append(Piece("r_" + code, kind, "red", x, y, score=SCORES[kind]))
        pieces.append(Piece("b_" + code, kind, "blue", x, ROWS - 1 - y, score=SCORES[kind]))
    return pieces


def in_bounds(x, y):
    return 0 <= x < COLS and 0 <= y < ROWS


def is_sea(x):
    return x < 2


def is_headquarter(x, y):
    return (x, y) in {(5, 1), (7, 1), (5, 10), (7, 10)}


def is_shallow_ford(x):
    return x in (5, 7)


def is_deep_river(x, y):
    return y in (5, 6) and x not in (5, 7)


def is_crossing_river(from_y, to_y):
    return (from_y <= 5 and to_y >= 6) or (from_y >= 6 and to_y <= 5)


def is_carrier(kind):
    return kind in (TANK, AIR_FORCE, WARSHIP)


def can_carry(carrier, passenger):
    if carrier in (TANK, AIR_FORCE):
        return passenger in (COMMANDER, INFANTRY, MILITIA)
    if carrier == WARSHIP:
        return passenger in (COMMANDER, INFANTRY, MILITIA, TANK, AIR_FORCE)
    return False


def can_stack(moving, occupants):
    if not occupants or len(occupants) >= 3:
        return False
    if any(p.side != moving.side for p in occupants):
        return False
    carrier = next((p for p in occupants if is_carrier(p.kind)), None)
    if carrier and can_carry(carrier.kind, moving.kind):
        return True
    return is_carrier(moving.kind) and all(can_carry(moving.kind, p.kind) for p in occupants)


DIRS4 = [(0, 1), (0, -1), (-1, 0), (1, 0)]
DIRS8 = DIRS4 + [(-1, 1), (1, 1), (-1, -1), (1, -1)]


def pieces_at(pieces, x, y):
    return [p for p in pieces if p.alive and p.x == x and p.y == y]


def has_enemy(piece, occupants):
    return any(p.side != piece.side for p in occupants)


def add_step_moves(pieces, piece, moves, max_steps, diagonal, engineer=False):
    for dx, dy in (DIRS8 if diagonal else DIRS4):
        for step in range(1, max_steps + 1):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty) or is_sea(tx) or is_headquarter(tx, ty):
                break
            if is_crossing_river(piece.y, ty) and not engineer and not is_shallow_ford(piece.x):
                break
            occs = pieces_at(pieces, tx, ty)
            if not occs:
                moves.add((tx, ty))
                continue
            if has_enemy(piece, occs) or can_stack(piece, occs):
                moves.add((tx, ty))
            break


def add_shore_shot(pieces, piece, moves):
    # Đứng ở bờ (x = 2) bắn quân địch trên biển ngay cạnh (x = 1)
    if piece.x == 2 and has_enemy(piece, pieces_at(pieces, 1, piece.y)):
        moves.add((1, piece.y))


def commander_moves(pieces, piece, moves, bonus):
    for dx, dy in DIRS4:
        step = 1
        while True:
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty) or is_sea(tx):
                break
            occs = pieces_at(pieces, tx, ty)
            if not occs:
                moves.add((tx, ty))
            else:
                if has_enemy(piece, occs):
                    if step <= 1 + bonus:
                        moves.add((tx, ty))
                elif can_stack(piece, occs):
                    moves.add((tx, ty))
                break
            step += 1


def valid_moves(pieces, piece):
    moves = set()
    if not piece.alive:
        return moves
    bonus = 1 if piece.hero else 0

    if piece.kind == COMMANDER:
        commander_moves(pieces, piece, moves, bonus)
    elif piece.kind == INFANTRY:
        add_step_moves(pieces, piece, moves, 1 + bonus, diagonal=piece.hero)
        add_shore_shot(pieces, piece, moves)
    elif piece.kind == MILITIA:
        add_step_moves(pieces, piece, moves, 1 + bonus, diagonal=True)
        add_shore_shot(pieces, piece, moves)
    elif piece.kind == ENGINEER:
        add_step_moves(pieces, piece, moves, 1 + bonus, diagonal=piece.hero, engineer=True)
    elif piece.kind == ANTI_AIR:
        add_step_moves(pieces, piece, moves, 1 + bonus, diagonal=piece.hero)
    elif piece.kind == TANK:
        tank_moves(pieces, piece, moves, bonus)
    elif piece.kind == ARTILLERY:
        artillery_moves(pieces, piece, moves, bonus)
    elif piece.kind == ROCKET:
        rocket_moves(pieces, piece, moves, bonus)
    elif piece.kind == AIR_FORCE:
        air_force_moves(pieces, piece, moves, bonus)
    elif piece.kind == WARSHIP:
        warship_moves(pieces, piece, moves)

    return moves


def tank_moves(pieces, piece, moves, bonus):
    for dx, dy in DIRS4:
        for step in range(1, 2 + bonus + 1):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty):
                break
            if is_sea(tx):
                # Không chạy xuống biển, nhưng được đứng tại chỗ bắn mục tiêu trên biển
                if has_enemy(piece, pieces_at(pieces, tx, ty)):
                    moves.add((tx, ty))
                break
            if is_crossing_river(piece.y, ty) and not is_shallow_ford(piece.x):
                break
            occs = pieces_at(pieces, tx, ty)
            if not occs:
                if not is_headquarter(tx, ty):
                    moves.add((tx, ty))
                continue
            if has_enemy(piece, occs):
                if not is_headquarter(tx, ty):
                    moves.add((tx, ty))
            elif can_stack(piece, occs):
                moves.add((tx, ty))
            break
def rocket_moves(pieces, piece, moves, bonus):
    # Vành đai hỏa lực: 2 ô theo trục, 1 ô theo đường chéo
    for i, (dx, dy) in enumerate(DIRS8):
        max_range = (1 if i >= 4 else 2) + bonus
        for step in range(1, max_range + 1):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty):
                break
            occs = pieces_at(pieces, tx, ty)
            if not occs:
                if not is_sea(tx) and not is_headquarter(tx, ty):
                    moves.add((tx, ty))
                continue
            if has_enemy(piece, occs):
                if not is_headquarter(tx, ty):
                    moves.add((tx, ty))
            elif can_stack(piece, occs):
                moves.add((tx, ty))
            break


def air_force_moves(pieces, piece, moves, bonus):
    # Bay vượt khối chắn: không dừng khi gặp quân
    for dx, dy in DIRS8:
        for step in range(1, 4 + bonus + 1):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty):
                break
            occs = pieces_at(pieces, tx, ty)
            if not occs:
                if not is_sea(tx) and not is_headquarter(tx, ty):
                    moves.add((tx, ty))
            elif has_enemy(piece, occs):
                if not is_headquarter(tx, ty):
                    moves.add((tx, ty))
            elif can_stack(piece, occs):
                moves.add((tx, ty))


def warship_moves(pieces, piece, moves):
    for dx, dy in DIRS8:
        for step in range(1, 5):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty):
                break
            occs = pieces_at(pieces, tx, ty)
            if tx <= 2 or is_deep_river(tx, ty):
                # Vùng tàu đi được: biển, dải bờ (x = 2), đoạn sông sâu
                if not occs:
                    moves.add((tx, ty))
                    continue
                if has_enemy(piece, occs) or can_stack(piece, occs):
                    moves.add((tx, ty))
                break
            # Gặp đất liền: pháo hạm đứng tại chỗ bắn mục tiêu cách tối đa 3 ô
            if step <= 3 and has_enemy(piece, occs) and not is_headquarter(tx, ty):
                moves.add((tx, ty))
            break

def artillery_moves(pieces, piece, moves, bonus):
    for dx, dy in DIRS8:
        screened = False  # đã gặp một quân chắn trên đường bắn chưa
        for step in range(1, 3 + bonus + 1):
            tx, ty = piece.x + dx * step, piece.y + dy * step
            if not in_bounds(tx, ty):
                break
            occs = pieces_at(pieces, tx, ty)
            if not screened:
                if not occs:
                    if not is_sea(tx) and not is_headquarter(tx, ty):
                        moves.add((tx, ty))
                    continue
                if has_enemy(piece, occs) and not is_headquarter(tx, ty):
                    moves.add((tx, ty))
                elif can_stack(piece, occs):
                    moves.add((tx, ty))
                screened = True
            elif occs:
                # Bắn vượt qua đúng một quân chắn
                if has_enemy(piece, occs) and not is_headquarter(tx, ty):
                    moves.add((tx, ty))
                break

def has_multiple_capture_modes(kind):
    return kind in (AIR_FORCE, ARTILLERY, WARSHIP, TANK)


def find_piece(pieces, piece_id):
    return next((p for p in pieces if p.piece_id == piece_id and p.alive), None)


def in_air_defense(pieces, attacker, x, y):
    for p in pieces:
        if not p.alive or p.side == attacker.side:
            continue
        if p.kind not in (ANTI_AIR, ROCKET, WARSHIP):
            continue
        radius = 2 if p.kind == ROCKET else 1
        if abs(p.x - x) <= radius and abs(p.y - y) <= radius:
            return True
    return False


def apply_move(pieces, side, piece_id, to_x, to_y, capture_mode="occupy", move_stack=True):
    """Kiểm tra rồi thực hiện một nước đi. Trả về (mã lỗi, None) hoặc (None, kết quả)."""
    piece = find_piece(pieces, piece_id)
    if piece is None:
        return "PIECE_NOT_FOUND", None
    if piece.side != side:
        return "NOT_YOUR_PIECE", None
    if (to_x, to_y) not in valid_moves(pieces, piece):
        return "ILLEGAL_MOVE", None

    from_x, from_y = piece.x, piece.y
    enemies = [p for p in pieces_at(pieces, to_x, to_y) if p.side != side]
    target = enemies[0] if enemies else None
    stand_shoot = False

    if enemies:
        if capture_mode == "stand" and has_multiple_capture_modes(piece.kind):
            stand_shoot = True
        elif piece.kind in (TANK, ARTILLERY) and is_sea(to_x):
            stand_shoot = True
        elif piece.kind == WARSHIP and not is_sea(to_x) and not is_deep_river(to_x, to_y):
            stand_shoot = True

        for e in enemies:
            e.alive = False
            if e.kind == COMMANDER:
                piece.hero = True
                piece.score += 10

        # Máy bay lao vào vùng phòng không của địch thì bị bắn rơi
        if piece.kind == AIR_FORCE and target.kind != AIR_FORCE and not piece.hero:
            if in_air_defense(pieces, piece, to_x, to_y):
                piece.alive = False

    moved = []
    if not stand_shoot:
        group = [piece]
        if move_stack and is_carrier(piece.kind):
            group += [p for p in pieces_at(pieces, from_x, from_y) if p is not piece and p.side == side]
        for p in group:
            p.x, p.y = to_x, to_y
        moved = [p.piece_id for p in group]

    return None, {
        "piece_id": piece_id,
        "from": [from_x, from_y],
        "to": [to_x, to_y],
        "captured": [e.piece_id for e in enemies],
        "stand_shoot": stand_shoot,
        "moved": moved,
        "attacker_destroyed": not piece.alive,
    }

GROUND_FORCES = (TANK, INFANTRY, ARTILLERY)


def other_side(side):
    return "blue" if side == "red" else "red"


def check_winner(pieces, next_side):
    """Gọi sau mỗi nước đi. Trả về (phe thắng, lý do) hoặc (None, None) nếu ván chưa kết thúc."""
    for side in ("red", "blue"):
        alive = [p for p in pieces if p.alive and p.side == side]
        if not any(p.kind == COMMANDER for p in alive):
            return other_side(side), "commander_captured"
        if not any(p.kind in GROUND_FORCES for p in alive):
            return other_side(side), "no_ground_forces"

    # Phe đến lượt mà không còn nước đi hợp lệ nào thì thua
    if not any(valid_moves(pieces, p) for p in pieces if p.alive and p.side == next_side):
        return other_side(next_side), "no_legal_moves"

    return None, None