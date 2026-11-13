import os
from pathlib import Path
import secrets
import string
import threading

from dotenv import load_dotenv
from flask import Flask, jsonify, request
from flask_migrate import Migrate
from flask_socketio import SocketIO, join_room, leave_room, emit
from sqlalchemy import text
from flask_jwt_extended import JWTManager, create_access_token, jwt_required, get_jwt_identity, decode_token


from flask_bcrypt import Bcrypt
from models import db, User, Room, Match
from pwdrs_routes import reset_bp 

load_dotenv(Path(__file__).resolve().with_name(".env"))

app = Flask(__name__)

# Cấu hình JWT (có fallback an toàn khi chưa điền env)
app.config["JWT_SECRET_KEY"] = os.getenv("JWT_SECRET_KEY") or "cotulenh-dev-jwt-secret-key-min-32-chars-replace-in-production"
jwt = JWTManager(app)
bcrypt = Bcrypt(app)

# Cấu hình Database (Hỗ trợ AWS RDS và Local)
database_url = os.getenv("DATABASE_URL")
if not database_url:
    # Fallback cho chạy test cục bộ nếu chưa set DATABASE_URL
    database_url = "sqlite:///cotulenh.db"

app.config["SQLALCHEMY_DATABASE_URI"] = database_url
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

# Cấu hình Pool tối ưu cho AWS RDS:
# - pool_pre_ping: Tự kiểm tra kết nối còn sống trước khi query (tránh lỗi "MySQL server has gone away")
# - pool_recycle: Tự làm mới kết nối sau 280 giây (tránh bị AWS RDS / NAT timeout cắt ngang)
app.config["SQLALCHEMY_ENGINE_OPTIONS"] = {
    "pool_pre_ping": True,
    "pool_recycle": 280,
}

db.init_app(app)
migrate = Migrate(app, db)
app.register_blueprint(reset_bp)
socketio = SocketIO(app, async_mode="threading")


@app.get("/health")  #Trạng thái server
def health():
    return jsonify({"status": "ok"})


@app.get("/db-health") #Trạng thái database trên server
def db_health():
    db.session.execute(text("SELECT 1"))
    return jsonify({"database": "connected"})

def generate_room_code():
    alphabet = string.ascii_uppercase + string.digits

    while True:
        code = "".join(secrets.choice(alphabet) for _ in range(6))
        if not Room.query.filter_by(room_code=code).first():
            return code


@app.post("/api/rooms")
@jwt_required()
def create_room():
    user_id = int(get_jwt_identity())

    if db.session.get(User, user_id) is None:
        return jsonify({"error": "User not found"}), 404

    room = Room(
        room_code=generate_room_code(),
        host_user_id=user_id,
        status="waiting",
    )
    db.session.add(room)
    db.session.commit()

    return jsonify({
        "message": "Room created",
        "room": {
            "id": room.id,
            "room_code": room.room_code,
            "host_user_id": room.host_user_id,
            "status": room.status,
        },
    }), 201


@app.get("/api/rooms")
@jwt_required()
def list_rooms():
    rooms = Room.query.filter_by(status="waiting").order_by(
        Room.created_at.desc()
    ).all()

    return jsonify({
        "rooms": [
            {
                "id": room.id,
                "room_code": room.room_code,
                "host_user_id": room.host_user_id,
                "status": room.status,
                "created_at": room.created_at.isoformat(),
            }
            for room in rooms
        ]
    }), 200




@app.post("/api/auth/register")
def register(): #API đăng ký tài khoản
    data = request.get_json(silent=True)

    if not isinstance(data, dict):
        return jsonify({"error": "Request body must be JSON"}), 400

    username = data.get("username") #Tên đăng ký
    email = data.get("email") #Email đăng ký
    password = data.get("password") #Mật khẩu

    if not all(isinstance(value, str) for value in (username, email, password)):
        return jsonify({"error": "username, email, and password are required"}), 400

    username = username.strip()
    email = email.strip().lower()

    if not username or not email or len(password) < 8:
        return jsonify({"error": "Check username, email, and password (minimum 8 characters)"}), 400

    existing_user = User.query.filter(
        (User.username == username) | (User.email == email)
    ).first()

    if existing_user:
        return jsonify({"error": "Username or email is already registered"}), 409

    password_hash = bcrypt.generate_password_hash(password).decode("utf-8") #Mã hóa mật khẩu

    user = User( #Thông tin tài khoản
        username=username,
        email=email,
        password_hash=password_hash,
    )
    db.session.add(user)
    db.session.commit()

    return jsonify({ #Trả về trạng thái tạo tài khoản (thành công/thất bại...)
        "message": "Account created",
        "user": {
            "id": user.id,
            "username": user.username,
            "email": user.email,
        },
    }), 201

@app.post("/api/auth/login")
def login(): #API đăng nhập
    data = request.get_json(silent=True)

    if not isinstance(data, dict):
        return jsonify({"error": "Request body must be JSON"}), 400

    identifier = data.get("identifier")
    password = data.get("password")

    if not isinstance(identifier, str) or not isinstance(password, str):
        return jsonify({"error": "identifier and password are required"}), 400

    identifier = identifier.strip()

    user = User.query.filter(
        (User.username == identifier) |
        (User.email == identifier.lower())
    ).first()

    if user is None or not bcrypt.check_password_hash(user.password_hash, password):
        return jsonify({"error": "Invalid username/email or password"}), 401

    access_token = create_access_token(identity=str(user.id)) #Tạo token cho mỗi account mới

    return jsonify({
        "access_token": access_token,
        "user": {
            "id": user.id,
            "username": user.username,
            "email": user.email,
            "coins": user.coins or 0,
            "elo_rating": user.elo_rating or 1200,
            "wins": user.wins or 0,
            "losses": user.losses or 0,
            "draws": user.draws or 0,
        },
    }), 200

@app.get("/api/auth/me")
@jwt_required()
def get_current_user():
    user_id = int(get_jwt_identity())
    user = db.session.get(User, user_id)

    if user is None:
        return jsonify({"error": "User not found"}), 404

    return jsonify({
        "user": {
            "id": user.id,
            "username": user.username,
            "email": user.email,
            "coins": user.coins or 0,
            "elo_rating": user.elo_rating or 1200,
            "wins": user.wins or 0,
            "losses": user.losses or 0,
            "draws": user.draws or 0,
        }
    }), 200


# =============================================================================
# LEADERBOARD & REALTIME MATCH ENGINE
# =============================================================================

def calculate_elo(red_elo, blue_elo, result, k=32):
    """
    Thuật toán ELO chuẩn quốc tế:
    - result: 'red_win', 'blue_win', 'draw'
    - k: hệ số biến động (mặc định 32)
    """
    expected_red = 1.0 / (1.0 + 10.0 ** ((blue_elo - red_elo) / 400.0))
    expected_blue = 1.0 - expected_red

    if result == "red_win":
        actual_red, actual_blue = 1.0, 0.0
    elif result == "blue_win":
        actual_red, actual_blue = 0.0, 1.0
    else:  # draw
        actual_red, actual_blue = 0.5, 0.5

    delta_red = round(k * (actual_red - expected_red))
    delta_blue = round(k * (actual_blue - expected_blue))
    return delta_red, delta_blue


def get_top_leaderboard(limit=50):
    """Lấy danh sách Top người chơi có ELO cao nhất"""
    users = User.query.order_by(User.elo_rating.desc(), User.wins.desc()).limit(limit).all()
    result = []
    for rank, u in enumerate(users, start=1):
        total = (u.wins or 0) + (u.losses or 0) + (u.draws or 0)
        win_rate = round((u.wins / total * 100), 1) if total > 0 else 0.0
        result.append({
            "rank": rank,
            "id": u.id,
            "username": u.username,
            "elo_rating": u.elo_rating or 1200,
            "wins": u.wins or 0,
            "losses": u.losses or 0,
            "draws": u.draws or 0,
            "win_rate": win_rate,
            "coins": u.coins or 0,
        })
    return result


@app.get("/api/leaderboard")
def get_leaderboard():
    """API lấy danh sách bảng xếp hạng ELO"""
    top = get_top_leaderboard(50)
    return jsonify(top), 200


@app.post("/api/matches/finish")
@jwt_required()
def finish_match():
    """
    API ghi nhận kết thúc ván đấu:
    - Tính toán ELO mới cho 2 bên
    - Cập nhật số trận thắng/thua/hòa và số dư xu
    - Lưu vào bảng matches
    - Phát sóng WebSocket 'leaderboard_updated' cho toàn bộ client kết nối
    """
    current_user_id = int(get_jwt_identity())
    user = db.session.get(User, current_user_id)
    if not user:
        return jsonify({"error": "User not found"}), 404

    data = request.get_json(silent=True) or {}
    room_code = data.get("room_code", "SOLO")
    result = data.get("result", "red_win")  # 'red_win', 'blue_win', 'draw'
    bet_amount = max(0, int(data.get("bet_amount", 0)))
    opponent_id = data.get("opponent_id")

    # Xác định đối thủ (nếu là trận đấu với bot/người chơi khác)
    opponent = None
    if opponent_id and isinstance(opponent_id, int):
        opponent = db.session.get(User, opponent_id)

    red_elo = user.elo_rating or 1200
    blue_elo = opponent.elo_rating if opponent else 1200

    delta_red, delta_blue = calculate_elo(red_elo, blue_elo, result)

    # Đảm bảo ELO tối thiểu không âm
    user.elo_rating = max(100, red_elo + delta_red)

    if result == "red_win":
        user.wins = (user.wins or 0) + 1
        user.coins = (user.coins or 0) + bet_amount
        winner_id = user.id
    elif result == "blue_win":
        user.losses = (user.losses or 0) + 1
        user.coins = max(0, (user.coins or 0) - bet_amount)
        winner_id = opponent.id if opponent else None
    else:
        user.draws = (user.draws or 0) + 1
        winner_id = None

    if opponent:
        opponent.elo_rating = max(100, blue_elo + delta_blue)
        if result == "blue_win":
            opponent.wins = (opponent.wins or 0) + 1
            opponent.coins = (opponent.coins or 0) + bet_amount
        elif result == "red_win":
            opponent.losses = (opponent.losses or 0) + 1
            opponent.coins = max(0, (opponent.coins or 0) - bet_amount)
        else:
            opponent.draws = (opponent.draws or 0) + 1

    match = Match(
        room_code=room_code,
        red_player_id=user.id,
        blue_player_id=opponent.id if opponent else user.id,
        winner_id=winner_id,
        result=result,
        red_elo_change=delta_red,
        blue_elo_change=delta_blue,
        coin_bet=bet_amount,
    )
    db.session.add(match)
    db.session.commit()

    # Phát sóng Real-time cập nhật Bảng Xếp Hạng tới TẤT CẢ client
    updated_leaderboard = get_top_leaderboard(50)
    socketio.emit("leaderboard_updated", {
        "leaderboard": updated_leaderboard,
        "affected_user_id": user.id,
        "elo_change": delta_red,
    })

    return jsonify({
        "success": True,
        "message": "Trận đấu đã được ghi nhận thành công!",
        "new_elo": user.elo_rating,
        "elo_change": delta_red,
        "new_coins": user.coins,
        "user": {
            "id": user.id,
            "username": user.username,
            "elo_rating": user.elo_rating,
            "wins": user.wins,
            "losses": user.losses,
            "draws": user.draws,
            "coins": user.coins,
        }
    }), 200


@app.get("/api/matches/history")
@jwt_required()
def get_match_history():
    """Lấy danh sách các trận đấu gần nhất của người dùng"""
    user_id = int(get_jwt_identity())
    matches = Match.query.filter(
        (Match.red_player_id == user_id) | (Match.blue_player_id == user_id)
    ).order_by(Match.created_at.desc()).limit(20).all()

    history = []
    for m in matches:
        is_red = (m.red_player_id == user_id)
        opponent_id = m.blue_player_id if is_red else m.red_player_id
        opponent = db.session.get(User, opponent_id) if opponent_id != user_id else None
        opp_name = opponent.username if opponent else "Thiếu Tá Quân Khu (Bot)"

        if m.result == "draw":
            outcome = "Hòa"
        elif (m.result == "red_win" and is_red) or (m.result == "blue_win" and not is_red):
            outcome = "Thắng"
        else:
            outcome = "Thua"

        elo_delta = m.red_elo_change if is_red else m.blue_elo_change
        coin_delta = m.coin_bet if outcome == "Thắng" else (-m.coin_bet if outcome == "Thua" else 0)

        history.append({
            "id": m.id,
            "room_code": m.room_code or "CTL-SOLO",
            "opponent_name": opp_name,
            "side": "red" if is_red else "blue",
            "result": outcome,
            "elo_change": elo_delta,
            "coin_change": coin_delta,
            "played_at": m.created_at.isoformat(),
        })

    return jsonify(history), 200


# =============================================================================
# SOCKET.IO REALTIME EVENTS
# =============================================================================

connected_users = {}

@socketio.on("connect")
def handle_connect(auth=None):
    token = auth.get("token") if isinstance(auth, dict) else None

    if not token:
        print(f"[SocketIO] Khách kết nối: {request.sid}")
        return

    try:
        decoded = decode_token(token)
    except Exception:
        print(f"[SocketIO] Từ chối, token không hợp lệ: {request.sid}")
        return False

    connected_users[request.sid] = int(decoded["sub"])
    print(f"[SocketIO] User {decoded['sub']} kết nối: {request.sid}")


@socketio.on("disconnect")
def handle_disconnect(reason=None):
    user_id = connected_users.pop(request.sid, None)
    if not hold_seat(request.sid, user_id):
        remove_from_room(request.sid, user_id)
    print(f"[SocketIO] Ngắt kết nối: {request.sid} (user {user_id})")


active_rooms = {}  

def record_match(room_code, red_id, blue_id, result, bet=0):
    red = db.session.get(User, red_id)
    blue = db.session.get(User, blue_id)
    red_elo = red.elo_rating or 1200
    blue_elo = blue.elo_rating or 1200
    delta_red, delta_blue = calculate_elo(red_elo, blue_elo, result)
    red.elo_rating = max(100, red_elo + delta_red)
    blue.elo_rating = max(100, blue_elo + delta_blue)

    if result == "red_win":
        winner, loser = red, blue
    elif result == "blue_win":
        winner, loser = blue, red
    else:
        winner, loser = None, None

    if winner:
        winner.wins = (winner.wins or 0) + 1
        loser.losses = (loser.losses or 0) + 1
        paid = min(bet, loser.coins or 0)
        winner.coins = (winner.coins or 0) + paid
        loser.coins = (loser.coins or 0) - paid
    else:
        red.draws = (red.draws or 0) + 1
        blue.draws = (blue.draws or 0) + 1

    db.session.add(Match(
        room_code=room_code,
        red_player_id=red.id,
        blue_player_id=blue.id,
        winner_id=winner.id if winner else None,
        result=result,
        red_elo_change=delta_red,
        blue_elo_change=delta_blue,
        coin_bet=bet,
    ))
    db.session.commit()
    socketio.emit("leaderboard_updated", {"leaderboard": get_top_leaderboard(50)})
    return delta_red, delta_blue

rooms_lock = threading.Lock()

user_rooms = {}  

room_states = {}  # mã (trạng thái: waiting, ready, playing và danh sách người chơi đã sẵn sàng)

RECONNECT_TIMEOUT = int(os.getenv("RECONNECT_TIMEOUT", "50"))


@socketio.on("join_room")
def handle_join_room(data):
    user_id = connected_users.get(request.sid)
    if user_id is None:
        emit("error", {"code": "AUTH_REQUIRED", "message": "Cần đăng nhập"})
        return

    room_code = str((data or {}).get("room_code") or "").strip()
    if user_rooms.get(request.sid) not in (None, room_code):
        emit("error", {"code": "ALREADY_IN_ROOM", "message": "Bạn đang ở phòng khác"})
        return
    room = Room.query.filter_by(room_code=room_code).first()
    if room is None:
        emit("error", {"code": "ROOM_NOT_FOUND", "message": "Phòng không tồn tại"})
        return

    with rooms_lock:
        players = active_rooms.setdefault(
            room_code, {"red": room.host_user_id, "blue": None}
        )
        if user_id == players["red"]:
            color = "red"
        elif players["blue"] in (None, user_id):
            players["blue"] = user_id
            color = "blue"
        else:
            color = None
        snapshot = dict(players)
        state = room_states.get(room_code)
        reconnected = bool(state) and state.get("waiting_reconnect", {}).pop(user_id, None) is not None

    if color is None:
        emit("error", {"code": "ROOM_FULL", "message": "Phòng đã đủ người"})
        return

    join_room(room_code)
    user_rooms[request.sid] = room_code
    emit("room_joined", {"room_code": room_code, "players": snapshot, "your_color": color})
    if reconnected:
        emit("player_reconnected", {"user_id": user_id}, to=room_code, include_self=False)
    else:
        emit(
            "player_joined",
            {"user_id": user_id, "color": color},
            to=room_code,
            include_self=False,
        )

def remove_from_room(sid, user_id, forfeit=False, room_code=None, reason="opponent_left"):
    if sid is not None:
        room_code = user_rooms.pop(sid, None)
    if room_code is None:
        return None

    with rooms_lock:
        players = active_rooms.get(room_code)
        if players is None:
            return room_code
        state = room_states.pop(room_code, None)
        was_playing = state is not None and state["status"] == "playing"
        snapshot = dict(players)
        if players["red"] == user_id:
            active_rooms.pop(room_code, None)
            closed = True
            others = [s for s, rc in user_rooms.items() if rc == room_code]
            for s in others:
                user_rooms.pop(s, None)
        else:
            if players["blue"] == user_id:
                players["blue"] = None
            closed = False
            others = []

    if was_playing and forfeit:
        result = "blue_win" if user_id == snapshot["red"] else "red_win"
        delta_red, delta_blue = record_match(
        room_code, snapshot["red"], snapshot["blue"], result
        )
        socketio.emit(
            "game_over",
            {
                "result": result,
                "winner_id": snapshot["blue"] if result == "blue_win" else snapshot["red"],
                "reason": reason,
                "elo_change": {"red": delta_red, "blue": delta_blue},
            },
            to=room_code,
        )

    if sid is not None:
        leave_room(room_code, sid=sid)
    socketio.emit(
        "player_left",
        {"user_id": user_id, "room_closed": closed},
        to=room_code,
    )
    for s in others:
        leave_room(room_code, sid=s)
    return room_code

def hold_seat(sid, user_id):
    room_code = user_rooms.get(sid)
    if room_code is None:
        return False

    with rooms_lock:
        state = room_states.get(room_code)
        if state is None or state["status"] != "playing":
            return False
        user_rooms.pop(sid, None)
        token = object()
        state.setdefault("waiting_reconnect", {})[user_id] = token

    socketio.emit(
        "opponent_disconnected",
        {"user_id": user_id, "timeout_seconds": RECONNECT_TIMEOUT},
        to=room_code,
    )
    socketio.start_background_task(forfeit_after_timeout, room_code, user_id, token)
    return True


def forfeit_after_timeout(room_code, user_id, token):
    socketio.sleep(RECONNECT_TIMEOUT)

    with rooms_lock:
        state = room_states.get(room_code)
        waiting = state.get("waiting_reconnect", {}) if state else {}
        if waiting.get(user_id) is not token:
            return
        waiting.pop(user_id, None)

    with app.app_context():
        remove_from_room(None, user_id, forfeit=True, room_code=room_code, reason="disconnect_timeout")


@socketio.on("leave_room")
def handle_leave_room(data=None):
    user_id = connected_users.get(request.sid)
    room_code = remove_from_room(request.sid, user_id, forfeit=True)
    emit("room_left", {"room_code": room_code})


@socketio.on("player_ready")
def handle_player_ready(data=None):
    user_id = connected_users.get(request.sid)
    room_code = user_rooms.get(request.sid)
    if user_id is None or room_code is None:
        emit("error", {"code": "NOT_IN_ROOM", "message": "Bạn chưa vào phòng"})
        return

    with rooms_lock:
        players = active_rooms.get(room_code)
        if players is None:
            return
        state = room_states.setdefault(
            room_code, {"status": "waiting", "ready": set()}
        )
        if state["status"] == "playing":
            return
        state["ready"].add(user_id)
        both = {players["red"], players["blue"]}
        started = None not in both and state["ready"] >= both
        if started:
            state["status"] = "playing"
        snapshot = dict(players)

    emit("player_ready", {"user_id": user_id}, to=room_code)
    if started:
        emit(
            "game_started",
            {"room_code": room_code, "players": snapshot, "first_turn": "red", "state": None},
            to=room_code,
        )


@socketio.on("request_leaderboard")
def handle_request_leaderboard():
    """Client yêu cầu dữ liệu bảng xếp hạng qua socket"""
    top = get_top_leaderboard(50)
    socketio.emit("leaderboard_updated", {"leaderboard": top}, to=request.sid)


if __name__ == "__main__":
    socketio.run(app, host="127.0.0.1", port=5000, debug=True)