import bcrypt
from flask import Flask, jsonify, request

from models import db,User


app = Flask(__name__)

app.config["SQLALCHEMY_DATABASE_URI"] = "sqlite:///cotulenh.db" #Sau doi sang SQL

db.init_app(app)

with app.app_context():
    db.create_all()
USERS = {
    "dung" : "123456"
}

@app.get("/")
def home(): 
    return "Server CTL dang chay!"

@app.post("/api/auth/login")
def login(): 
    #Doc du lieu
    data = request.get_json(silent=True) or {}
    username = str(data.get("username") or "").strip()
    password = str(data.get("password" or ""))

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

   
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)