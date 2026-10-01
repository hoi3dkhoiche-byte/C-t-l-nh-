import os
from pathlib import Path
import secrets
import string

from dotenv import load_dotenv
from flask import Flask, jsonify, request
from flask_migrate import Migrate
from flask_socketio import SocketIO
from sqlalchemy import text
from flask_jwt_extended import JWTManager, create_access_token, jwt_required, get_jwt_identity

from flask_bcrypt import Bcrypt
from models import db, User, Room

load_dotenv(Path(__file__).resolve().with_name(".env"))

app = Flask(__name__)
app.config["JWT_SECRET_KEY"] = os.getenv("JWT_SECRET_KEY")
jwt = JWTManager(app)
bcrypt=Bcrypt(app)
app.config["SQLALCHEMY_DATABASE_URI"] = os.getenv("DATABASE_URL")
app.config["SQLALCHEMY_TRACK_MODIFICATIONS"] = False

db.init_app(app)
migrate = Migrate(app, db)
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
            "coins": user.coins,
        }
    }), 200

if __name__ == "__main__":
    socketio.run(app, host="127.0.0.1", port=5000, debug=True)