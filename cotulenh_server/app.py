from flask import Flask

from models import db
from auth_routes import auth_bp


app = Flask(__name__)

app.config["SQLALCHEMY_DATABASE_URI"] = "sqlite:///cotulenh.db" #Sau doi sang SQL

db.init_app(app)

#Gan API dang nhap / dang ky
app.register_blueprint(auth_bp)

with app.app_context():
    db.create_all()


@app.get("/")
def home(): 
    return "Server CTL dang chay!"

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)