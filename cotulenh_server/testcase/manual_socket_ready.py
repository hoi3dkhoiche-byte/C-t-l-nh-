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

res = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {t1}"})
room = res.json()["room"]["room_code"]
print("Chuẩn bị: user1 (red) và user2 (blue) vào phòng", room)
send(user1, "join_room", room)
send(user2, "join_room", room)

print("1. Chỉ red sẵn sàng")
send(user1, "player_ready", room)
print("2. blue sẵn sàng (ván bắt đầu)")
send(user2, "player_ready", room)
print("3. Người lạ vào phòng đang chơi")
send(user3, "join_room", room)
print("4. Người chưa vào phòng gửi sẵn sàng")
send(user3, "player_ready", room)
print("5. blue rời rồi vào lại, chỉ blue sẵn sàng")
send(user2, "leave_room", room)
send(user2, "join_room", room)
send(user2, "player_ready", room)
print("6. red sẵn sàng lại (ván mới bắt đầu)")
send(user1, "player_ready", room)

for client in (user1, user2, user3):
    client.disconnect()