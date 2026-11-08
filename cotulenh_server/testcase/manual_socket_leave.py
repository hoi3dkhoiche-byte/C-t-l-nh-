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


def create_room(token):
    res = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {token}"})
    return res.json()["room"]["room_code"]


def make_client(name, token):
    sio = socketio.Client()

    @sio.on("*")
    def catch_all(event, data):
        print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, room_code):
    client.emit(event, {"room_code": room_code})
    time.sleep(0.5)


password = getpass.getpass("Mật khẩu test (dùng chung 3 tài khoản): ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)
t3 = login("sockettest3", password)

user1 = make_client("user1", t1)
user2 = make_client("user2", t2)
user3 = make_client("user3", t3)

r1 = create_room(t1)
print("Chuẩn bị: user1 (red) và user2 (blue) vào phòng", r1)
send(user1, "join_room", r1)
send(user2, "join_room", r1)

print("A1. blue rời phòng")
send(user2, "leave_room", r1)
print("A2. người mới vào chỗ blue")
send(user3, "join_room", r1)

print("B1. chủ phòng rời")
send(user1, "leave_room", r1)
r2 = create_room(t3)
print("B2. user3 sang phòng mới", r2)
send(user3, "join_room", r2)

print("C. user2 vào phòng mới rồi mất kết nối")
send(user2, "join_room", r2)
user2.disconnect()
time.sleep(0.5)

user1.disconnect()
user3.disconnect()