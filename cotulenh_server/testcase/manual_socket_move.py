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
        if event == "leaderboard_updated":
            return
        if event == "game_started":
            data = {**data, "board": f"<{len(data['board'])} quân>"}
        if event == "move_made":
            m = data["move"]
            data = f"#{m['no']} {m['by']} {m['piece_id']} {m['from']}->{m['to']} ăn={m['captured']} lượt_sau={data['next_turn']}"
        print(f"  [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, data):
    client.emit(event, data)
    time.sleep(0.4)


def move(client, piece_id, fx, fy, tx, ty, **extra):
    send(client, "make_move", {
        "piece_id": piece_id, "from_x": fx, "from_y": fy, "to_x": tx, "to_y": ty, **extra,
    })


password = getpass.getpass("Mật khẩu test: ")
t1 = login("sockettest1", password)
t2 = login("sockettest2", password)
red = make_client("red", t1)
blue = make_client("blue", t2)

room = requests.post(f"{BASE_URL}/api/rooms", headers={"Authorization": f"Bearer {t1}"}).json()["room"]["room_code"]
send(red, "join_room", {"room_code": room})
send(blue, "join_room", {"room_code": room})
send(red, "player_ready", {})
send(blue, "player_ready", {})

print("1. Xanh đi trước lượt")
move(blue, "b_inf2", 10, 7, 10, 6)
print("2. Đỏ đi quân xanh")
move(red, "b_inf2", 10, 7, 10, 6)
print("3. Đỏ đi sai luật (bộ binh nhảy 3 ô)")
move(red, "r_inf2", 10, 4, 10, 7)
print("4. Đỏ gửi sai vị trí xuất phát")
move(red, "r_air1", 3, 3, 4, 5)
print("5. Thiếu trường dữ liệu")
send(red, "make_move", {"piece_id": "r_air1"})
print("6. Ván hợp lệ: máy bay đỏ bay sang ăn Tư lệnh xanh")
move(red, "r_air1", 4, 1, 4, 5)
move(blue, "b_inf2", 10, 7, 10, 6)
move(red, "r_air1", 4, 5, 4, 9)
move(blue, "b_inf2", 10, 6, 10, 7)
move(red, "r_air1", 4, 9, 6, 11)
time.sleep(0.5)

red.disconnect()
blue.disconnect()