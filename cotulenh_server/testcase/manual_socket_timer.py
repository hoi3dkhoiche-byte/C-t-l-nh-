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
        if event in ("leaderboard_updated", "player_ready", "room_joined", "player_joined"):
            return
        if event == "game_started":
            data = {"turn_seconds": data["turn_seconds"], "first_turn": data["first_turn"]}
        if event == "move_made":
            m = data["move"]
            data = f"#{m['no']} {m['by']} {m['piece_id']} lượt_sau={data['next_turn']}"
        print(f"  [{time.strftime('%H:%M:%S')}] [{name}] nhận {event}: {data}")

    sio.connect(BASE_URL, auth={"token": token}, wait_timeout=5)
    return sio


def send(client, event, data):
    client.emit(event, data)
    time.sleep(0.4)


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

print("1. Đỏ không đi, chờ hết giờ: phải bị bỏ lượt, sang lượt xanh")
time.sleep(5.5)
print("2. Đỏ đi lúc không còn lượt: phải bị từ chối")
send(red, "make_move", {"piece_id": "r_air1", "from_x": 4, "from_y": 1, "to_x": 4, "to_y": 5})
print("3. Xanh đi kịp giờ: đồng hồ đếm lại cho đỏ")
send(blue, "make_move", {"piece_id": "b_inf2", "from_x": 10, "from_y": 7, "to_x": 10, "to_y": 6})
print("4. Đỏ đi sau 3 giây (vẫn kịp vì đồng hồ đã đếm lại từ đầu)")
time.sleep(3)
send(red, "make_move", {"piece_id": "r_air1", "from_x": 4, "from_y": 1, "to_x": 4, "to_y": 5})
time.sleep(1)

red.disconnect()
blue.disconnect()