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
    return {k: user.get(k) for k in ("username", "wins", "losses")}


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


def start_game(t1, c1, c2):
    res = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {t1}"})
    room = res.json()["room"]["room_code"]
    send(c1, "join_room", room)
    send(c2, "join_room", room)
    send(c1, "player_ready", room)
    send(c2, "player_ready", room)
    return room


password = getpass.getpass("Mật khẩu test: ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)
print("Trước:", stats(t1), stats(t2))

user1 = make_client("user1", t1)

print("A. blue mất kết nối, quay lại sau 2 giây (ván tiếp tục)")
user2 = make_client("user2", t2)
room = start_game(t1, user1, user2)
user2.disconnect()
time.sleep(2)
user2 = make_client("user2-moi", t2)
send(user2, "join_room", room)
print("  ...chờ 6 giây, KHÔNG được có game_over")
time.sleep(6)
print("Sau A:", stats(t1), stats(t2))
send(user2, "leave_room", room)
send(user1, "leave_room", room)

print("B. blue mất kết nối, không quay lại (red thắng sau 5 giây)")
room = start_game(t1, user1, user2)
user2.disconnect()
print("  ...chờ 7 giây")
time.sleep(7)
print("Sau B:", stats(t1), stats(t2))
send(user1, "leave_room", room)

print("C. Mất kết nối, quay lại, rồi mất lần nữa (đồng hồ cũ không được xử thua sớm)")
user2 = make_client("user2", t2)
room = start_game(t1, user1, user2)
user2.disconnect()
time.sleep(1)
user2 = make_client("user2-moi", t2)
send(user2, "join_room", room)
time.sleep(2)
user2.disconnect()
print("  ...giây thứ 5 kể từ lần đầu: đồng hồ cũ hết hạn, KHÔNG được có game_over")
time.sleep(2.5)
print("  ...chờ thêm, đồng hồ mới hết hạn thì mới có game_over")
time.sleep(4)
print("Sau C:", stats(t1), stats(t2))

user1.disconnect()