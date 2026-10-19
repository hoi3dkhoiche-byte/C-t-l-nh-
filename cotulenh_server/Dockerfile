FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
 && pip install --no-cache-dir gunicorn cryptography


COPY . .

EXPOSE 5000


CMD ["sh", "-c", "flask --app main db upgrade && gunicorn --workers 1 --threads 100 --bind 0.0.0.0:5000 main:app"]