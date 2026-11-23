import getpass
import time

import requests
import socketio

BASE_URL = "http://127.0.0.1:5000"


def login(identifier, password):
    res = requests.post(
        f"{BASE_URL}/api/auth/login",
        json={"identifier": identifier, "password": password},
    )
    if res.status_code != 200:
        print(f"Login {identifier} lỗi:", res.status_code, res.json())
        raise SystemExit(1)
    return res.json()["access_token"]


def stats(token):
    user = requests.get(
        f"{BASE_URL}/api/auth/me", headers={"Authorization": f"Bearer {token}"}
    ).json()["user"]
    return {k: user.get(k) for k in ("username", "elo_rating", "reputation", "banned_until")}


def make_client(name, token):
    sio = socketio.Client()

    @sio.on("*")
    def catch_all(event, data):
        if event in ("game_over", "error"):
            print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, data):
    client.emit(event, data)
    time.sleep(0.4)


def new_room(token):
    return requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {token}"})


password = getpass.getpass("Mật khẩu test: ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)
red = make_client("red", t1)
blue = make_client("blue", t2)
print("Trước:", stats(t1), stats(t2))

for i in range(1, 5):
    print(f"Lần {i}: xanh bỏ ván")
    room = new_room(t1).json()["room"]["room_code"]
    send(red, "join_room", {"room_code": room})
    send(blue, "join_room", {"room_code": room})
    send(red, "player_ready", {})
    send(blue, "player_ready", {})
    send(blue, "leave_room", {"room_code": room})
    send(red, "leave_room", {"room_code": room})
    print("  ", stats(t1), stats(t2))

print("B. Xanh đang bị cấm thử vào phòng và tạo phòng")
room = new_room(t1).json()["room"]["room_code"]
send(blue, "join_room", {"room_code": room})
res = new_room(t2)
print("  tạo phòng:", res.status_code, res.json())

red.disconnect()
blue.disconnect()