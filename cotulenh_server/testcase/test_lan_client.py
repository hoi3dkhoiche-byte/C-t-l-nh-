"""Run two separate .NET clients using the real app services and board control.

Run after dotnet restore CoTuLenh.Client/LanSmoke/LanSmoke.csproj:
    python -m unittest discover -s cotulenh_server/testcase -p test_lan_client.py -v
"""

import json
import os
from pathlib import Path
import subprocess
import sys
import threading
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import test_lan_smoke as backend


ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "CoTuLenh.Client" / "LanSmoke" / "LanSmoke.csproj"
DRIVER = PROJECT.parent / "bin" / "Debug" / "net9.0-windows" / "LanSmoke.dll"


class DotnetPlayer:
    def __init__(self, base_url):
        env = os.environ.copy()
        env["COTULENH_SERVER_URL"] = base_url
        self.process = subprocess.Popen(
            ["dotnet", str(DRIVER)], cwd=ROOT, env=env,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            text=True, encoding="utf-8", creationflags=subprocess.CREATE_NO_WINDOW,
        )
        self.inbox = backend.EventInbox()
        self.stderr = []
        self.sequence = 0
        self.reader = threading.Thread(target=self.read_output, daemon=True)
        self.error_reader = threading.Thread(target=self.read_errors, daemon=True)
        self.reader.start()
        self.error_reader.start()

    def read_output(self):
        for line in self.process.stdout:
            try:
                message = json.loads(line)
            except json.JSONDecodeError:
                self.stderr.append(line)
                continue
            if "event" in message:
                self.inbox.receive(message["event"], message["data"])
            elif "id" in message:
                self.inbox.receive(f"reply:{message['id']}", message)

    def read_errors(self):
        self.stderr.extend(self.process.stderr)

    def command(self, name, **data):
        self.sequence += 1
        self.process.stdin.write(json.dumps({"id": self.sequence, "command": name, **data}) + "\n")
        self.process.stdin.flush()
        reply = self.inbox.wait(f"reply:{self.sequence}", timeout=15)
        if not reply["ok"]:
            raise AssertionError(f"C# {name} failed: {reply}; events={self.inbox.events}; stderr={self.stderr}")
        return reply.get("data")

    def event(self, name, index=0):
        return self.inbox.wait(name, index, timeout=15)

    def close(self):
        if self.process.poll() is None:
            try:
                self.command("shutdown")
            except (AssertionError, BrokenPipeError, OSError):
                pass
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait(timeout=5)
        self.reader.join(timeout=2)
        self.error_reader.join(timeout=2)
        for stream in (self.process.stdin, self.process.stdout, self.process.stderr):
            stream.close()


@unittest.skipUnless(os.name == "nt", "The WinForms client requires Windows")
class LanClientSmokeTest(backend.LanServerFixture):
    @classmethod
    def setUpClass(cls):
        built = subprocess.run(
            ["dotnet", "build", str(PROJECT), "--no-restore"], cwd=ROOT,
            capture_output=True, text=True, timeout=60,
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        if built.returncode:
            raise RuntimeError(f"Build failed; restore the LanSmoke project first:\n{built.stdout}\n{built.stderr}")
        super().setUpClass()

    def player(self, username):
        player = DotnetPlayer(self.base_url)
        self.addCleanup(player.close)
        player.command("register", username=username, email=f"{username}@example.test", password="LanSmoke123!")
        user = player.command("login", username=username, password="LanSmoke123!")
        player.command("connect")
        player.event("connected")
        return player, user

    def test_actual_csharp_players_and_authoritative_board(self):
        red, red_user = self.player("dotnet_red")
        blue, blue_user = self.player("dotnet_blue")
        self.assertNotEqual(red.process.pid, blue.process.pid)
        room = red.command("create_room")["room_code"]
        red.command("join", room_code=room)
        self.assertEqual(red.event("room_joined")["your_color"], "red")
        blue.command("join", room_code=room)
        self.assertEqual(blue.event("room_joined")["your_color"], "blue")
        red.command("ready")
        blue.command("ready")
        started = red.event("game_started")
        self.assertEqual(started, blue.event("game_started"))
        self.assertEqual(len(started["board"]), 34)

        blue.command("move", move={"piece_id": "b_inf2", "from_x": 10, "from_y": 7, "to_x": 10, "to_y": 6})
        self.assertEqual(blue.event("move_rejected")["reason"], "NOT_YOUR_TURN")
        moves = [
            (red, "r_air1", (4, 1), (4, 5)),
            (blue, "b_inf2", (10, 7), (10, 6)),
            (red, "r_air1", (4, 5), (4, 9)),
            (blue, "b_inf2", (10, 6), (10, 7)),
            (red, "r_air1", (4, 9), (6, 11)),
        ]
        for index, (player, piece, source, target) in enumerate(moves):
            player.command("move", move={
                "piece_id": piece, "from_x": source[0], "from_y": source[1],
                "to_x": target[0], "to_y": target[1],
            })
            accepted = red.event("move_made", index)
            self.assertEqual(accepted, blue.event("move_made", index))
            self.assertEqual(accepted["move"]["no"], index + 1)
            self.assertEqual(accepted["game"]["move_count"], index + 1)
            if index == 0:
                checked = red.command("board_assert", board=started["board"], accepted_move=accepted["move"])
                self.assertTrue(checked["authoritative_board"])
                self.assertTrue(checked["ownership_and_turn_guards"])

        over = red.event("game_over")
        self.assertEqual(over, blue.event("game_over"))
        self.assertEqual(over["result"], "red_win")
        self.assertEqual(over["winner_id"], red_user["Id"])
        self.assertNotEqual(red_user["Id"], blue_user["Id"])
        red.command("leave")
        red.event("room_left")
        blue.command("leave")
        blue.event("room_left")
        self.assertIsNone(red.command("state")["room_code"])
        self.assertIsNone(blue.command("state")["room_code"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
