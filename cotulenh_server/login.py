import requests

URL = "http://127.0.0.1:5000/api/auth/login"

def thu(ten,body): 
    r = requests.post(URL, json=body)
    print(f"{ten:<20} -> HTTP {r.status_code} | {r.json()}")


thu("Dung", {"username": "dung", "password": "Abc@123"})
thu("Sai mat khau", {"username": "dung", "password": "sai"})
thu("Sai tai khoan", {"username": "nam", "password": "Abc@123"})
thu("Thieu mat khau", {"username": "dung"})
thu("Body rong", {})