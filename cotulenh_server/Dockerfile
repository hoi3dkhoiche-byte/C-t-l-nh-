FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# Tải gói chứng chỉ CA chính thức của AWS RDS để xác minh TLS (VERIFY_CA / VERIFY_IDENTITY)
RUN python -c "import urllib.request; urllib.request.urlretrieve('https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem', '/etc/ssl/certs/aws-rds-global-bundle.pem')"

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
 && pip install --no-cache-dir gunicorn

COPY . .

EXPOSE 5000

CMD ["sh", "-c", "if [ \"${RUN_MIGRATIONS:-1}\" = \"1\" ]; then flask --app main db upgrade; fi && exec gunicorn -k gthread --workers 1 --threads 100 --timeout 120 --bind 0.0.0.0:5000 main:app"]
