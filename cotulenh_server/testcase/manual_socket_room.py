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


def make_client(name, token=None):
    sio = socketio.Client()

    @sio.on("*")
    def catch_all(event, data):
        print(f"  [{name}] nhận {event}: {data}")

    auth = {"token": token} if token else None
    sio.connect(BASE_URL, auth=auth, wait_timeout=5)
    return sio


def join(client, room_code):
    client.emit("join_room", {"room_code": room_code})
    time.sleep(0.5)


password = getpass.getpass("Mật khẩu test (dùng chung 3 tài khoản): ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)
t3 = login("sockettest3", password)

res = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {t1}"})
print("Tạo phòng:", res.status_code)
room_code = res.json()["room"]["room_code"]
print("room_code:", room_code)

user1 = make_client("user1", t1)
user2 = make_client("user2", t2)
user3 = make_client("user3", t3)
guest = make_client("khach")

print("1. Chủ phòng vào")
join(user1, room_code)
print("2. Người thứ hai vào")
join(user2, room_code)
print("3. Người thứ ba vào (phòng đầy)")
join(user3, room_code)
print("4. Khách vào")
join(guest, room_code)
print("5. Phòng không tồn tại")
join(user3, "XXXXXXXX")

for client in (user1, user2, user3, guest):
    client.disconnect()