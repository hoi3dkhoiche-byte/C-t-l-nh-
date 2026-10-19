import requests

BASE = "http://127.0.0.1:5000/api/auth"
EMAIL = "duongtandung1208@gmail.com"
NEW_PASSWORD = "Moi@12345"                      # mật khẩu mới để thử


def show(name, r):
    print(f"{name:<26} -> HTTP {r.status_code} | {r.json()}")


def reset(otp, password):                       # gọi API reset-password cho gọn
    return requests.post(f"{BASE}/reset-password", json={"email": EMAIL, "otp": otp, "newPassword": password})


show("Gửi OTP", requests.post(f"{BASE}/send-otp", json={"email": EMAIL}))
otp = input("Nhập mã OTP vừa nhận trong mail: ").strip()       # script dừng ở đây chờ bạn gõ mã
wrong = "000000" if otp != "000000" else "111111"               # một mã chắc chắn sai

show("OTP sai", reset(wrong, NEW_PASSWORD))                     # mong đợi: 400, còn 4 lần thử
show("OTP đúng, mật khẩu yếu", reset(otp, "abc12345"))          # mong đợi: 400, thiếu ký tự đặc biệt
show("OTP đúng, mật khẩu OK", reset(otp, NEW_PASSWORD))         # mong đợi: 200, đổi thành công
show("Dùng lại mã vừa dùng", reset(otp, "Khac@12345"))          # mong đợi: 400, mã đã dùng
show("Login mật khẩu mới", requests.post(f"{BASE}/login", json={"identifier": EMAIL, "password": NEW_PASSWORD}))   # mong đợi: 200 + access_token