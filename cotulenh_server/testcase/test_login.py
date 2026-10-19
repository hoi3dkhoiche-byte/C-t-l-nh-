import requests

URL = "http://127.0.0.1:5000/api/auth/login"

def thu(ten,body): 
    r = requests.post(URL, json=body)
    print(f"{ten:<20} -> HTTP {r.status_code} | {r.json()}")


# ← SỬA: server hiện tại (main.py) nhận "identifier" = username HOẶC email, không còn "username"
# ← SỬA: dùng tài khoản dungtest với mật khẩu mới vừa đổi bằng reset-password
thu("Dung (username)", {"identifier": "dungtest", "password": "Moi@12345"})
thu("Dung (email)", {"identifier": "duongtandung1208@gmail.com", "password": "Moi@12345"})
thu("Sai mat khau", {"identifier": "dungtest", "password": "sai"})
thu("Sai tai khoan", {"identifier": "nam", "password": "Moi@12345"})
thu("Thieu mat khau", {"identifier": "dungtest"})
thu("Body rong", {})