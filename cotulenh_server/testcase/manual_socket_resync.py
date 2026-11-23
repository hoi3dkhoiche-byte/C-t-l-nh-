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
        if event in ("leaderboard_updated", "player_ready", "game_started"):
            return
        if event == "move_made":
            m = data["move"]
            data = f"#{m['no']} {m['by']} {m['piece_id']} {m['from']}->{m['to']}"
        if event == "room_joined" and data.get("game"):
            game = data["game"]
            air = next(p for p in game["board"] if p["piece_id"] == "r_air1")
            data = {**data, "game": {
                "turn": game["turn"],
                "move_count": game["move_count"],
                "board": f"<{len(game['board'])} quân>",
                "r_air1": (air["x"], air["y"]),
            }}
        print(f"  [{name}] nhận {event}: {data}")

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

print("1. Đỏ đi máy bay (4,1) -> (4,5), rồi xanh mất kết nối")
send(red, "make_move", {"piece_id": "r_air1", "from_x": 4, "from_y": 1, "to_x": 4, "to_y": 5})
blue.disconnect()
time.sleep(1)

print("2. Xanh kết nối lại, phải nhận lại bàn cờ: lượt xanh, 1 nước, máy bay ở (4,5)")
blue = make_client("blue-moi", t2)
send(blue, "join_room", {"room_code": room})

print("3. Xanh đi tiếp được")
send(blue, "make_move", {"piece_id": "b_inf2", "from_x": 10, "from_y": 7, "to_x": 10, "to_y": 6})

send(red, "leave_room", {"room_code": room})
red.disconnect()
blue.disconnect()