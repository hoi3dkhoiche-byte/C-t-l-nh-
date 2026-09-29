import bcrypt
import re
from flask import Blueprint, jsonify, request

from models import db,User

#Nhom API dang nhap / dang ky, moi route tu them "/api/auth" phia truoc
auth_bp = Blueprint("auth", __name__, url_prefix="/api/auth")

@auth_bp.post("/login")
def login(): 
    #Doc du lieu
    data = request.get_json(silent=True) or {}
    username = str(data.get("username") or "").strip()
    password = str(data.get("password")or "")

    if not username or not password: 
        return jsonify ({
            "success" : False,
            "message" : "Vui long nhap day du thong tin",
            "code" : 1001,
        }), 400

    #Find user in DB
    user = db.session.scalar(db.select(User).filter_by(username=username))

    #user khong ton tai / sai pass
    if user is None or not bcrypt.checkpw(
        password.encode("utf-8"),
        user.password_hash.encode("utf-8")
    ):
        return jsonify({
            "success" : False,
            "message" : "Sai tài khoản hoặc mật khẩu",
            "code" : 1001,
        }), 401
    #Login successful 
    return jsonify ({
        "success" : True,
        "message" : f"Đăng nhập thành công! Chào {user.username}"
    }), 200

@auth_bp.post("/register")
def register(): 
    data = request.get_json(silent=True) or {}
    username = str(data.get("username") or "").strip()
    password = str(data.get("password" )or "")
    email = str(data.get("email")or "").strip().lower()

    if not username or not password or not email: 
        return jsonify ({
            "success" : False,
            "message" : "Vui lòng nhập đầy đủ thông tin"
        }), 400
    if not re.fullmatch(r"[a-zA-Z0-9]+", username):
        return jsonify ({
            "success" : False,
            "message" : "Tên đăng nhập chỉ được chứa chữ cái và số"
        }),400
    if (
        len(password) < 6
        or not re.search(r"[a-zA-Z]", password)
        or not re.search(r"[0-9]", password)
        or not re.search(r"[^a-zA-Z0-9]", password)
    ):
        return jsonify ({
            "success" : False,
            "message" : "Mật khẩu phải từ 6 kí tự, gồm cả chữ, số và kí tự đặc biệt",
        }),400
    if not re.fullmatch(r"[^@\s]+@[^@\s]+\.[^@\s]+", email):
        return jsonify ({
            "success" : False,
            "message": "Định dạng email không hợp lệ",
        }),400
    user = db.session.scalar(db.select(User).filter_by(username=username))
    if user is not None: 
        return jsonify ({
            "success" : False,
            "message" : "Tên đăng nhập đã tồn tại",
        }),409
    mail = db.session.scalar(db.select(User).filter_by(email=email))
    if mail is not None: 
        return jsonify ({
            "success" : False,
            "message" : "Email đã tồn tại",
        }), 409
    password_hash = bcrypt.hashpw(
        password.encode("utf-8"),
        bcrypt.gensalt(),
    ).decode("utf-8")

    user = User(
        username = username,
        email = email,
        password_hash=password_hash,
    )
    db.session.add(user)
    db.session.commit()
    return jsonify ({
        "success" : True,
        "message" : "Đăng ký thành công! Vui lòng đăng nhập",
    }),201