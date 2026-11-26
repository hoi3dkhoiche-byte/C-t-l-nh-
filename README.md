# 🎖️ CỜ TƯ LỆNH ONLINE (COMMANDER CHESS)

> Hệ thống game Cờ Tư Lệnh trực tuyến đa nền tảng (Client - Server) mô phỏng chiến thuật quân sự hiện đại của Việt Nam, hỗ trợ thi đấu Multiplayer Realtime, tính điểm ELO, hệ thống uy tín và bảng xếp hạng trực tiếp.

---

## 📌 MỤC LỤC
1. [Giới thiệu dự án](#-giới-thiệu-dự-án)
2. [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
3. [Các tính năng nổi bật](#-các-tính-năng-nổi-bật)
4. [Luật chơi Cờ Tư Lệnh](#-luật-chơi-cờ-tư-lệnh)
5. [Cấu trúc thư mục](#-cấu-trúc-thư-mục)
6. [Hướng dẫn cài đặt & Khởi chạy](#-hướng-dẫn-cài-đặt--khởi-chạy)
   - [Khởi chạy Server Local (Python Flask)](#1-khởi-chạy-server-local-python-flask)
   - [Khởi chạy bằng Docker Compose (MySQL Local)](#2-khởi-chạy-bằng-docker-compose-mysql-local)
   - [Triển khai Production với AWS RDS MySQL](#3-triển-khai-production-với-aws-rds-mysql)
   - [Khởi chạy Client (C# WinForms .NET 9)](#4-khởi-chạy-client-c-winforms-net-9)
7. [Tài liệu API & Socket.IO Events](#-tài-liệu-api--socketio-events)
8. [Cấu hình biến môi trường (.env)](#-cấu-hình-biến-môi-trường-env)

---

## 📖 GIỚI THIỆU DỰ ÁN

**Cờ Tư Lệnh** là môn cờ quân sự trí tuệ do Đại tá Hải quân - nhà văn Nguyễn Quý Hải sáng tạo, mô phỏng các chiến dịch hiệp đồng tác chiến giữa các quân chủng: Lục quân, Hải quân, Không quân và Phòng không.

Dự án này số hóa toàn diện trò chơi với mô hình Client - Server:
* **Backend:** Xử lý toàn bộ logic ván cờ, thẩm định nước đi hợp lệ (`game_engine.py`), quản trị phiên kết nối realtime qua WebSocket (Socket.IO), tính toán biến động điểm ELO chuẩn FIDE, kiểm soát đồng hồ từng lượt đấu, quản lý người chơi bỏ cuộc/mất kết nối và thực thi hệ thống điểm uy tín (Reputation).
* **Frontend:** Giao diện đồ họa C# Windows Forms (.NET 9) với bàn cờ vector 11x12 chuẩn quân sự, hiển thị vành đai hỏa lực, hỗ trợ cõng/ghép quân trực quan và cập nhật Bảng Xếp Hạng thời gian thực.

---

## 🏗️ KIẾN TRÚC HỆ THỐNG

```
┌────────────────────────────────────────────────────────┐
│           C# .NET 9 Client (WinForms)                  │
│  - BoardControl (Vẽ bàn cờ vector, kéo thả quân)       │
│  - AuthService & GameApiService (REST API Client)      │
│  - RealtimeService (SocketIOClient 4.x)                │
└──────────────────────────┬─────────────────────────────┘
                           │ HTTP REST / WebSocket (Socket.IO)
┌──────────────────────────▼─────────────────────────────┐
│             Python Flask Server (Port 5000)            │
│  ┌───────────────────────┬──────────────────────────┐  │
│  │ REST APIs:            │ Realtime Engine:         │  │
│  │ - /api/auth/*         │ - make_move              │  │
│  │ - /api/rooms          │ - turn_timeout           │  │
│  │ - /api/leaderboard    │ - reconnect_sync         │  │
│  │ - /api/matches/*      │ - reputation_penalty     │  │
│  └───────────────────────┴──────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Game Engine Validator (game_engine.py)           │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────────────┬─────────────────────────────┘
                           │ SQLAlchemy ORM / PyMySQL
┌──────────────────────────▼─────────────────────────────┐
│    Database (MySQL 8.4 / AWS RDS / SQLite Fallback)     │
│  - users: Thông tin, mật khẩu băm Bcrypt, ELO, Uy tín │
│  - rooms: Phòng chờ & trạng thái ghép trận             │
│  - matches: Lịch sử đối đầu & biến động điểm ELO      │
│  - password_resets: Mã OTP đặt lại mật khẩu            │
└────────────────────────────────────────────────────────┘
```

---

## ⚡ CÁC TÍNH NĂNG NỔI BẬT

### 1. Tài khoản & Bảo mật
* **Đăng ký / Đăng nhập:** Mã hóa mật khẩu an toàn với Bcrypt, quản lý phiên bằng JWT (JSON Web Tokens).
* **Quên mật khẩu qua OTP:** Tích hợp gửi mã xác thực 6 số qua Gmail SMTP (`smtp.gmail.com`), tự động hết hạn sau 5 phút và giới hạn 5 lần thử chống brute-force.
* **Ghi nhớ đăng nhập:** Lưu thông tin tài khoản cục bộ tiện lợi (`remember.json`).

### 2. Sảnh chờ & Quản lý phòng
* **Tạo & Tìm phòng:** Sinh ngẫu nhiên mã phòng 6 ký tự, hỗ trợ mức cược xu linh hoạt.
* **Hệ thống Uy tín (Reputation System):**
  * Mỗi người chơi bắt đầu với **10 điểm Uy tín**. Hoàn thành ván đấu bình thường được cộng +1 (tối đa 10).
  * Thoát trận giữa chừng hoặc để đứt kết nối quá thời gian chờ: Trừ điểm uy tín theo chuỗi vi phạm (2 → 3 → 4 điểm).
  * Khi điểm uy tín về **0**: Tài khoản bị **khóa 7 ngày**, sau khi hết hạn được khôi phục về mức 6 điểm.
  * **Cơ chế duyệt người chơi uy tín thấp:** Nếu người tham gia có uy tín dưới 6, chủ phòng sẽ nhận được thông báo xét duyệt (Chấp thuận / Từ chối).
  * **Đền bù ELO:** Khi đối thủ bỏ ván, bên ở lại nhận 1/2 ELO thắng; bên bỏ ván bị trừ toàn bộ ELO thua.

### 3. Đấu Realtime qua Socket.IO
* **Đồng bộ nước đi:** Mọi nước đi đều được Server kiểm duyệt qua `game_engine.py` độc lập trước khi phát sóng tới cả 2 kỳ thủ.
* **Đồng hồ từng lượt (Turn Timer):** Mỗi lượt đi có thời gian giới hạn (mặc định 45 giây). Nếu hết giờ, Server tự động huỷ lượt và chuyển quyền đi cho đối thủ kèm hiệu ứng âm thanh/thông báo.
* **Giữ chỗ khi rớt mạng (Reconnection & Resync):** Khi kỳ thủ mất mạng, Server giữ chỗ trong thời gian quy định (mặc định 50 giây). Khi kết nối lại, Server lập tức gửi lại toàn bộ tình trạng bàn cờ, lượt đi hiện tại và lịch sử nước đi để ván đấu tiếp tục liền mạch.

### 4. Bảng xếp hạng ELO & Lịch sử trận đấu
* **Tính điểm ELO chuẩn FIDE:** Thuật toán tính toán kỳ vọng thắng dựa trên chênh lệch trình độ giữa 2 kỳ thủ ($K = 32$).
* **Cập nhật Live (Không cần F5):** Khi một trận đấu bất kỳ kết thúc, Bảng Xếp Hạng của tất cả client đang mở sẽ tự động nhảy vị trí và điểm số mới qua WebSocket broadcast.

---

## 🎮 LUẬT CHƠI CỜ TƯ LỆNH

Bàn cờ có kích thước **11 đường dọc x 12 đường ngang**, chia đôi bởi **Sông** (hàng 5 và hàng 6). Hai cột ngoài cùng bên trái (cột 0 và 1) là **Vùng Biển**, cột 2 là vùng Duyên hải.

### 10 Quân chủng & Đặc tính:
1. **Tư Lệnh (★):** Đi dọc/ngang không giới hạn (như Xe cờ vua), ăn quân trong phạm vi 1 nấc. Khi chiếm Sở chỉ huy đối phương → Thắng ngay.
2. **Bộ Binh (B):** Đi và ăn 1 nấc 4 hướng. Đứng ở bờ (X=2) bắn được quân biển (X=1).
3. **Dân Quân Tóc Dài (D):** Đi và ăn 1 nấc 8 hướng (kể cả chéo).
4. **Công Binh (C):** Đi và ăn 1 nấc. **Có khả năng vượt sông ở bất kỳ vị trí nào** (các quân bộ khác chỉ vượt sông ở ngầm cạn X=5, X=7).
5. **Xe Tăng (T):** Đi và ăn thẳng 1-2 nấc. Có thể cõng Bộ binh, Dân quân hoặc Tư lệnh. Đứng trên bờ bắn được quân trên biển.
6. **Pháo Binh (P):** Tầm bắn 1-3 nấc theo 8 hướng. **Có khả năng bắn vượt qua 1 quân cản**.
7. **Cao Xạ (F):** Đi và ăn thẳng 1 nấc. Vành đai hỏa lực khống chế máy bay bán kính 1 nấc.
8. **Tên Lửa (L):** Tầm bắn 2 nấc theo trục dọc/ngang, 1 nấc đường chéo.
9. **Không Quân (K):** Bay thẳng/chéo 1-4 nấc, **bay vượt qua mọi khối cản**. Có 2 chế độ: ăn thế chỗ hoặc oanh tạc rồi bay về. Bị tiêu diệt nếu bay vào vùng hỏa lực phòng không đối phương (trừ khi là Quân Anh Hùng).
10. **Tàu Chiến (H):** Hoạt động trên biển và sông sâu. Có thể cõng Xe tăng, Không quân, Bộ binh. Đứng từ biển bắn pháo hạm vào đất liền (1-3 nấc).

### Quân Anh Hùng:
* Quân nào tiêu diệt được Tư Lệnh đối phương sẽ được nâng cấp thành **Quân Anh Hùng** (viền vàng): +10 điểm, +1 nấc đi/ăn và miễn nhiễm với vùng phòng không (đối với Không quân).

---

## 📂 CẤU TRÚC THƯ MỤC

```
C-t-l-nh-/
├── CoTuLenh.Client/                # Source code Client (C# .NET 9 WinForms)
│   └── ClientAPP/
│       ├── BoardControl.cs          # Bàn cờ, vẽ vector quân cờ & tương tác UI
│       ├── PieceView.cs             # Định nghĩa đồ họa và loại quân cờ
│       ├── GamePlayForm.cs          # Màn hình ván đấu, đồng hồ, lịch sử nước đi
│       ├── LobbyForm.cs             # Sảnh chờ, danh sách phòng, nạp xu
│       ├── RoomForm.cs              # Phòng chờ ghép trận 2 người
│       ├── LeaderboardForm.cs       # Bảng xếp hạng ELO Realtime
│       ├── LoginForm.cs             # Đăng nhập & nhớ tài khoản
│       ├── RegisterForm.cs          # Đăng ký tài khoản
│       ├── ForgotPasswordForm.cs    # Quên mật khẩu & xác nhận mã OTP
│       ├── AuthService.cs           # HTTP Client xác thực tài khoản
│       ├── GameApiService.cs        # HTTP Client phòng chơi & trận đấu
│       ├── RealtimeService.cs       # Client Socket.IO quản lý kết nối WebSocket
│       ├── SessionService.cs        # Lưu phiên người dùng hiện tại (In-Memory)
│       └── ClientAPP.csproj         # Cấu hình dự án & thư viện NuGet
│
├── cotulenh_server/                 # Source code Server (Python Flask)
│   ├── main.py                      # Server chính: Flask REST API & Socket.IO handlers
│   ├── game_engine.py               # Bộ thẩm định luật cờ, tính nước đi & thắng/thua
│   ├── models.py                    # SQLAlchemy Models (User, Room, Match, PasswordReset)
│   ├── mail_service.py              # Dịch vụ gửi email OTP qua Gmail SMTP
│   ├── pwdrs_routes.py              # Blueprint API quên mật khẩu
│   ├── requirements.txt             # Danh sách thư viện Python
│   ├── Dockerfile                   # Docker build file (tích hợp CA bundle AWS RDS & auto-migrate)
│   ├── docker-compose.yml           # Compose cho môi trường Local (Server Flask + MySQL 8.4 nội bộ)
│   ├── docker-compose.rds.yml       # Compose cho Production (Chỉ chạy Server kết nối AWS RDS)
│   ├── .env.example                 # File mẫu cấu hình biến môi trường (chuẩn hóa local & hướng dẫn RDS)
│   ├── migrations/                  # Script migration cơ sở dữ liệu (Alembic)
│   └── testcase/                    # Kịch bản kiểm thử tự động
│       ├── test_db_config_and_migrations.py # Kiểm thử tách env, TLS RDS, pool, /db-health & migrations
│       ├── manual_socket_move.py    # Test di chuyển quân qua Socket
│       ├── manual_socket_timer.py   # Test đồng hồ đếm ngược lượt đi
│       ├── manual_socket_resync.py  # Test khôi phục trạng thái khi rớt mạng
│       └── manual_socket_reputation.py # Test trừ uy tín & cấm chơi khi bỏ ván
│
├── cotulenh_board_11x12.png         # Hình nền bản đồ địa hình bàn cờ
└── README.md                        # Tài liệu hướng dẫn dự án
```

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT & KHỞI CHẠY

### 1. Khởi chạy Server Local (Python Flask)

#### Yêu cầu:
* Python 3.12 trở lên.

#### Các bước thực hiện:
```bash
# 1. Di chuyển vào thư mục server
cd cotulenh_server

# 2. Tạo và kích hoạt môi trường ảo
python -m venv .venv
# Trên Windows:
.venv\Scripts\activate
# Trên Linux/macOS:
source .venv/bin/activate

# 3. Cài đặt các gói phụ thuộc (bao gồm PyMySQL & cryptography)
pip install -r requirements.txt

# 4. Tạo file cấu hình môi trường (mặc định cấu hình sẵn SQLite local hợp lệ)
cp .env.example .env

# 5. BẮT BUỘC: Chạy nâng cấp Cơ sở dữ liệu lên migration mới nhất trước khi mở server
flask --app main db upgrade

# 6. Khởi chạy Server
python main.py
```
> Server sẽ chạy tại địa chỉ: `http://127.0.0.1:5000` (Kiểm tra tại `/health` và `/db-health`).

---

### 2. Khởi chạy bằng Docker Compose (MySQL Local)

Dành cho phát triển & kiểm thử cục bộ gồm cả Web Server và MySQL 8.4 nội bộ:

```bash
cd cotulenh_server

# Chạy cụm container (tự động khởi động MySQL 8.4, chạy flask db upgrade và bật Gunicorn)
docker compose up --build -d
```
> Server lắng nghe tại port `5000`, kết nối MySQL container (`db:3306`, đồng thời mở `127.0.0.1:3306` để kiểm tra từ máy host nếu cần).

---

### 3. Triển khai Production với AWS RDS MySQL

Khi triển khai lên Cloud (EC2 / ECS), hệ thống tách riêng cấu hình qua [`docker-compose.rds.yml`](cotulenh_server/docker-compose.rds.yml) — **chỉ khởi chạy container `server` và không bật MySQL nội bộ**:

1. **Mạng & Bảo mật AWS:**
   * Tạo AWS RDS **MySQL 8.x** đặt trong Private Subnet cùng VPC với máy chủ chạy backend.
   * Cấu hình **RDS Security Group**: chỉ mở Inbound port `3306` từ Security Group của máy chủ ứng dụng (không mở `0.0.0.0/0`).
2. **Cấu hình biến môi trường trên máy chủ Production (`.env` hoặc file riêng như `.env.rds`):**
   ```env
   APP_ENV=production
   JWT_SECRET_KEY=<chuoi_ngau_nhien_bao_mat_toi_thieu_32_ky_tu>
   # Khuyên dùng bộ biến rời DB_* để mật khẩu chứa ký tự đặc biệt (@, :, /, ?, #, %) được mã hóa an toàn bằng URL.create:
   DB_HOST=<RDS_ENDPOINT>
   DB_PORT=3306
   DB_NAME=cotulenh
   DB_USER=<DB_USER>
   DB_PASSWORD=<DB_PASSWORD>
   DB_SSL_MODE=VERIFY_IDENTITY
   # LƯU Ý: Khi chạy bằng Docker, KHÔNG đặt DB_SSL_CA=certs/global-bundle.pem để container tự dùng CA tích hợp sẵn
   # (/etc/ssl/certs/aws-rds-global-bundle.pem). Chỉ đặt DB_SSL_CA khi chạy Python trực tiếp ngoài Docker trên máy host.
   ```
   > **Cơ chế an toàn (`Fail-fast`):** Khi `APP_ENV=production`, server bắt buộc có kết nối MySQL/RDS và `JWT_SECRET_KEY` hợp lệ; tự động từ chối mọi tham số TLS mâu thuẫn trên URL (như `?ssl_disabled=true`, `?ssl_verify_cert=0`).
   > **Lưu ý dữ liệu:** Thay đổi nơi kết nối sang RDS sẽ tạo cấu trúc bảng mới trên RDS qua Alembic; nếu muốn giữ dữ liệu SQLite cũ cần xuất/nhập dữ liệu riêng.
3. **Khởi chạy Production Compose:**
   ```bash
   cd cotulenh_server
   # Dùng file .env mặc định:
   docker compose -f docker-compose.rds.yml up --build -d
   # Hoặc chỉ định file cấu hình riêng thống nhất qua --env-file:
   docker compose --env-file .env.rds -f docker-compose.rds.yml up --build -d
   ```

---

### 4. Khởi chạy Client (C# WinForms .NET 9)

#### Yêu cầu:
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) hoặc Visual Studio 2022 (v17.12+).

#### Các bước thực hiện:
```bash
# Di chuyển vào thư mục client
cd CoTuLenh.Client/ClientAPP

# Khôi phục dependencies và build
dotnet restore
dotnet build

# Chạy ứng dụng Client
dotnet run
```

---

## 📡 TÀI LIỆU API & SOCKET.IO EVENTS

### REST APIs chính:
| Phương thức | Đường dẫn | Xác thực | Mô tả |
| :--- | :--- | :---: | :--- |
| `GET` | `/health` | Không | Kiểm tra trạng thái sống của Web Server (`200 OK`) |
| `GET` | `/db-health` | Không | Kiểm tra kết nối Database (`200 OK` hoặc `503` có kiểm soát khi mất kết nối) |
| `POST` | `/api/auth/register` | Không | Đăng ký tài khoản mới (`username`, `password`, `email`) |
| `POST` | `/api/auth/login` | Không | Đăng nhập (`identifier`, `password`), trả về JWT token |
| `GET` | `/api/auth/me` | Bearer Token | Lấy thông tin tài khoản, xu, ELO và điểm uy tín |
| `POST` | `/api/auth/send-otp` | Không | Gửi mã OTP xác nhận về email |
| `POST` | `/api/auth/reset-password` | Không | Đặt lại mật khẩu mới bằng OTP |
| `GET` | `/api/rooms` | Bearer Token | Lấy danh sách các phòng chờ và uy tín chủ phòng |
| `POST` | `/api/rooms` | Bearer Token | Tạo phòng chơi mới |
| `GET` | `/api/leaderboard` | Không | Lấy danh sách Top 50 Bảng Xếp Hạng ELO |
| `GET` | `/api/matches/history` | Bearer Token | Lấy lịch sử 20 trận gần nhất của người dùng |
| `POST` | `/api/matches/finish` | Bearer Token | Ghi nhận kết quả ván đấu và kích hoạt tính điểm ELO |

### WebSocket (Socket.IO) Events:
* **Kết nối:** `socket.connect(url, auth={"token": "<JWT_TOKEN>"})`
* **Client gửi lên Server:**
  * `join_room`: Tham gia phòng chơi (`{"room_code": "..."}`).
  * `player_ready`: Báo trạng thái sẵn sàng bắt đầu trận.
  * `make_move`: Gửi nước đi (`{"piece_id": "...", "from_x": ..., "from_y": ..., "to_x": ..., "to_y": ...}`).
  * `leave_room`: Rời phòng (nếu đang trong ván chơi sẽ bị xử thua và phạt uy tín).
  * `approve_join` / `reject_join`: Chủ phòng xét duyệt người chơi có uy tín thấp.
* **Server phát về Client:**
  * `room_joined`: Thông báo vào phòng thành công (kèm dữ liệu ván đấu nếu là kết nối lại giữa chừng).
  * `game_started`: Cả 2 đã sẵn sàng, phát bàn cờ ban đầu và khởi động đồng hồ.
  * `move_made`: Nước đi được duyệt, phát cho 2 bên cập nhật bàn cờ và lượt tiếp theo.
  * `turn_timeout`: Hết thời gian lượt đi, tự động mất lượt.
  * `game_over`: Kết thúc trận đấu (chiếu tướng, hết quân, đầu hàng hoặc rớt mạng quá giờ).
  * `leaderboard_updated`: Bảng xếp hạng ELO toàn server vừa có biến động.

---

## ⚙️ CẤU HÌNH BIẾN MÔI TRƯỜNG (.env)

Sao chép từ file mẫu [`cotulenh_server/.env.example`](cotulenh_server/.env.example) sang `cotulenh_server/.env`:

```env
# 1. Môi trường chạy: development (cho phép SQLite/MySQL local) | production (bắt buộc MySQL/RDS)
APP_ENV=development

# 2. Chuỗi bí mật mã hóa JWT (bắt buộc đổi sang chuỗi ngẫu nhiên >= 32 ký tự khi lên production)
JWT_SECRET_KEY=doi-thanh-chuoi-ngau-nhien-it-nhat-32-ky-tu

# 3. Chuỗi kết nối Database (Thống nhất driver PyMySQL cho MySQL / AWS RDS):
# - Mặc định local SQLite:
DATABASE_URL=sqlite:///cotulenh.db
# - Hoặc kết nối MySQL / AWS RDS qua bộ biến rời (an toàn với mật khẩu có ký tự đặc biệt; nhớ comment dòng DATABASE_URL ở trên):
# DB_HOST=<RDS_ENDPOINT_HOAC_127.0.0.1>
# DB_PORT=3306
# DB_NAME=cotulenh
# DB_USER=<DB_USER>
# DB_PASSWORD=<DB_PASSWORD>
# DB_SSL_MODE=VERIFY_IDENTITY
# (Chỉ đặt DB_SSL_CA=certs/global-bundle.pem khi chạy trực tiếp ngoài Docker; Docker tự dùng CA tích hợp sẵn)

# 4. Connection Pool & Timeout (MySQL / RDS)
DB_CONNECT_TIMEOUT=10
DB_READ_TIMEOUT=30
DB_WRITE_TIMEOUT=30
DB_POOL_SIZE=10
DB_MAX_OVERFLOW=20
DB_POOL_TIMEOUT=10
DB_POOL_RECYCLE=280

# 5. Tài khoản Gmail SMTP gửi OTP
MAIL_USERNAME=your_email@gmail.com
MAIL_APP_PASSWORD=your_16_character_app_password

# 6. Cấu hình thời gian gameplay (giây)
TURN_SECONDS=45
RECONNECT_TIMEOUT=50
JOIN_REQUEST_TIMEOUT=20
```

---

## 👥 ĐÓNG GÓP & BẢN QUYỀN
* **Tác giả luật cờ:** Đại tá Hải quân - Nhà văn Nguyễn Quý Hải.
* **Đội ngũ phát triển phần mềm:** Nhóm phát triển dự án Cờ Tư Lệnh Online.
* **Giấy phép:** Phục vụ mục đích học tập, nghiên cứu và phát triển văn hóa trò chơi trí tuệ Việt Nam.
