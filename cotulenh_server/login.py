import requests

r = requests.post("http://127.0.0.1:5000/api/auth/login")
print("Ma trang thai:", r.status_code)
print("Noi dung:", r.json())