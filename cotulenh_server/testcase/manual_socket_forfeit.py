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
    return {k: user.get(k) for k in ("username", "elo_rating", "wins", "losses", "coins")}


def make_client(name, token):
    sio = socketio.Client()

    @sio.on("*")
    def catch_all(event, data):
        if event != "leaderboard_updated":
            print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, room_code):
    client.emit(event, {"room_code": room_code})
    time.sleep(0.5)


def new_room(token):
    res = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {token}"})
    return res.json()["room"]["room_code"]


def start_game(c1, c2, room):
    send(c1, "join_room", room)
    send(c2, "join_room", room)
    send(c1, "player_ready", room)
    send(c2, "player_ready", room)


password = getpass.getpass("Mật khẩu test: ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)

print("Trước:", stats(t1), stats(t2))

user1 = make_client("user1", t1)
user2 = make_client("user2", t2)

print("A. Rời phòng khi CHƯA bắt đầu (không ai thua)")
room = new_room(t1)
send(user1, "join_room", room)
send(user2, "join_room", room)
send(user2, "leave_room", room)
send(user1, "leave_room", room)
print("Sau A:", stats(t1), stats(t2))

print("B. Đang chơi, blue rời (red thắng)")
room = new_room(t1)
start_game(user1, user2, room)
send(user2, "leave_room", room)
send(user1, "leave_room", room)
print("Sau B:", stats(t1), stats(t2))

print("C. Đang chơi, red (chủ phòng) rời (blue thắng)")
room = new_room(t1)
start_game(user1, user2, room)
send(user1, "leave_room", room)
print("Sau C:", stats(t1), stats(t2))

print("D. Đang chơi, blue mất kết nối (chỉ hủy ván, không ai thua)")
room = new_room(t1)
send(user2, "leave_room", room)
start_game(user1, user2, room)
user2.disconnect()
time.sleep(0.5)
print("Sau D:", stats(t1), stats(t2))

user1.disconnect()