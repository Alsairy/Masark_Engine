import os
import sys
# DON'T CHANGE THIS !!!
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from flask import Flask, send_from_directory
from flask_cors import CORS
from src.models.masark_models import db
from src.routes.user import user_bp
from src.routes.assessment import assessment_bp
from src.routes.system import system_bp
from src.routes.careers import careers_bp
from src.routes.reports import reports_bp
from src.routes.auth import auth_bp
from src.routes.localization import localization_bp

app = Flask(__name__, static_folder=os.path.join(os.path.dirname(__file__), 'static'))

# Enable CORS for all routes to allow frontend-backend interaction
CORS(app, origins="*")

# Configuration
import secrets
secret_key = os.environ.get('SECRET_KEY')
if not secret_key:
    secret_key = secrets.token_urlsafe(32)
    print("⚠️  Generated temporary SECRET_KEY. Set SECRET_KEY environment variable for production!")
app.config['SECRET_KEY'] = secret_key
app.config['SQLALCHEMY_DATABASE_URI'] = f"sqlite:///{os.path.join(os.path.dirname(__file__), 'database', 'masark.db')}"
app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False

# Register blueprints
app.register_blueprint(user_bp, url_prefix='/api')
app.register_blueprint(assessment_bp, url_prefix='/api/assessment')
app.register_blueprint(system_bp, url_prefix='/api/system')
app.register_blueprint(careers_bp, url_prefix='/api/careers')
app.register_blueprint(reports_bp, url_prefix='/api/reports')
app.register_blueprint(auth_bp, url_prefix='/api/auth')
app.register_blueprint(localization_bp, url_prefix='/api/localization')

# Initialize database
db.init_app(app)

# Create tables and initialize data
with app.app_context():
    db.create_all()
    
    # Initialize database with seed data if not already done
    from src.models.masark_models import PersonalityType
    if PersonalityType.query.count() == 0:
        print("Initializing database with seed data...")
        from src.database_init import initialize_database
        initialize_database()
    
    # Create default admin user
    print("Initializing authentication system...")
    from src.services.authentication import create_default_admin
    create_default_admin()

@app.route('/', defaults={'path': ''})
@app.route('/<path:path>')
def serve(path):
    static_folder_path = app.static_folder
    if static_folder_path is None:
            return "Static folder not configured", 404

    if path != "" and os.path.exists(os.path.join(static_folder_path, path)):
        return send_from_directory(static_folder_path, path)
    else:
        index_path = os.path.join(static_folder_path, 'index.html')
        if os.path.exists(index_path):
            return send_from_directory(static_folder_path, 'index.html')
        else:
            return "index.html not found", 404


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5003))
    app.run(host='0.0.0.0', port=port, debug=True)
