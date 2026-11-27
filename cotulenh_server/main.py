import os
from pathlib import Path
import secrets
import ssl
import string
import threading
import time
from datetime import datetime, timedelta, timezone

import certifi
from dotenv import load_dotenv
from flask import Flask, jsonify, request
from flask_migrate import Migrate
from flask_socketio import SocketIO, join_room, leave_room, emit
from sqlalchemy import text
from sqlalchemy.engine import URL, make_url
from sqlalchemy.exc import ArgumentError, SQLAlchemyError
from flask_jwt_extended import JWTManager, create_access_token, jwt_required, get_jwt_identity, decode_token


from flask_bcrypt import Bcrypt
from models import db, User, Room, Match
from dataclasses import asdict
import game_engine
from pwdrs_routes import reset_bp 

BASE_DIR = Path(__file__).resolve().parent
load_dotenv(BASE_DIR / ".env")

DEV_ENVIRONMENTS = {"development", "dev", "local", "test", "testing"}
PROD_ENVIRONMENTS = {"production", "prod"}
DEV_FALLBACK_JWT_SECRET = "cotulenh-dev-jwt-secret-key-min-32-chars-replace-in-production"
PLACEHOLDER_JWT_SECRETS = {
    DEV_FALLBACK_JWT_SECRET,
    "doi-thanh-chuoi-ngau-nhien-it-nhat-32-ky-tu",
    "cotulenh-super-secret-key-production-32-chars",
}
PLACEHOLDER_DB_MARKERS = (
    "<RDS_ENDPOINT>",
    "<DB_USER>",
    "<DB_PASSWORD>",
    "<DB_NAME>",
    "rds-cotulenh-endpoint.amazonaws.com",
)
FORBIDDEN_MYSQL_QUERY_KEYS = {
    "read_default_file",
    "read_default_group",
    "client_flag",
}
DEFAULT_RDS_CA_CANDIDATES = (
    BASE_DIR / "certs" / "global-bundle.pem",
    Path("/etc/ssl/certs/aws-rds-global-bundle.pem"),
)


def resolve_app_env(env=os.environ) -> tuple[str, bool]:
    """Xác định môi trường chạy (local/development hay production)."""
    raw_env = (env.get("APP_ENV") or env.get("FLASK_ENV") or "development").strip().lower()
    if raw_env in PROD_ENVIRONMENTS:
        return raw_env, True
    if raw_env in DEV_ENVIRONMENTS:
        return raw_env, False
    raise RuntimeError(
        f"Giá trị APP_ENV='{raw_env}' không hợp lệ. "
        "Chỉ chấp nhận: development, local, test, production."
    )


def _env_int(env, key: str, default: int, min_value: int = 1) -> int:
    raw = (env.get(key) or "").strip()
    if not raw:
        return default
    try:
        value = int(raw)
    except ValueError as exc:
        raise RuntimeError(f"Biến môi trường {key}='{raw}' phải là số nguyên.") from exc
    if value < min_value:
        raise RuntimeError(f"Biến môi trường {key}={value} phải >= {min_value}.")
    return value


def resolve_jwt_secret(env=os.environ, *, is_production: bool = False) -> str:
    """Lấy JWT_SECRET_KEY; bắt buộc khóa thật tối thiểu 32 ký tự khi chạy production."""
    secret = (env.get("JWT_SECRET_KEY") or "").strip()
    if is_production:
        if not secret or secret in PLACEHOLDER_JWT_SECRETS or len(secret) < 32:
            raise RuntimeError(
                "APP_ENV=production bắt buộc phải cấu hình JWT_SECRET_KEY bảo mật "
                "(tối thiểu 32 ký tự và không dùng giá trị mẫu)."
            )
        return secret
    return secret or DEV_FALLBACK_JWT_SECRET


def build_mysql_ssl_context(env=os.environ, *, is_production: bool = False) -> ssl.SSLContext | None:
    """
    Khởi tạo SSLContext cho kết nối MySQL / AWS RDS theo DB_SSL_MODE:
    - DISABLED: Tắt TLS (mặc định cho MySQL local/Docker)
    - REQUIRED: Bật mã hóa TLS nhưng không xác minh CA/hostname
    - VERIFY_CA: Bật TLS và xác minh chứng chỉ CA (DB_SSL_CA)
    - VERIFY_IDENTITY: Bật TLS + xác minh chứng chỉ CA + kiểm tra khớp hostname RDS (mặc định ở production)
    """
    default_mode = "VERIFY_IDENTITY" if is_production else "DISABLED"
    mode = (env.get("DB_SSL_MODE") or default_mode).strip().upper()

    if mode in ("DISABLED", "OFF", "FALSE", "0"):
        return None

    if mode == "REQUIRED":
        ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
        ctx.minimum_version = ssl.TLSVersion.TLSv1_2
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE
        return ctx

    if mode not in ("VERIFY_CA", "VERIFY_IDENTITY"):
        raise RuntimeError(
            f"DB_SSL_MODE='{mode}' không hợp lệ. "
            "Chỉ hỗ trợ: DISABLED, REQUIRED, VERIFY_CA, VERIFY_IDENTITY."
        )

    raw_ca = (env.get("DB_SSL_CA") or "").strip()
    ca_file: Path | None = None
    if raw_ca:
        candidate = Path(raw_ca)
        if not candidate.is_absolute():
            candidate = candidate if candidate.exists() else (BASE_DIR / candidate)
        if not candidate.is_file():
            raise RuntimeError(
                f"Không tìm thấy file chứng chỉ CA tại DB_SSL_CA='{raw_ca}'. "
                "Tải bundle của AWS RDS tại: https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem"
            )
        ca_file = candidate
    else:
        for candidate in DEFAULT_RDS_CA_CANDIDATES:
            if candidate.is_file():
                ca_file = candidate
                break

    if ca_file is None:
        if is_production:
            raise RuntimeError(
                f"Chế độ DB_SSL_MODE={mode} trong production yêu cầu chứng chỉ CA cho RDS. "
                "Hãy đặt biến môi trường DB_SSL_CA trỏ tới file global-bundle.pem của AWS RDS."
            )
        ca_file = Path(certifi.where())

    ctx = ssl.create_default_context(cafile=str(ca_file))
    if hasattr(ssl, "VERIFY_X509_STRICT"):
        # Tránh lỗi strict X509 trên Python 3.13+ với chứng chỉ trung gian của MySQL/RDS
        ctx.verify_flags &= ~ssl.VERIFY_X509_STRICT
    ctx.minimum_version = ssl.TLSVersion.TLSv1_2
    ctx.verify_mode = ssl.CERT_REQUIRED
    ctx.check_hostname = mode == "VERIFY_IDENTITY"
    return ctx


def _validate_no_conflicting_url_params(parsed_url: URL) -> None:
    """
    Từ chối các tham số TLS/SSL hoặc cấu hình client nguy hiểm trên query string của DATABASE_URL
    (ví dụ ?ssl_disabled=true, ?ssl_verify_cert=0, ?read_default_file=...) để tránh ghi đè DB_SSL_MODE.
    """
    conflicting = []
    for raw_key in parsed_url.query.keys():
        norm_key = raw_key.strip().lower().replace("-", "_")
        if norm_key.startswith(("ssl", "tls")) or norm_key in FORBIDDEN_MYSQL_QUERY_KEYS:
            conflicting.append(raw_key)
    if conflicting:
        raise RuntimeError(
            f"DATABASE_URL chứa tham số cấu hình mâu thuẫn trên query string ({', '.join(conflicting)}). "
            "Vui lòng xóa các tham số này khỏi URL và cấu hình TLS thống nhất qua DB_SSL_MODE / DB_SSL_CA."
        )


def _resolve_parsed_database_url(env, *, is_production: bool) -> URL:
    """
    Xây dựng đối tượng SQLAlchemy URL an toàn:
    1. Nếu có DATABASE_URL: kiểm tra placeholder, parse URL và hỗ trợ bổ sung DB_PASSWORD qua URL.set().
    2. Nếu không có DATABASE_URL nhưng có DB_HOST: tạo bằng URL.create() để tự động mã hóa ký tự đặc biệt trong mật khẩu (@, :, /, ?, #, %).
    3. Nếu cả hai đều trống: cho phép fallback SQLite ở local/dev; báo lỗi ngay ở production.
    """
    raw_url = (env.get("DATABASE_URL") or "").strip()
    db_host = (env.get("DB_HOST") or "").strip()

    if raw_url:
        if any(marker in raw_url for marker in PLACEHOLDER_DB_MARKERS):
            raise RuntimeError(
                "DATABASE_URL đang chứa giá trị mẫu (placeholder). "
                "Vui lòng cấu hình thông tin kết nối thực tế."
            )
        try:
            parsed_url = make_url(raw_url)
        except ArgumentError as exc:
            raise RuntimeError(f"Định dạng DATABASE_URL không hợp lệ: {exc}") from exc

        extra_password = env.get("DB_PASSWORD")
        if parsed_url.password is None and extra_password:
            parsed_url = parsed_url.set(password=extra_password)
        return parsed_url

    if db_host:
        db_user = (env.get("DB_USER") or "").strip()
        db_password = env.get("DB_PASSWORD") or ""
        db_name = (env.get("DB_NAME") or "").strip()
        db_port = _env_int(env, "DB_PORT", 3306)

        for field_name, val in (
            ("DB_HOST", db_host),
            ("DB_USER", db_user),
            ("DB_PASSWORD", db_password),
            ("DB_NAME", db_name),
        ):
            if any(marker in val for marker in PLACEHOLDER_DB_MARKERS):
                raise RuntimeError(f"Biến môi trường {field_name} đang chứa giá trị mẫu (placeholder).")

        if not db_user or not db_name:
            raise RuntimeError("Khi cấu hình qua DB_HOST, bắt buộc phải khai báo đầy đủ DB_USER và DB_NAME.")
        if is_production and not db_password:
            raise RuntimeError("APP_ENV=production bắt buộc phải khai báo DB_PASSWORD khi dùng DB_HOST.")

        return URL.create(
            drivername="mysql+pymysql",
            username=db_user,
            password=db_password,
            host=db_host,
            port=db_port,
            database=db_name,
        )

    if is_production:
        raise RuntimeError(
            "APP_ENV=production bắt buộc phải cấu hình DATABASE_URL hoặc bộ biến "
            "(DB_HOST, DB_USER, DB_PASSWORD, DB_NAME) trỏ tới MySQL/AWS RDS; "
            "không cho phép tự động chạy SQLite."
        )
    return make_url("sqlite:///cotulenh.db")


def build_database_config(env=os.environ) -> tuple[str, dict]:
    """
    Xây dựng (SQLALCHEMY_DATABASE_URI, SQLALCHEMY_ENGINE_OPTIONS):
    - Local/dev: cho phép fallback sang SQLite nếu chưa khai báo DATABASE_URL / DB_HOST.
    - Production: bắt buộc có MySQL/RDS hợp lệ, cấm fallback âm thầm sang SQLite.
    - Chặn tham số TLS mâu thuẫn trên URL và đảm bảo DISABLED tắt hẳn TLS trên PyMySQL.
    """
    _, is_production = resolve_app_env(env)
    parsed_url = _resolve_parsed_database_url(env, is_production=is_production)
    backend = parsed_url.get_backend_name()

    if is_production and backend == "sqlite":
        raise RuntimeError(
            "APP_ENV=production không cho phép sử dụng SQLite. "
            "Hãy trỏ DATABASE_URL hoặc DB_HOST tới MySQL / AWS RDS."
        )

    if backend == "mysql":
        _validate_no_conflicting_url_params(parsed_url)

    database_url = parsed_url.render_as_string(hide_password=False)
    connect_timeout = _env_int(env, "DB_CONNECT_TIMEOUT", 10)

    if backend == "sqlite":
        engine_options: dict = {
            "pool_pre_ping": True,
            "connect_args": {"timeout": connect_timeout},
        }
        return database_url, engine_options

    engine_options = {
        "pool_pre_ping": True,
        "pool_recycle": _env_int(env, "DB_POOL_RECYCLE", 280),
        "pool_size": _env_int(env, "DB_POOL_SIZE", 10),
        "max_overflow": _env_int(env, "DB_MAX_OVERFLOW", 20, min_value=0),
        "pool_timeout": _env_int(env, "DB_POOL_TIMEOUT", 10),
    }

    if backend == "mysql":
        connect_args: dict = {
            "connect_timeout": connect_timeout,
            "read_timeout": _env_int(env, "DB_READ_TIMEOUT", 30),
            "write_timeout": _env_int(env, "DB_WRITE_TIMEOUT", 30),
            "charset": "utf8mb4",
        }
        ssl_ctx = build_mysql_ssl_context(env, is_production=is_production)
        if ssl_ctx is not None:
            connect_args["ssl_disabled"] = False
            connect_args["ssl"] = ssl_ctx
        else:
            # PyMySQL 1.2+ mặc định thử TLS (PREFERRED) nếu không truyền ssl_disabled=True
            connect_args["ssl_disabled"] = True
        engine_options["connect_args"] = connect_args

    return database_url, engine_options


APP_ENV, IS_PRODUCTION = resolve_app_env()

app = Flask(__name__)
app.config["JWT_SECRET_KEY"] = resolve_jwt_secret(is_production=IS_PRODUCTION)
jwt = JWTManager(app)
bcrypt = Bcrypt(app)

database_url, engine_options = build_database_config()
app.config["SQLALCHEMY_DATABASE_URI"] = database_url
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False
app.config["SQLALCHEMY_ENGINE_OPTIONS"] = engine_options

db.init_app(app)
migrate = Migrate(app, db)
app.register_blueprint(reset_bp)
socketio = SocketIO(app, async_mode="threading")


@app.get("/health")  # Trạng thái server
def health():
    return jsonify({"status": "ok", "env": APP_ENV})


@app.get("/db-health")  # Trạng thái database trên server
def db_health():
    try:
        db.session.execute(text("SELECT 1"))
        return jsonify({
            "status": "ok",
            "database": "connected",
            "backend": db.engine.url.get_backend_name(),
        }), 200
    except (SQLAlchemyError, OSError) as exc:
        db.session.rollback()
        app.logger.error("Database health check failed: %s", exc)
        return jsonify({
            "status": "error",
            "database": "disconnected",
            "error": "Không thể kết nối tới cơ sở dữ liệu",
        }), 503

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

    user = db.session.get(User, user_id)
    if user is None:
        return jsonify({"error": "User not found"}), 404
    if is_banned(user):
        return jsonify({
            "error": "Tài khoản đang bị cấm chơi do bỏ ván nhiều lần",
            "banned_until": user.banned_until.isoformat(),
        }), 403

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
                "host_reputation": db.session.get(User, room.host_user_id).reputation,
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
            "reputation": user.reputation,
            "banned_until": user.banned_until.isoformat() if user.banned_until else None,
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
    cancel_pending_join(request.sid)
    if not hold_seat(request.sid, user_id):
        remove_from_room(request.sid, user_id)
    print(f"[SocketIO] Ngắt kết nối: {request.sid} (user {user_id})")


active_rooms = {}  

MAX_REPUTATION = 10
ABANDON_PENALTIES = (2, 3, 4)
BAN_DAYS = 7
REPUTATION_AFTER_BAN = 6


def utc_now():
    return datetime.now(timezone.utc).replace(tzinfo=None)


def is_banned(user):
    return user.banned_until is not None and user.banned_until > utc_now()


def update_reputation(winner, loser, abandoned):
    winner.reputation = min(MAX_REPUTATION, winner.reputation + 1)
    winner.abandon_streak = 0
    if not abandoned:
        loser.reputation = min(MAX_REPUTATION, loser.reputation + 1)
        loser.abandon_streak = 0
        return

    penalty = ABANDON_PENALTIES[min(loser.abandon_streak, len(ABANDON_PENALTIES) - 1)]
    loser.abandon_streak += 1
    loser.reputation = max(0, loser.reputation - penalty)
    if loser.reputation == 0:
        loser.banned_until = utc_now() + timedelta(days=BAN_DAYS)
        loser.reputation = REPUTATION_AFTER_BAN
        loser.abandon_streak = 0

def record_match(room_code, red_id, blue_id, result, bet=0, abandoned=False):
    red = db.session.get(User, red_id)
    blue = db.session.get(User, blue_id)
    red_elo = red.elo_rating or 1200
    blue_elo = blue.elo_rating or 1200
    delta_red, delta_blue = calculate_elo(red_elo, blue_elo, result)
    if abandoned:
        # Bên còn lại chỉ nhận 1/2 Elo thắng, bên bỏ ván bị trừ đủ
        if result == "red_win":
            delta_red = round(delta_red / 2)
        else:
            delta_blue = round(delta_blue / 2)
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
        update_reputation(winner, loser, abandoned)
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
LOW_REPUTATION = 6
JOIN_REQUEST_TIMEOUT = int(os.getenv("JOIN_REQUEST_TIMEOUT", "20"))
JOIN_COOLDOWN = 60
pending_joins = {}   # room_code -> yêu cầu vào phòng đang chờ chủ phòng duyệt
join_cooldowns = {}  # (room_code, user_id) -> thời điểm được xin vào lại


@socketio.on("join_room")
def handle_join_room(data):
    user_id = connected_users.get(request.sid)
    if user_id is None:
        emit("error", {"code": "AUTH_REQUIRED", "message": "Cần đăng nhập"})
        return

    user = db.session.get(User, user_id)
    if is_banned(user):
        emit("error", {
            "code": "BANNED",
            "message": "Tài khoản đang bị cấm chơi do bỏ ván nhiều lần",
            "banned_until": user.banned_until.isoformat(),
        })
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
        elif players["blue"] == user_id:
            color = "blue"
        elif players["blue"] is not None or room_code in pending_joins:
            color = None
        elif user.reputation < LOW_REPUTATION:
            if join_cooldowns.get((room_code, user_id), 0) > time.monotonic():
                color = "cooldown"
            else:
                token = object()
                pending_joins[room_code] = {"user_id": user_id, "sid": request.sid, "token": token}
                color = "pending"
        else:
            players["blue"] = user_id
            color = "blue"
        snapshot = dict(players)
        state = room_states.get(room_code)
        reconnected = bool(state) and state.get("waiting_reconnect", {}).pop(user_id, None) is not None
        game = None
        if state and state.get("status") == "playing":
            game = {
                "turn": state["turn"],
                "move_count": len(state["moves"]),
                "board": [asdict(p) for p in state["pieces"]],
            }

    if color is None:
        emit("error", {"code": "ROOM_FULL", "message": "Phòng đã đủ người"})
        return

    if color == "cooldown":
        emit("error", {"code": "JOIN_COOLDOWN", "message": "Vừa bị từ chối, thử lại sau"})
        return
    if color == "pending":
        emit("join_pending", {"room_code": room_code, "timeout_seconds": JOIN_REQUEST_TIMEOUT})
        emit(
            "join_request",
            {
                "user_id": user_id,
                "username": user.username,
                "reputation": user.reputation,
                "timeout_seconds": JOIN_REQUEST_TIMEOUT,
            },
            to=room_code,
        )
        socketio.start_background_task(expire_join_request, room_code, token)
        return

    join_room(room_code)
    user_rooms[request.sid] = room_code
    emit("room_joined", {"room_code": room_code, "players": snapshot, "your_color": color, "game":game},)
    if reconnected:
        emit("player_reconnected", {"user_id": user_id}, to=room_code, include_self=False)
    else:
        emit(
            "player_joined",
            {"user_id": user_id, "color": color},
            to=room_code,
            include_self=False,
        )

def expire_join_request(room_code, token):
    socketio.sleep(JOIN_REQUEST_TIMEOUT)
    with rooms_lock:
        pending = pending_joins.get(room_code)
        if not pending or pending["token"] is not token:
            return
        pending_joins.pop(room_code)

    socketio.emit(
        "error",
        {"code": "JOIN_REJECTED", "message": "Chủ phòng không phản hồi"},
        to=pending["sid"],
    )
    socketio.emit("join_request_expired", {"user_id": pending["user_id"]}, to=room_code)


def cancel_pending_join(sid):
    with rooms_lock:
        cancelled = [(code, p) for code, p in pending_joins.items() if p["sid"] == sid]
        for code, _ in cancelled:
            pending_joins.pop(code)

    for code, pending in cancelled:
        socketio.emit("join_request_cancelled", {"user_id": pending["user_id"]}, to=code)


@socketio.on("answer_join")
def handle_answer_join(data=None):
    user_id = connected_users.get(request.sid)
    room_code = user_rooms.get(request.sid)
    accept = (data or {}).get("accept") is True

    pending = None
    with rooms_lock:
        players = active_rooms.get(room_code)
        if players and players["red"] == user_id and room_code in pending_joins:
            pending = pending_joins.pop(room_code)
            if accept:
                players["blue"] = pending["user_id"]
                snapshot = dict(players)
            else:
                join_cooldowns[(room_code, pending["user_id"])] = time.monotonic() + JOIN_COOLDOWN

    if pending is None:
        emit("error", {"code": "NO_JOIN_REQUEST", "message": "Không có yêu cầu nào đang chờ"})
        return

    if not accept:
        socketio.emit(
            "error",
            {"code": "JOIN_REJECTED", "message": "Chủ phòng đã từ chối"},
            to=pending["sid"],
        )
        return

    join_room(room_code, sid=pending["sid"])
    user_rooms[pending["sid"]] = room_code
    socketio.emit(
        "room_joined",
        {"room_code": room_code, "players": snapshot, "your_color": "blue", "game": None},
        to=pending["sid"],
    )
    emit("player_joined", {"user_id": pending["user_id"], "color": "blue"})

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
        room_code, snapshot["red"], snapshot["blue"], result, abandoned=True
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
            state["turn"] = "red"
            state["moves"] = []
            state["pieces"] = game_engine.initial_pieces()
            board = [asdict(p) for p in state["pieces"]]
            turn_token = state["turn_token"] = object()
        snapshot = dict(players)

    emit("player_ready", {"user_id": user_id}, to=room_code)
    if started:
        emit(
            "game_started",
            {
                "room_code": room_code,
                "players": snapshot,
                "first_turn": "red",
                "turn_seconds": TURN_SECONDS,
                "board": board,
            },
            to=room_code,
        )
        socketio.start_background_task(turn_timeout, room_code, turn_token)

TURN_SECONDS = int(os.getenv("TURN_SECONDS", "45"))


def turn_timeout(room_code, token):
    socketio.sleep(TURN_SECONDS)
    with rooms_lock:
        state = room_states.get(room_code)
        if not state or state.get("turn_token") is not token:
            return
        skipped = state["turn"]
        state["turn"] = game_engine.other_side(skipped)
        new_token = state["turn_token"] = object()
        next_turn = state["turn"]

    socketio.emit("turn_timeout", {"skipped": skipped, "next_turn": next_turn}, to=room_code)
    socketio.start_background_task(turn_timeout, room_code, new_token)

def finish_game(room_code, players, winner, reason):
    result = "red_win" if winner == "red" else "blue_win"
    delta_red, delta_blue = record_match(room_code, players["red"], players["blue"], result)
    socketio.emit(
        "game_over",
        {
            "result": result,
            "winner_id": players[winner],
            "reason": reason,
            "elo_change": {"red": delta_red, "blue": delta_blue},
        },
        to=room_code,
    )


@socketio.on("make_move")
def handle_make_move(data=None):
    user_id = connected_users.get(request.sid)
    room_code = user_rooms.get(request.sid)
    if user_id is None or room_code is None:
        emit("error", {"code": "NOT_IN_ROOM", "message": "Bạn chưa vào phòng"})
        return

    data = data or {}
    try:
        piece_id = str(data["piece_id"])
        from_xy = [int(data["from_x"]), int(data["from_y"])]
        to_x, to_y = int(data["to_x"]), int(data["to_y"])
        capture_mode = str(data.get("capture_mode", "occupy"))
        move_stack = data.get("move_stack", True) is True
    except (KeyError, TypeError, ValueError):
        emit("move_rejected", {"reason": "BAD_FORMAT"})
        return

    winner = reason = None
    with rooms_lock:
        players = active_rooms.get(room_code)
        state = room_states.get(room_code)
        error = None
        if not players or not state or state["status"] != "playing":
            error = "NOT_PLAYING"
        else:
            color = "red" if players["red"] == user_id else "blue"
            piece = game_engine.find_piece(state["pieces"], piece_id)
            if state["turn"] != color:
                error = "NOT_YOUR_TURN"
            elif piece and [piece.x, piece.y] != from_xy:
                error = "OUT_OF_SYNC"
            else:
                error, move = game_engine.apply_move(
                    state["pieces"], color, piece_id, to_x, to_y, capture_mode, move_stack
                )
        if not error:
            state["turn"] = game_engine.other_side(color)
            move["no"] = len(state["moves"]) + 1
            move["by"] = color
            state["moves"].append(move)
            winner, reason = game_engine.check_winner(state["pieces"], state["turn"])
            if winner:
                room_states.pop(room_code, None)
            else:
                turn_token = state["turn_token"] = object()
            snapshot = dict(players)
            next_turn = None if winner else state["turn"]

    if error:
        emit("move_rejected", {"reason": error})
        return

    emit("move_made", {"move": move, "next_turn": next_turn}, to=room_code)
    if winner:
        finish_game(room_code, snapshot, winner, reason)
    else:
        socketio.start_background_task(turn_timeout, room_code, turn_token)



@socketio.on("request_leaderboard")
def handle_request_leaderboard():
    """Client yêu cầu dữ liệu bảng xếp hạng qua socket"""
    top = get_top_leaderboard(50)
    socketio.emit("leaderboard_updated", {"leaderboard": top}, to=request.sid)


# =============================================================================
# CHAT TRONG PHÒNG / TRONG TRẬN
# =============================================================================

CHAT_MAX_LENGTH = 200     # số ký tự tối đa mỗi tin
CHAT_MIN_INTERVAL = 1.0   # số giây tối thiểu giữa 2 tin của một người
last_chat_at = {}         # user_id -> lần gửi gần nhất, để chống spam


@socketio.on("chat")
def handle_chat(data=None):
    # Người gửi và phòng lấy từ phiên socket, không tin dữ liệu client gửi lên
    user_id = connected_users.get(request.sid)
    room_code = user_rooms.get(request.sid)
    if user_id is None or room_code is None:
        emit("chat_rejected", {"reason": "NOT_IN_ROOM"})
        return

    message = (data or {}).get("message")
    if not isinstance(message, str) or not message.strip():
        emit("chat_rejected", {"reason": "EMPTY"})
        return
    message = message.strip()
    if len(message) > CHAT_MAX_LENGTH:
        emit("chat_rejected", {"reason": "TOO_LONG"})
        return

    now = time.monotonic()
    if now - last_chat_at.get(user_id, 0) < CHAT_MIN_INTERVAL:
        emit("chat_rejected", {"reason": "TOO_FAST"})
        return
    last_chat_at[user_id] = now

    user = db.session.get(User, user_id)
    # Phát cho cả phòng, kể cả người gửi
    emit(
        "chat_message",
        {
            "user_id": user_id,
            "username": user.username,
            "message": message,
            "sent_at": datetime.now(timezone.utc).isoformat(),
        },
        to=room_code,
    )


if __name__ == "__main__":
    host = os.getenv("HOST", "127.0.0.1")
    port = int(os.getenv("PORT", "5000"))
    debug = (not IS_PRODUCTION) and os.getenv("FLASK_DEBUG", "0") == "1"
    socketio.run(app, host=host, port=port, debug=debug, allow_unsafe_werkzeug=True)
