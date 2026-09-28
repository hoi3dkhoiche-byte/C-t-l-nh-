from flask import Flask, jsonify, request

app = Flask(__name__)

USERS = {
    "dung" : "123456"
}

@app.get("/")
def home(): 
    return "Server CTL dang chay!"

@app.post("/api/auth/login")
def login(): 
    data = request.get_json(silent=True) or {}
    username = str(data.get("username") or "").strip()
    password = str(data.get("password" or ""))

    if not username or not password: 
        return jsonify ({
            "success" : False,
            "message" : "Vui long nhap day du thong tin",
            "code" : 1001,
        }), 400
    if USERS.get(username) != password: 
        return jsonify ({
            "success" : False, 
            "message" : "Sai tai khoan hoac mat khau", 
            "code" : 1001,
        }), 401
    return jsonify ({
        "success" : True,
        "message" : f"Đăng nhập thành công! Chào {username}",
    }), 200

   
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)