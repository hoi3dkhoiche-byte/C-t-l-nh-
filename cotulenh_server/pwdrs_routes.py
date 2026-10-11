import secrets                                  
from datetime import datetime, timedelta, timezone   

from flask import Blueprint, jsonify, request
from flask_bcrypt import generate_password_hash   

from models import db, User, PasswordReset
from mail_service import send_otp_email    

OTP_TTL_MINUTES = 5

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