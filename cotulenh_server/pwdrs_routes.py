import re
import secrets                                  
from datetime import datetime, timedelta, timezone   

from flask import Blueprint, jsonify, request
from flask_bcrypt import generate_password_hash   

from models import db, User, PasswordReset
from mail_service import send_otp_email    
from flask_bcrypt import generate_password_hash, check_password_hash

OTP_TTL_MINUTES = 5
MAX_OTP_ATTEMPTS = 5

reset_bp = Blueprint("password_reset", __name__, url_prefix="/api/auth")


def utc_now(): 
    return datetime.now(timezone.utc).replace(tzinfo=None)

@reset_bp.post("/send-otp")
def send_otp(): 
    data =request.get_json(silent=True) or {}
    email = str(data.get("email") or "").strip().lower()

    if not email: 
        return jsonify({"success": False,"message": "Vui lòng nhập email"}), 400

    generic = {
        "success" : True,
        "message": "Nếu Email đã đăng kí, mã OTP đã được gửi"
    }

    user = db.session.scalar(db.select(User).filter_by(email=email))
    if user is None: 
        return jsonify(generic),200

    #Vô hiệu hoá mã cũ chưa dùng (gửi lại OTP -> mã cũ hết giá trị)
    db.session.execute(
        db.update(PasswordReset)
        .where(PasswordReset.user_id == user.id, PasswordReset.used.is_(False))
        .values(used=True)
    )

    otp = f"{secrets.randbelow(1_000_000):06d}"

    reset = PasswordReset(
        user_id=user.id,
        otp_hash=generate_password_hash(otp).decode("utf-8"),
        expires_at=utc_now() + timedelta(minutes=OTP_TTL_MINUTES),
    )
    db.session.add(reset)

    try: 
        send_otp_email(email,otp)
    except Exception as e: 
        db.session.rollback()
        print("Lỗi gửi mail: ",e)
        return jsonify({"success" : False,"message" : "Không gửi được email, vui lòng thử lại sau"}), 500

    db.session.commit()
    return jsonify(generic), 200

def password_error(password): 
    if len(password) < 8: 
        return "Mật khẩu phải từ 8 ký tự trở lên"
    if not re.search (r"[a-zA-z]", password): 
        return "Mật khẩu phải có ít nhất 1 chữ cái"
    if not re.search(r"[0-9]",password): 
        return "Mật khẩu phải có ít nhất một chữ số"
    if not re.search(r"[^a-zA-Z0-9]", password): 
        return "Mật khẩu phải có ít nhất 1 kí tự đặc biệt"
    return None

@reset_bp.post("/reset-password")
def reset_password(): 
    data = request.get_json(silent=True) or {}
    email = str(data.get("email") or "").strip().lower()
    otp = str(data.get("otp") or "").strip()
    new_password = str(data.get("newPassword") or "")

    if not email or not otp or not new_password:
        return jsonify({"success" : False, "message" : "Vui lòng nhập đầy đủ email, mã OTP và mật khẩu mới"}), 400

    invalid = {"success" : False, "message" : "Mã OTP không đúng hoặc đã hết hạn"}

    user = db.session.scalar(db.select(User).filter_by(email=email))
    if user is None: 
        return jsonify(invalid),400

    #Lay ma OTP moi nhat chua su dung cua user

    reset = db.session.scalar(
        db.select(PasswordReset)
        .filter_by(user_id=user.id, used=False)
        .order_by(PasswordReset.id.desc())
    )

    if reset is None or reset.expires_at < utc_now(): 
        return jsonify(invalid),400

    if not check_password_hash(reset.otp_hash,otp): 
        reset.attempts += 1
        if reset.attempts >= MAX_OTP_ATTEMPTS: 
            reset.used = True
            db.session.commit()
            return jsonify({"success" : False, "message" : "Nhập sai quá nhiều lần, vui lòng gửi lại mã OTP"}), 400
        db.session.commit()
        remaining = MAX_OTP_ATTEMPTS - reset.attempts 
        return jsonify({"success" : False, "message" : f"Mã OTP không đúng (còn {remaining} lần thử)"}), 400

    error = password_error(new_password)
    if error: 
        return jsonify({"success" : False, "message" : error}), 400

    user.password_hash = generate_password_hash(new_password).decode("utf-8")
    reset.used = True
    db.session.commit()

    return jsonify({"success" : True, "message" : "Đổi mật khẩu thành công! Vui lòng đăng nhập lại"}), 200
