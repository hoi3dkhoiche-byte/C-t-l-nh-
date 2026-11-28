r"""Exercise two real network clients against an isolated local server.

Run with the server virtual environment:
    .venv\Scripts\python.exe -m unittest discover -s testcase -p test_lan_smoke.py -v

This checks the server protocol, not two physical machines or the WinForms UI.
"""

import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import threading
import time
import unittest

import requests
import socketio


SERVER_DIR = Path(__file__).resolve().parents[1]


class EventInbox:
    def __init__(self):
        self.events = {}
        self.condition = threading.Condition()

    def receive(self, event, data):
        with self.condition:
            self.events.setdefault(event, []).append(data)
            self.condition.notify_all()

    def wait(self, event, index=0, timeout=8):
        with self.condition:
            received = self.condition.wait_for(
                lambda: len(self.events.get(event, [])) > index, timeout=timeout
            )
            if not received:
                raise AssertionError(
                    f"Timed out waiting for {event}[{index}]; "
                    f"received events: {self.events}"
                )
            return self.events[event][index]


class LanServerFixture(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scratch = tempfile.TemporaryDirectory(prefix="cotulenh-lan-smoke-")
        cls.addClassCleanup(cls.cleanup_scratch)
        scratch = Path(cls.scratch.name)
        cls.server_log = (scratch / "server.log").open("w+", encoding="utf-8")
        cls.addClassCleanup(cls.server_log.close)
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", 0))
            port = probe.getsockname()[1]
        address = os.getenv("COTULENH_TEST_ADDRESS", "127.0.0.1")
        cls.base_url = f"http://{address}:{port}"
        env = os.environ.copy()
        env.update(
            DATABASE_URL=f"sqlite:///{(scratch / 'smoke.db').as_posix()}",
            JWT_SECRET_KEY="isolated-lan-smoke-test-secret-at-least-32-chars",
            HOST="127.0.0.1" if address == "127.0.0.1" else "0.0.0.0",
            PORT=str(port),
            TURN_SECONDS="10",
            RECONNECT_TIMEOUT="60",
            FLASK_DEBUG="0",
            PYTHONUNBUFFERED="1",
            PYTHONUTF8="1",
            PYTHONIOENCODING="utf-8",
        )
        flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        migrated = subprocess.run(
            [sys.executable, "-m", "flask", "--app", "main", "db", "upgrade"],
            cwd=SERVER_DIR,
            env=env,
            stdout=cls.server_log,
            stderr=subprocess.STDOUT,
            timeout=30,
            creationflags=flags,
        )
        if migrated.returncode:
            raise RuntimeError(f"Database migration failed:\n{cls.log_text()}")
        cls.server = subprocess.Popen(
            [sys.executable, "main.py"],
            cwd=SERVER_DIR,
            env=env,
            stdout=cls.server_log,
            stderr=subprocess.STDOUT,
            creationflags=flags,
        )
        cls.addClassCleanup(cls.stop_server)
        cls.http = requests.Session()
        cls.http.trust_env = False
        cls.addClassCleanup(cls.http.close)
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline and cls.server.poll() is None:
            try:
                if cls.http.get(f"{cls.base_url}/health", timeout=0.5).status_code == 200:
                    return
            except requests.RequestException:
                pass
            time.sleep(0.1)
        raise RuntimeError(f"Server did not start:\n{cls.log_text()}")

    @classmethod
    def log_text(cls):
        cls.server_log.flush()
        cls.server_log.seek(0)
        return cls.server_log.read()

    @classmethod
    def cleanup_scratch(cls):
        # Windows can retain inherited file handles briefly after process exit.
        deadline = time.monotonic() + 5
        while True:
            try:
                cls.scratch.cleanup()
                return
            except PermissionError:
                if time.monotonic() >= deadline:
                    raise
                time.sleep(0.1)

    @classmethod
    def stop_server(cls):
        if cls.server.poll() is None:
            if os.name == "nt":
                # Windows virtualenv Python can launch a child interpreter.
                subprocess.run(
                    ["taskkill", "/PID", str(cls.server.pid), "/T", "/F"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.DEVNULL,
                    timeout=5,
                    creationflags=subprocess.CREATE_NO_WINDOW,
                )
            else:
                cls.server.terminate()
            try:
                cls.server.wait(timeout=5)
            except subprocess.TimeoutExpired:
                cls.server.kill()
                cls.server.wait(timeout=5)

    def api(self, method, path, token=None, expected=200, **kwargs):
        headers = {"Authorization": f"Bearer {token}"} if token else {}
        response = self.http.request(
            method, f"{self.base_url}{path}", headers=headers, timeout=5, **kwargs
        )
        self.assertEqual(response.status_code, expected, response.text)
        return response.json()

    def register_and_login(self, name):
        credentials = {"username": name, "email": f"{name}@example.test", "password": "LanSmoke123!"}
        self.api("POST", "/api/auth/register", expected=201, json=credentials)
        return self.api(
            "POST", "/api/auth/login",
            json={"identifier": name, "password": credentials["password"]},
        )

    def client(self, token):
        inbox = EventInbox()
        session = requests.Session()
        session.trust_env = False
        self.addCleanup(session.close)
        client = socketio.Client(reconnection=False, http_session=session)
        self.addCleanup(client.disconnect)
        client.on("*", inbox.receive)
        # Polling is also used by Socket.IO when websocket-client is unavailable.
        try:
            client.connect(self.base_url, auth={"token": token}, transports=["polling"], wait_timeout=5)
        except socketio.exceptions.ConnectionError as error:
            raise RuntimeError(f"Socket.IO connection failed:\n{self.log_text()}") from error
        return client, inbox

    def running_match(self, prefix):
        red_login = self.register_and_login(f"{prefix}_red")
        blue_login = self.register_and_login(f"{prefix}_blue")
        red, red_events = self.client(red_login["access_token"])
        blue, blue_events = self.client(blue_login["access_token"])
        room = self.api(
            "POST", "/api/rooms", red_login["access_token"], expected=201
        )["room"]["room_code"]
        red.emit("join_room", {"room_code": room})
        red_events.wait("room_joined")
        blue.emit("join_room", {"room_code": room})
        blue_events.wait("room_joined")
        waiting = self.api("GET", "/api/rooms", red_login["access_token"])["rooms"]
        listed = next(entry for entry in waiting if entry["room_code"] == room)
        self.assertEqual(listed["host_username"], red_login["user"]["username"])
        self.assertEqual(listed["guest_user_id"], blue_login["user"]["id"])
        red.emit("player_ready", {})
        red_events.wait("player_ready")
        blue.emit("player_ready", {})
        started = red_events.wait("game_started")
        self.assertEqual(started, blue_events.wait("game_started"))
        return room, red_login, blue_login, red, blue, red_events, blue_events, started

class LanServerSmokeTest(LanServerFixture):
    def test_room_rejections_and_single_forfeit(self):
        room, red_login, blue_login, red, blue, red_events, blue_events, _ = self.running_match("forfeit")
        listed = self.api("GET", "/api/rooms", red_login["access_token"])["rooms"]
        self.assertNotIn(room, [entry["room_code"] for entry in listed])
        visitor_login = self.register_and_login("visitor")
        visitor, visitor_events = self.client(visitor_login["access_token"])
        visitor.emit("join_room", {"room_code": "MISSING0"})
        self.assertEqual(visitor_events.wait("error")["code"], "ROOM_NOT_FOUND")
        visitor.emit("join_room", {"room_code": room})
        self.assertEqual(visitor_events.wait("error", 1)["code"], "ROOM_FULL")
        visitor.emit("player_ready", {})
        self.assertEqual(visitor_events.wait("error", 2)["code"], "NOT_IN_ROOM")

        red.emit("leave_room", {})
        over = blue_events.wait("game_over")
        self.assertEqual(over, red_events.wait("game_over"))
        self.assertEqual(over["result"], "blue_win")
        self.assertEqual(over["reason"], "opponent_left")
        self.assertEqual(over["elo_change"], {"red": -16, "blue": 8})
        self.assertEqual(red_events.wait("room_left")["room_code"], room)
        red.emit("leave_room", {})
        self.assertIsNone(red_events.wait("room_left", 1)["room_code"])
        blue.emit("leave_room", {})
        blue_events.wait("room_left")
        listed = self.api("GET", "/api/rooms", red_login["access_token"])["rooms"]
        self.assertNotIn(room, [entry["room_code"] for entry in listed])
        visitor.emit("join_room", {"room_code": room})
        self.assertEqual(visitor_events.wait("error", 3)["code"], "ROOM_NOT_FOUND")
        for login, delta, reputation in ((red_login, -16, 8), (blue_login, 8, 10)):
            history = self.api("GET", "/api/matches/history", login["access_token"])
            self.assertEqual(len(history), 1)
            self.assertEqual(history[0]["elo_change"], delta)
            profile = self.api("GET", "/api/auth/me", login["access_token"])["user"]
            self.assertEqual(profile["elo_rating"], 1200 + delta)
            self.assertEqual(profile["reputation"], reputation)

    def test_turn_timeout_broadcasts_same_board_and_new_turn(self):
        _, _, _, red, blue, red_events, blue_events, started = self.running_match("timer")
        timeout = red_events.wait("turn_timeout", timeout=15)
        self.assertEqual(timeout, blue_events.wait("turn_timeout"))
        self.assertEqual(timeout["skipped"], "red")
        self.assertEqual(timeout["next_turn"], "blue")
        game = timeout["game"]
        self.assertEqual(game["board"], started["board"])
        self.assertEqual(game["turn"], "blue")
        self.assertEqual(game["move_count"], 0)
        self.assertEqual(game["turn_seconds"], 10)
        self.assertGreaterEqual(game["turn_remaining_seconds"], 9)
        self.assertLessEqual(game["turn_remaining_seconds"], 10)
        blue.emit("make_move", {
            "piece_id": "b_inf2", "from_x": 10, "from_y": 7, "to_x": 10, "to_y": 6,
        })
        move = red_events.wait("move_made")
        self.assertEqual(move, blue_events.wait("move_made"))
        self.assertEqual(move["next_turn"], "red")
        self.assertEqual(move["game"]["move_count"], 1)
        red.emit("leave_room", {})
        blue_events.wait("game_over")
        red_events.wait("room_left")

    def test_two_players_move_reconnect_and_finish(self):
        red_login = self.register_and_login("lan_smoke_red")
        blue_login = self.register_and_login("lan_smoke_blue")
        red_token, blue_token = red_login["access_token"], blue_login["access_token"]
        red, red_events = self.client(red_token)
        blue, blue_events = self.client(blue_token)
        room = self.api("POST", "/api/rooms", red_token, expected=201)["room"]["room_code"]
        red.emit("join_room", {"room_code": room})
        self.assertEqual(red_events.wait("room_joined")["your_color"], "red")
        blue.emit("join_room", {"room_code": room})
        self.assertEqual(blue_events.wait("room_joined")["your_color"], "blue")
        self.assertEqual(red_events.wait("player_joined")["user_id"], blue_login["user"]["id"])
        red.emit("chat", {"message": "Chao doi thu LAN!"})
        chat_msg = blue_events.wait("chat_message")
        self.assertEqual(chat_msg["message"], "Chao doi thu LAN!")
        self.assertEqual(red_events.wait("chat_message")["username"], red_login["user"]["username"])
        red.emit("player_ready", {})
        red_events.wait("player_ready")
        self.assertNotIn("game_started", red_events.events)
        blue.emit("player_ready", {})
        started = red_events.wait("game_started")
        self.assertEqual(started, blue_events.wait("game_started"))
        self.assertEqual(started["first_turn"], "red")
        self.assertEqual(len(started["board"]), 34)

        blue.emit("make_move", {"piece_id": "b_inf2", "from_x": 10, "from_y": 7, "to_x": 10, "to_y": 6})
        self.assertEqual(blue_events.wait("move_rejected")["reason"], "NOT_YOUR_TURN")

        def move(client, piece, source, target, count, blue_index):
            client.emit("make_move", {
                "piece_id": piece, "from_x": source[0], "from_y": source[1],
                "to_x": target[0], "to_y": target[1],
            })
            result = red_events.wait("move_made", count - 1)
            self.assertEqual(result, blue_events.wait("move_made", blue_index))
            self.assertEqual(result["move"]["no"], count)
            self.assertEqual(result["move"]["piece_id"], piece)
            self.assertEqual(result["move"]["to"], list(target))
            self.assertEqual(result["game"]["move_count"], count)
            piece_state = next(p for p in result["game"]["board"] if p["piece_id"] == piece)
            self.assertEqual((piece_state["x"], piece_state["y"]), target)
            return result

        move(red, "r_air1", (4, 1), (4, 5), 1, 0)
        blue.disconnect()
        self.assertEqual(red_events.wait("opponent_disconnected")["user_id"], blue_login["user"]["id"])
        blue, blue_events = self.client(blue_token)
        blue.emit("join_room", {"room_code": room})
        restored = blue_events.wait("room_joined")["game"]
        self.assertEqual(restored["turn"], "blue")
        self.assertEqual(restored["move_count"], 1)
        aircraft = next(p for p in restored["board"] if p["piece_id"] == "r_air1")
        self.assertEqual((aircraft["x"], aircraft["y"]), (4, 5))
        self.assertEqual(red_events.wait("player_reconnected")["user_id"], blue_login["user"]["id"])
        move(blue, "b_inf2", (10, 7), (10, 6), 2, 0)
        move(red, "r_air1", (4, 5), (4, 9), 3, 1)
        move(blue, "b_inf2", (10, 6), (10, 7), 4, 2)
        final_move = move(red, "r_air1", (4, 9), (6, 11), 5, 3)
        self.assertIn("b_cmd", final_move["move"]["captured"])
        game_over = red_events.wait("game_over")
        self.assertEqual(game_over, blue_events.wait("game_over"))
        self.assertEqual(game_over["result"], "red_win")
        self.assertEqual(game_over["reason"], "commander_captured")
        self.assertEqual(game_over["winner_id"], red_login["user"]["id"])

        # Leaving a finished match must not record the outcome twice.
        red.emit("leave_room", {})
        red_events.wait("room_left")
        for token, outcome, delta in ((red_token, "Thắng", 16), (blue_token, "Thua", -16)):
            history = self.api("GET", "/api/matches/history", token)
            self.assertEqual(len(history), 1, history)
            self.assertEqual(history[0]["room_code"], room)
            self.assertEqual(history[0]["result"], outcome)
            self.assertEqual(history[0]["elo_change"], delta)
            user = self.api("GET", "/api/auth/me", token)["user"]
            self.assertEqual(user["elo_rating"], 1200 + delta)


if __name__ == "__main__":
    unittest.main(verbosity=2)
