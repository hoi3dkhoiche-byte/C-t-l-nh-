import requests

BASE_URL = "http://127.0.0.1:5000"

def test_leaderboard():
    r = requests.get(f"{BASE_URL}/api/leaderboard")
    print(f"Leaderboard status: {r.status_code}")
    print("Leaderboard items:", len(r.json()) if r.status_code == 200 else r.text)

if __name__ == "__main__":
    test_leaderboard()

