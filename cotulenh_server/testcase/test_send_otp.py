import requests

BASE = "http://127.0.0.1:5000/api/auth"
EMAIL = "duongtandung1208@gmail.com"  


def show(name, r):                         
    print(f"{name:<24} -> HTTP {r.status_code} | {r.json()}")


show("Đăng ký tài khoản test", requests.post(f"{BASE}/register", json={"username": "dungtest", "email": EMAIL, "password": "Abc@12345"}))

show("Email rỗng", requests.post(f"{BASE}/send-otp", json={}))                               # mong đợi: 400
show("Email không tồn tại", requests.post(f"{BASE}/send-otp", json={"email": "khongco@gmail.com"}))   # mong đợi: 200, KHÔNG có mail
show("Email đúng", requests.post(f"{BASE}/send-otp", json={"email": EMAIL}))               # mong đợi: 200, CÓ mail