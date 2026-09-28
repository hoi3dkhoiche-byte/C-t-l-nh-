from flask import Flask, jsonify

app = Flask(__name__)


@app.get("/")
def home(): 
    return "Server CTL dang chay!"

@app.post("/api/auth/login")
def login(): 
    return jsonify({
        "success" : True,
        "message" : "Server da nhan duoc yeu cau dang nhap"
    })

if __name__ == "__main__": 
    app.run(host="0.0.0.0", port=5000, debug=True)