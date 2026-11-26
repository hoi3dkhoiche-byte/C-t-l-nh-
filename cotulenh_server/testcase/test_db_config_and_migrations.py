"""
Kiểm thử toàn diện cấu hình Database (Local vs Production, TLS/SSL RDS, Pool/Timeout,
/db-health có kiểm soát lỗi) và quy trình Migration (tạo DB mới, nâng cấp DB cũ, biên dịch SQL MySQL).
Chạy:
    python testcase/test_db_config_and_migrations.py
"""
import os
from pathlib import Path
import sqlite3
import ssl
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import certifi
from sqlalchemy.exc import OperationalError

SERVER_DIR = Path(__file__).resolve().parents[1]
if str(SERVER_DIR) not in sys.path:
    sys.path.insert(0, str(SERVER_DIR))

import main  # noqa: E402


class DatabaseConfigTests(unittest.TestCase):
    def test_local_defaults_to_sqlite_when_database_url_missing(self):
        uri, opts = main.build_database_config({"APP_ENV": "development"})
        self.assertEqual(uri, "sqlite:///cotulenh.db")
        self.assertTrue(opts["pool_pre_ping"])
        self.assertEqual(opts["connect_args"]["timeout"], 10)

    def test_production_rejects_missing_database_url(self):
        with self.assertRaises(RuntimeError) as ctx:
            main.build_database_config({"APP_ENV": "production"})
        self.assertIn("DATABASE_URL", str(ctx.exception))

    def test_production_rejects_sqlite_fallback(self):
        with self.assertRaises(RuntimeError) as ctx:
            main.build_database_config({
                "APP_ENV": "production",
                "DATABASE_URL": "sqlite:///cotulenh.db",
            })
        self.assertIn("SQLite", str(ctx.exception))

    def test_rejects_placeholder_database_url(self):
        with self.assertRaises(RuntimeError):
            main.build_database_config({
                "APP_ENV": "development",
                "DATABASE_URL": "mysql+pymysql://admin:pass@<RDS_ENDPOINT>:3306/cotulenh",
            })

    def test_production_jwt_secret_validation(self):
        with self.assertRaises(RuntimeError):
            main.resolve_jwt_secret({}, is_production=True)
        with self.assertRaises(RuntimeError):
            main.resolve_jwt_secret(
                {"JWT_SECRET_KEY": "doi-thanh-chuoi-ngau-nhien-it-nhat-32-ky-tu"},
                is_production=True,
            )
        valid_secret = "a" * 40
        self.assertEqual(
            main.resolve_jwt_secret({"JWT_SECRET_KEY": valid_secret}, is_production=True),
            valid_secret,
        )

    def test_mysql_pool_timeout_and_tls_verify_identity(self):
        ca_bundle = certifi.where()
        env = {
            "APP_ENV": "production",
            "DATABASE_URL": "mysql+pymysql://appuser:secretpass@db-instance.ap-southeast-1.rds.amazonaws.com:3306/cotulenh",
            "DB_SSL_MODE": "VERIFY_IDENTITY",
            "DB_SSL_CA": ca_bundle,
            "DB_CONNECT_TIMEOUT": "7",
            "DB_READ_TIMEOUT": "25",
            "DB_WRITE_TIMEOUT": "25",
            "DB_POOL_SIZE": "12",
            "DB_MAX_OVERFLOW": "24",
            "DB_POOL_TIMEOUT": "8",
            "DB_POOL_RECYCLE": "240",
        }
        uri, opts = main.build_database_config(env)
        self.assertTrue(uri.startswith("mysql+pymysql://"))
        self.assertTrue(opts["pool_pre_ping"])
        self.assertEqual(opts["pool_recycle"], 240)
        self.assertEqual(opts["pool_size"], 12)
        self.assertEqual(opts["max_overflow"], 24)
        self.assertEqual(opts["pool_timeout"], 8)

        cargs = opts["connect_args"]
        self.assertEqual(cargs["connect_timeout"], 7)
        self.assertEqual(cargs["read_timeout"], 25)
        self.assertEqual(cargs["write_timeout"], 25)
        self.assertEqual(cargs["charset"], "utf8mb4")

        ssl_ctx = cargs["ssl"]
        self.assertIsInstance(ssl_ctx, ssl.SSLContext)
        self.assertEqual(ssl_ctx.verify_mode, ssl.CERT_REQUIRED)
        self.assertTrue(ssl_ctx.check_hostname)
        self.assertEqual(ssl_ctx.minimum_version, ssl.TLSVersion.TLSv1_2)

    def test_mysql_tls_verify_ca_disables_hostname_check(self):
        env = {
            "APP_ENV": "production",
            "DATABASE_URL": "mysql+pymysql://u:p@10.0.1.25:3306/cotulenh",
            "DB_SSL_MODE": "VERIFY_CA",
            "DB_SSL_CA": certifi.where(),
        }
        _, opts = main.build_database_config(env)
        ssl_ctx = opts["connect_args"]["ssl"]
        self.assertEqual(ssl_ctx.verify_mode, ssl.CERT_REQUIRED)
        self.assertFalse(ssl_ctx.check_hostname)

    def test_mysql_missing_ca_file_raises_clear_error(self):
        with self.assertRaises(RuntimeError) as ctx:
            main.build_database_config({
                "APP_ENV": "production",
                "DATABASE_URL": "mysql+pymysql://u:p@mydb.rds.amazonaws.com:3306/cotulenh",
                "DB_SSL_MODE": "VERIFY_IDENTITY",
                "DB_SSL_CA": "khong_ton_tai/global-bundle.pem",
            })
        self.assertIn("DB_SSL_CA", str(ctx.exception))


class HealthEndpointTests(unittest.TestCase):
    def setUp(self):
        self.client = main.app.test_client()

    def test_health_and_db_health_ok(self):
        res = self.client.get("/health")
        self.assertEqual(res.status_code, 200)
        self.assertEqual(res.get_json()["status"], "ok")

        db_res = self.client.get("/db-health")
        self.assertEqual(db_res.status_code, 200)
        body = db_res.get_json()
        self.assertEqual(body["database"], "connected")

    def test_db_health_returns_controlled_503_when_db_down(self):
        with patch.object(
            main.db.session,
            "execute",
            side_effect=OperationalError("SELECT 1", {}, Exception("Connection refused")),
        ):
            res = self.client.get("/db-health")
        self.assertEqual(res.status_code, 503)
        body = res.get_json()
        self.assertEqual(body["status"], "error")
        self.assertEqual(body["database"], "disconnected")
        self.assertNotIn("Connection refused", str(body))


class MigrationWorkflowTests(unittest.TestCase):
    def _run_flask_db(self, args: list[str], extra_env: dict[str, str]) -> subprocess.CompletedProcess:
        env = os.environ.copy()
        env.update(extra_env)
        cmd = [sys.executable, "-m", "flask", "--app", "main", "db", *args]
        return subprocess.run(
            cmd,
            cwd=str(SERVER_DIR),
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_fresh_db_and_incremental_upgrade_preserve_data(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            db_file = Path(tmpdir) / "test_mig.db"
            db_url = f"sqlite:///{db_file.as_posix()}"
            env = {"APP_ENV": "test", "DATABASE_URL": db_url}

            # 1. Tạo DB ở bản cũ (c8e1f5d2b7a4 - trước khi có các cột uy tín)
            r1 = self._run_flask_db(["upgrade", "c8e1f5d2b7a4"], env)
            self.assertEqual(r1.returncode, 0, msg=r1.stderr)

            # Chèn thử 1 user ở schema cũ
            conn = sqlite3.connect(db_file)
            conn.execute(
                "INSERT INTO users (username, email, password_hash, coins, elo_rating, wins, losses, draws) "
                "VALUES ('olduser', 'old@example.com', 'hash', 500, 1300, 3, 1, 0)"
            )
            conn.commit()
            conn.close()

            # 2. Nâng DB cũ lên head (28dba2a870e1) và kiểm tra bảo toàn dữ liệu + cột mới
            r2 = self._run_flask_db(["upgrade", "head"], env)
            self.assertEqual(r2.returncode, 0, msg=r2.stderr)

            conn = sqlite3.connect(db_file)
            row = conn.execute(
                "SELECT username, coins, elo_rating, reputation, abandon_streak, banned_until "
                "FROM users WHERE username='olduser'"
            ).fetchone()
            conn.close()
            self.assertEqual(row, ("olduser", 500, 1300, 10, 0, None))

    def test_mysql_dialect_offline_sql_generation(self):
        """Kiểm chứng toàn bộ chuỗi 5 migrations biên dịch hợp lệ sang MySQL DDL."""
        env = {
            "APP_ENV": "development",
            "DATABASE_URL": "mysql+pymysql://cotulenh:cotulenh123@127.0.0.1:3306/cotulenh",
            "DB_SSL_MODE": "DISABLED",
        }
        proc = self._run_flask_db(["upgrade", "--sql"], env)
        self.assertEqual(proc.returncode, 0, msg=proc.stderr)
        sql_out = proc.stdout
        self.assertIn("CREATE TABLE users", sql_out)
        self.assertIn("CREATE TABLE rooms", sql_out)
        self.assertIn("CREATE TABLE password_resets", sql_out)
        self.assertIn("CREATE TABLE matches", sql_out)
        self.assertIn("ADD COLUMN reputation", sql_out)
        self.assertIn("28dba2a870e1", sql_out)


if __name__ == "__main__":
    unittest.main(verbosity=2)
