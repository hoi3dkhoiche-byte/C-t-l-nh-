"""
Kiểm thử toàn diện cấu hình Database (Local vs Production, TLS/SSL RDS, chặn ghi đè TLS qua URL,
tắt hẳn TLS với DISABLED trên PyMySQL, URL.create cho mật khẩu có ký tự đặc biệt, Docker Compose --env-file,
Pool/Timeout, /db-health có kiểm soát lỗi) và quy trình Migration.
Chạy:
    python testcase/test_db_config_and_migrations.py
"""
import json
import os
from pathlib import Path
import shutil
import sqlite3
import ssl
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import certifi
import pymysql.connections
from sqlalchemy import create_engine
from sqlalchemy.exc import OperationalError

SERVER_DIR = Path(__file__).resolve().parents[1]
if str(SERVER_DIR) not in sys.path:
    sys.path.insert(0, str(SERVER_DIR))

import main  # noqa: E402


def build_deferred_pymysql_connection(uri: str, engine_options: dict) -> pymysql.connections.Connection:
    """
    Tạo đối tượng pymysql.connections.Connection thực tế thông qua SQLAlchemy dialect
    với defer_connect=True (không cần mạng) để kiểm chứng chính xác trạng thái SSL và mật khẩu.
    """
    engine = create_engine(uri, **engine_options)
    try:
        cargs, cparams = engine.dialect.create_connect_args(engine.url)
        merged_params = dict(cparams)
        merged_params.update(engine_options.get("connect_args", {}))
        merged_params["defer_connect"] = True
        return pymysql.connections.Connection(*cargs, **merged_params)
    finally:
        engine.dispose()


class DatabaseConfigTests(unittest.TestCase):
    def test_local_defaults_to_sqlite_when_database_url_missing(self):
        uri, opts = main.build_database_config({"APP_ENV": "development"})
        self.assertEqual(uri, "sqlite:///cotulenh.db")
        self.assertTrue(opts["pool_pre_ping"])
        self.assertEqual(opts["connect_args"]["timeout"], 10)

    def test_production_rejects_missing_database_url_and_host(self):
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

    def test_rejects_placeholder_database_url_and_db_host(self):
        with self.assertRaises(RuntimeError):
            main.build_database_config({
                "APP_ENV": "development",
                "DATABASE_URL": "mysql+pymysql://admin:pass@<RDS_ENDPOINT>:3306/cotulenh",
            })
        with self.assertRaises(RuntimeError):
            main.build_database_config({
                "APP_ENV": "production",
                "DB_HOST": "<RDS_ENDPOINT>",
                "DB_USER": "admin",
                "DB_PASSWORD": "secret",
                "DB_NAME": "cotulenh",
                "DB_SSL_CA": certifi.where(),
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

    def test_rejects_conflicting_tls_query_parameters_in_url(self):
        """Điểm 1a: Không cho phép query string trên URL ghi đè cấu hình TLS."""
        malicious_queries = [
            "ssl_disabled=true",
            "ssl_verify_cert=0",
            "ssl_verify_identity=0",
            "ssl_check_hostname=false",
            "ssl_ca=/tmp/fake.pem",
            "tls_version=TLSv1.2",
            "read_default_file=/etc/my.cnf",
            "client_flag=0",
        ]
        for query in malicious_queries:
            with self.subTest(query=query):
                with self.assertRaises(RuntimeError) as ctx:
                    main.build_database_config({
                        "APP_ENV": "production",
                        "DATABASE_URL": f"mysql+pymysql://u:p@mydb.rds.amazonaws.com:3306/cotulenh?{query}",
                        "DB_SSL_MODE": "VERIFY_IDENTITY",
                        "DB_SSL_CA": certifi.where(),
                    })
                self.assertIn("DATABASE_URL", str(ctx.exception))

    def test_disabled_mode_truly_disables_tls_in_pymysql_driver(self):
        """Điểm 1b: DB_SSL_MODE=DISABLED phải thực sự tắt TLS (conn.ssl == False) trên PyMySQL."""
        uri, opts = main.build_database_config({
            "APP_ENV": "development",
            "DB_HOST": "127.0.0.1",
            "DB_USER": "cotulenh",
            "DB_PASSWORD": "secret",
            "DB_NAME": "cotulenh",
            "DB_SSL_MODE": "DISABLED",
        })
        self.assertTrue(opts["connect_args"]["ssl_disabled"])
        self.assertNotIn("ssl", opts["connect_args"])

        conn = build_deferred_pymysql_connection(uri, opts)
        self.assertFalse(conn.ssl)
        self.assertFalse(conn._ssl_required)

    def test_mysql_pool_timeout_and_tls_verify_identity_on_driver(self):
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
        self.assertEqual(opts["pool_recycle"], 240)
        self.assertEqual(opts["pool_size"], 12)
        self.assertEqual(opts["max_overflow"], 24)
        self.assertEqual(opts["pool_timeout"], 8)

        conn = build_deferred_pymysql_connection(uri, opts)
        self.assertTrue(conn.ssl)
        self.assertTrue(conn._ssl_required)
        self.assertEqual(conn.ctx.verify_mode, ssl.CERT_REQUIRED)
        self.assertTrue(conn.ctx.check_hostname)
        self.assertEqual(conn.ctx.minimum_version, ssl.TLSVersion.TLSv1_2)

    def test_mysql_tls_verify_ca_disables_hostname_check(self):
        env = {
            "APP_ENV": "production",
            "DATABASE_URL": "mysql+pymysql://u:p@10.0.1.25:3306/cotulenh",
            "DB_SSL_MODE": "VERIFY_CA",
            "DB_SSL_CA": certifi.where(),
        }
        uri, opts = main.build_database_config(env)
        conn = build_deferred_pymysql_connection(uri, opts)
        self.assertTrue(conn.ssl)
        self.assertEqual(conn.ctx.verify_mode, ssl.CERT_REQUIRED)
        self.assertFalse(conn.ctx.check_hostname)

    def test_special_characters_in_password_via_url_create(self):
        """Điểm 2: Mật khẩu chứa @, :, /, ?, #, %, &, =, khoảng trắng không làm hỏng hostname/URL."""
        tricky_password = "P@ss:w/rd?1#2%3!a&b=c space"
        uri, opts = main.build_database_config({
            "APP_ENV": "production",
            "DB_HOST": "mydb.ap-southeast-1.rds.amazonaws.com",
            "DB_PORT": "3306",
            "DB_USER": "admin@corp",
            "DB_PASSWORD": tricky_password,
            "DB_NAME": "cotulenh",
            "DB_SSL_MODE": "VERIFY_IDENTITY",
            "DB_SSL_CA": certifi.where(),
        })
        conn = build_deferred_pymysql_connection(uri, opts)
        self.assertEqual(conn.host, "mydb.ap-southeast-1.rds.amazonaws.com")
        self.assertEqual(conn.port, 3306)
        self.assertEqual(conn.user, "admin@corp")
        self.assertEqual(conn.password, tricky_password.encode("utf-8"))

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


class MigrationAndComposeTests(unittest.TestCase):
    def _run_flask_db(self, args: list[str], extra_env: dict[str, str]) -> subprocess.CompletedProcess:
        env = os.environ.copy()
        for k in ("DATABASE_URL", "DB_HOST", "DB_USER", "DB_PASSWORD", "DB_NAME", "DB_SSL_MODE", "DB_SSL_CA"):
            env.pop(k, None)
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

            r1 = self._run_flask_db(["upgrade", "c8e1f5d2b7a4"], env)
            self.assertEqual(r1.returncode, 0, msg=r1.stderr)

            conn = sqlite3.connect(db_file)
            conn.execute(
                "INSERT INTO users (username, email, password_hash, coins, elo_rating, wins, losses, draws) "
                "VALUES ('olduser', 'old@example.com', 'hash', 500, 1300, 3, 1, 0)"
            )
            conn.commit()
            conn.close()

            r2 = self._run_flask_db(["upgrade", "head"], env)
            self.assertEqual(r2.returncode, 0, msg=r2.stderr)

            conn = sqlite3.connect(db_file)
            row = conn.execute(
                "SELECT username, coins, elo_rating, reputation, abandon_streak, banned_until "
                "FROM users WHERE username='olduser'"
            ).fetchone()
            conn.close()
            self.assertEqual(row, ("olduser", 500, 1300, 10, 0, None))

    def test_mysql_dialect_offline_sql_with_special_password(self):
        """Kiểm chứng chuỗi 5 migrations biên dịch sang MySQL DDL ngay cả khi mật khẩu có @:/?#%."""
        env = {
            "APP_ENV": "development",
            "DB_HOST": "127.0.0.1",
            "DB_PORT": "3306",
            "DB_USER": "cotulenh",
            "DB_PASSWORD": "P@ss:w/rd?1#2%3",
            "DB_NAME": "cotulenh",
            "DB_SSL_MODE": "DISABLED",
        }
        proc = self._run_flask_db(["upgrade", "--sql"], env)
        self.assertEqual(proc.returncode, 0, msg=proc.stderr)
        self.assertIn("CREATE TABLE users", proc.stdout)
        self.assertIn("ADD COLUMN reputation", proc.stdout)

    @unittest.skipUnless(shutil.which("docker"), "Docker CLI không có sẵn trên máy này")
    def test_docker_compose_unified_env_file_and_special_passwords(self):
        """Điểm 2, 3, 4: Kiểm chứng --env-file cho cả local compose và RDS compose."""
        with tempfile.TemporaryDirectory() as tmpdir:
            rds_env = Path(tmpdir) / "custom.rds.env"
            rds_env.write_text(
                "JWT_SECRET_KEY=0123456789abcdef0123456789abcdef_prod\n"
                "DB_HOST=mydb.ap-southeast-1.rds.amazonaws.com\n"
                "DB_USER=rdsadmin\n"
                "DB_PASSWORD='Rds@Pass:w/rd#2026?!'\n"
                "DB_NAME=cotulenh\n",
                encoding="utf-8",
            )
            proc_rds = subprocess.run(
                [
                    "docker", "compose",
                    "--env-file", str(rds_env),
                    "-f", "docker-compose.rds.yml",
                    "config", "--format", "json",
                ],
                cwd=str(SERVER_DIR),
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(proc_rds.returncode, 0, msg=proc_rds.stderr)
            rds_cfg = json.loads(proc_rds.stdout)
            srv_env = rds_cfg["services"]["server"]["environment"]
            self.assertEqual(srv_env["APP_ENV"], "production")
            self.assertEqual(srv_env["DB_HOST"], "mydb.ap-southeast-1.rds.amazonaws.com")
            self.assertEqual(srv_env["DB_PASSWORD"], "Rds@Pass:w/rd#2026?!")
            self.assertEqual(srv_env["DB_SSL_CA"], "/etc/ssl/certs/aws-rds-global-bundle.pem")

            local_env = Path(tmpdir) / "custom.local.env"
            local_env.write_text(
                "DB_USER=localuser\n"
                "DB_PASSWORD='Loc@l:Pass/Word#123'\n"
                "DB_NAME=cotulenh_dev\n",
                encoding="utf-8",
            )
            proc_local = subprocess.run(
                [
                    "docker", "compose",
                    "--env-file", str(local_env),
                    "-f", "docker-compose.yml",
                    "config", "--format", "json",
                ],
                cwd=str(SERVER_DIR),
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(proc_local.returncode, 0, msg=proc_local.stderr)
            local_cfg = json.loads(proc_local.stdout)
            db_env = local_cfg["services"]["db"]["environment"]
            local_srv_env = local_cfg["services"]["server"]["environment"]
            self.assertEqual(db_env["MYSQL_PASSWORD"], "Loc@l:Pass/Word#123")
            self.assertEqual(local_srv_env["DB_PASSWORD"], "Loc@l:Pass/Word#123")
            self.assertEqual(local_srv_env["DB_HOST"], "db")
            self.assertEqual(local_srv_env["DATABASE_URL"], "")


if __name__ == "__main__":
    unittest.main(verbosity=2)
