# Server Cờ Tư Lệnh

## Cài đặt (lần đầu)
    python -m venv .venv
    .venv\Scripts\activate
    python -m pip install -r requirements.txt

## Chạy
    python app.py           # server chạy tại http://127.0.0.1:5000

## API đã có
- POST /api/auth/login     {username, password}
- POST /api/auth/register  {username, password, email}