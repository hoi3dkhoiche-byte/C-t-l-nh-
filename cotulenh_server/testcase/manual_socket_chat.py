import time

import requests
import socketio

BASE_URL = "http://127.0.0.1:5000"
PASSWORD = "matkhau123"


def account(username):
    # Đăng ký (đã có thì bỏ qua) rồi đăng nhập
    requests.post(f"{BASE_URL}/api/auth/register",
                  json={"username": username, "email": f"{username}@test.com", "password": PASSWORD})
    res = requests.post(f"{BASE_URL}/api/auth/login", json={"identifier": username, "password": PASSWORD})
    return res.json()["access_token"]


def make_client(name, token):
    sio = socketio.Client()

    # In mọi event nhận được, trừ các event không liên quan
    @sio.on("*")
    def catch_all(event, data):
        if event in ("leaderboard_updated", "game_started", "player_ready", "player_joined", "room_joined"):
            return
        print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, data=None):
    client.emit(event, data or {})
    time.sleep(0.5)


t1, t2, t3 = account("chattest1"), account("chattest2"), account("chattest3")
red, blue, outsider = make_client("red", t1), make_client("blue", t2), make_client("ngoài", t3)

# red và blue chung phòng, outsider ở ngoài
room = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {t1}"}).json()["room"]["room_code"]
send(red, "join_room", {"room_code": room})
send(blue, "join_room", {"room_code": room})
send(red, "player_ready")
send(blue, "player_ready")

print("1. Red gửi tin hợp lệ -> red và blue đều nhận, outsider không nhận")
send(red, "chat", {"message": "  xin chào  "})
print("2. Người ngoài phòng gửi -> bị từ chối NOT_IN_ROOM")
send(outsider, "chat", {"message": "chen ngang"})
print("3. Tin rỗng / sai kiểu -> EMPTY")
send(blue, "chat", {"message": "   "})
send(blue, "chat", {"message": 123})
print("4. Tin quá 200 ký tự -> TOO_LONG")
time.sleep(1)
send(blue, "chat", {"message": "a" * 201})
print("5. Gửi 2 tin liền nhau -> tin thứ hai bị TOO_FAST")
time.sleep(1)
blue.emit("chat", {"message": "tin 1"})
blue.emit("chat", {"message": "tin 2"})
time.sleep(1)

for c in (red, blue, outsider):
    c.disconnect()