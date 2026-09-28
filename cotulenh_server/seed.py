import bcrypt

from app import app
# Lấy đối tượng app từ app.py. Nhờ có "if __name__ == '__main__'" trong app.py,
# import vào đây chỉ tạo app và bảng, KHÔNG bật server.
from models import db, User

with app.app_context():  # mọi thao tác database phải nằm trong app_context
    existing = db.session.scalar(db.select(User).filter_by(username="dung"))  # đã có "dung" chưa?

    if existing:
        print("Da co tai khoan dung, bo qua.")  # chạy lại nhiều lần không bị tạo trùng
    else:
        password_hash = bcrypt.hashpw(
            "Abc@123".encode("utf-8"),  # mật khẩu gốc, đổi sang bytes
            bcrypt.gensalt(),           # tạo "muối" (salt): chuỗi ngẫu nhiên trộn vào trước khi băm
        ).decode("utf-8")               # kết quả bytes -> str để lưu vào cột String

        user = User(                    # tạo một đối tượng User = một dòng mới cho bảng users
            username="dung",
            email="dung@example.com",
            password_hash=password_hash,  # lưu chuỗi đã băm, không phải "Abc@123"
        )
        db.session.add(user)   # đưa dòng mới vào "giỏ" chờ lưu (giống git add)
        db.session.commit()    # lưu thật xuống database (giống git commit)

        print("Da tao tai khoan dung.")
        print("Chuoi luu trong database:", password_hash)
        # Dạng: $2b$12$ + 22 ký tự salt + 31 ký tự kết quả băm
        #   $2b$ : phiên bản thuật toán bcrypt
        #   12   : độ "khó" (máy phải băm 2^12 vòng), càng cao càng chậm, kẻ đoán mò càng mệt