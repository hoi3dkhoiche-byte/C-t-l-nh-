from flask_sqlalchemy import SQLAlchemy

db = SQLAlchemy()

class User(db.Model): 
    __tablename__ = "users"

    id = db.Column(db.Integer, primary_key=True)

    username = db.Column(db.String(50), unique=True, nullable = False)

    email = db.Column(db.String(255), unique=True, nullable = False )

    password_hash = db.Column(db.String(255), nullable = False)
    coins = db.Column(db.Integer, nullable=False, default=0)
    created_at = db.Column(
        db.DateTime,
        nullable=False,
        server_default=db.func.current_timestamp(),
    )

class Room(db.Model):
    __tablename__ = "rooms"

    id = db.Column(db.Integer, primary_key=True)
    room_code = db.Column(db.String(8), unique=True, nullable=False)
    host_user_id = db.Column(
        db.Integer,
        db.ForeignKey("users.id"),
        nullable=False,
    )
    status = db.Column(db.String(20), nullable=False, default="waiting")
    created_at = db.Column(
        db.DateTime,
        nullable=False,
        server_default=db.func.current_timestamp(),
    )