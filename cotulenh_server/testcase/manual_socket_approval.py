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


def make_client(name, token):
    sio = socketio.Client()

    @sio.on("*")
    def catch_all(event, data):
        if event in ("leaderboard_updated", "player_left", "room_left"):
            return
        print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, data):
    client.emit(event, data)
    time.sleep(0.4)


def open_room(host, token):
    room = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {token}"}).json()["room"]["room_code"]
    send(host, "join_room", {"room_code": room})
    return room


password = getpass.getpass("Mật khẩu test: ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)   # uy tín thấp (dưới 6)
t3 = login("sockettest3", password)   # uy tín bình thường
host = make_client("chu-phong", t1)
low = make_client("uy-tin-thap", t2)
normal = make_client("binh-thuong", t3)

print("A. Uy tín thấp xin vào, người khác chen vào lúc đang chờ, chủ phòng đồng ý")
room = open_room(host, t1)
send(low, "join_room", {"room_code": room})
send(normal, "join_room", {"room_code": room})
send(host, "answer_join", {"accept": True})
send(low, "leave_room", {"room_code": room})
send(host, "leave_room", {"room_code": room})

print("B. Chủ phòng từ chối, xin lại ngay thì bị chặn")
room = open_room(host, t1)
send(low, "join_room", {"room_code": room})
send(host, "answer_join", {"accept": False})
send(low, "join_room", {"room_code": room})
send(host, "leave_room", {"room_code": room})

print("C. Chủ phòng không trả lời, hết giờ tự từ chối")
room = open_room(host, t1)
send(low, "join_room", {"room_code": room})
time.sleep(3.5)
send(host, "leave_room", {"room_code": room})

print("D. Người uy tín bình thường vào thẳng, không cần duyệt")
room = open_room(host, t1)
send(normal, "join_room", {"room_code": room})

for c in (host, low, normal):
    c.disconnect()