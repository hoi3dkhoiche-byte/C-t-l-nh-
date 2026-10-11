import os
import smtplib
from email.message import EmailMessage

SMTP_HOST= "smtp.gmail.com"
SMTP_PORT= 587


def send_otp_email(to_email, otp):
    mail_user = os.getenv("MAIL_USERNAME")
    mail_password = os.getenv("MAIL_APP_PASSWORD")

    msg = EmailMessage()
    msg["Subject"] = "Mã OTP đặt lại mật khẩu - Cờ Tư Lệnh"
    msg["From"] = mail_user
    msg["To"] = to_email
    msg.set_content(
        f"Mã OTP của bạn là: {otp} \n"
        "Mã có hiệu lực trong 5 phút. \n"
        "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này."
    )

    with smtplib.SMTP(SMTP_HOST, SMTP_PORT, timeout=10) as smtp: 
        smtp.starttls()
        smtp.login(mail_user, mail_password)
        smtp.send_message(msg)