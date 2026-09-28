import requests

URL = "http://127.0.0.1:5000/api/auth/register"


def test(ten, body):
    r = requests.post(URL, json=body)
    print(f"{ten:<22} -> HTTP {r.status_code} | {r.json()['message']}")


test("Dang ky moi", {"username": "nam", "password": "Nam@123", "email": "nam@gmail.com"})
test("Trung ten", {"username": "nam", "password": "Nam@123", "email": "khac@gmail.com"})
test("Trung email", {"username": "nam2", "password": "Nam@123", "email": "NAM@gmail.com"})
test("Thieu email", {"username": "an", "password": "An@1234"})
test("Ten co dau cach", {"username": "an an", "password": "An@1234", "email": "an@gmail.com"})
test("Mat khau yeu", {"username": "an", "password": "123456", "email": "an@gmail.com"})
test("Email sai", {"username": "an", "password": "An@1234", "email": "an.gmail.com"})