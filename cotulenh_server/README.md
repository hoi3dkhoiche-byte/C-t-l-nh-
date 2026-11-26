# Server Cờ Tư Lệnh

## 1. Chạy trực tiếp trên máy Local (SQLite hoặc MySQL)
```bash
python -m venv .venv
.venv\Scripts\activate          # Linux/macOS: source .venv/bin/activate
python -m pip install -r requirements.txt
cp .env.example .env            # Mặc định cấu hình sẵn SQLite local hợp lệ
flask --app main db upgrade     # Nâng DB lên migration mới nhất (bắt buộc trước khi chạy)
python main.py                  # Chạy tại http://127.0.0.1:5000
```

## 2. Chạy Local bằng Docker Compose (Server + MySQL 8.4 nội bộ)
```bash
docker compose up --build -d
# Hoặc dùng file biến môi trường riêng:
docker compose --env-file .env.local up --build -d
```

## 3. Triển khai Production kết nối AWS RDS MySQL
1. Tạo `.env` (hoặc `.env.rds`) với `APP_ENV=production`, `JWT_SECRET_KEY` (>= 32 ký tự), bộ biến `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` (tự mã hóa ký tự đặc biệt qua `URL.create`) hoặc `DATABASE_URL`, và `DB_SSL_MODE=VERIFY_IDENTITY`.
2. **Về chứng chỉ `DB_SSL_CA`:**
   - **Chạy bằng Docker:** Không đặt `DB_SSL_CA` để container tự dùng chứng chỉ AWS RDS tích hợp sẵn tại `/etc/ssl/certs/aws-rds-global-bundle.pem`.
   - **Chạy trực tiếp ngoài Docker:** Tải `https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem` về `certs/global-bundle.pem` và đặt `DB_SSL_CA=certs/global-bundle.pem`.
3. Khởi chạy file Compose tách biệt không kèm MySQL nội bộ:
```bash
docker compose -f docker-compose.rds.yml up --build -d
# Hoặc chỉ định file biến môi trường riêng thống nhất qua --env-file:
docker compose --env-file .env.rds -f docker-compose.rds.yml up --build -d
```

## 4. Kiểm tra trạng thái kết nối
- `GET /health`: Trạng thái server (`200 OK`)
- `GET /db-health`: Trạng thái kết nối DB (`200 OK` hoặc `503 Service Unavailable` có kiểm soát)
