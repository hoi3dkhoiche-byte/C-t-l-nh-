import getpass

import requests
import socketio

BASE_URL = "http://127.0.0.1:5000"


def try_connect(label, auth):
    sio = socketio.Client()
    try:
        sio.connect(BASE_URL, auth=auth, wait_timeout=5)
        print(f"{label}: KẾT NỐI ĐƯỢC")
        sio.disconnect()
    except socketio.exceptions.ConnectionError as e:
        print(f"{label}: BỊ TỪ CHỐI ({e})")


identifier = input("Username hoặc email: ")
password = getpass.getpass("Mật khẩu: ")

res = requests.post(
    f"{BASE_URL}/api/auth/login",
    json={"identifier": identifier, "password": password},
)
print("Login status:", res.status_code)
if res.status_code != 200:
    print("Login lỗi:", res.json())
    raise SystemExit(1)
token = res.json().get("access_token")

try_connect("1. Không token", None)
try_connect("2. Token đúng", {"token": token})
try_connect("3. Token sai", {"token": "abc.def.ghi"})