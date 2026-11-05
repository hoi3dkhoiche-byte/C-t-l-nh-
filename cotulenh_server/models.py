from flask_sqlalchemy import SQLAlchemy

db = SQLAlchemy()

class User(db.Model): 
    __tablename__ = "users"

    id = db.Column(db.Integer, primary_key=True)

    username = db.Column(db.String(50), unique=True, nullable = False)

    email = db.Column(db.String(255), unique=True, nullable = False )

    password_hash = db.Column(db.String(255), nullable = False)
    coins = db.Column(db.Integer, nullable=False, default=0)
    elo_rating = db.Column(db.Integer, nullable=False, default=1200, index=True)
    wins = db.Column(db.Integer, nullable=False, default=0)
    losses = db.Column(db.Integer, nullable=False, default=0)
    draws = db.Column(db.Integer, nullable=False, default=0)
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

class PasswordReset(db.Model): 
    __tablename__ = "password_resets"

    id = db.Column(db.Integer, primary_key =True)

    user_id = db.Column(
        db.Integer,
        db.ForeignKey("users.id"),
        nullable = False,
    )

    otp_hash = db.Column(db.String(255), nullable=False)

    expires_at = db.Column(db.DateTime, nullable=False)

    attempts = db.Column(db.Integer, nullable=False, default=0)

    used = db.Column(db.Boolean, nullable=False, default=False)

    created_at = db.Column (
        db.DateTime,
        nullable = False,
        server_default=db.func.current_timestamp(),
    )


class Match(db.Model):
    __tablename__ = "matches"

    id = db.Column(db.Integer, primary_key=True)
    room_code = db.Column(db.String(8), nullable=True)
    red_player_id = db.Column(db.Integer, db.ForeignKey("users.id"), nullable=False)
    blue_player_id = db.Column(db.Integer, db.ForeignKey("users.id"), nullable=False)
    winner_id = db.Column(db.Integer, db.ForeignKey("users.id"), nullable=True)
    result = db.Column(db.String(20), nullable=False)  # "red_win", "blue_win", "draw"
    red_elo_change = db.Column(db.Integer, nullable=False, default=0)
    blue_elo_change = db.Column(db.Integer, nullable=False, default=0)
    coin_bet = db.Column(db.Integer, nullable=False, default=0)
    created_at = db.Column(
        db.DateTime,
        nullable=False,
        server_default=db.func.current_timestamp(),
    )


