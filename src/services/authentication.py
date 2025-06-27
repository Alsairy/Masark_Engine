"""
Authentication Service for Masark Personality-Career Matching Engine
Handles JWT tokens, user authentication, and role-based access control
"""

import jwt
import bcrypt
from datetime import datetime, timedelta
from functools import wraps
from flask import current_app, request, jsonify
from src.models.masark_models import db, User

class AuthenticationService:
    """Service for handling authentication and authorization"""
    
    def __init__(self):
        self.secret_key = None
        self.token_expiry_hours = 24
    
    def initialize(self, secret_key, token_expiry_hours=24):
        """Initialize the authentication service"""
        self.secret_key = secret_key
        self.token_expiry_hours = token_expiry_hours
    
    def hash_password(self, password):
        """Hash a password using bcrypt"""
        salt = bcrypt.gensalt()
        hashed = bcrypt.hashpw(password.encode('utf-8'), salt)
        return hashed.decode('utf-8')
    
    def verify_password(self, password, hashed_password):
        """Verify a password against its hash"""
        return bcrypt.checkpw(password.encode('utf-8'), hashed_password.encode('utf-8'))
    
    def generate_token(self, user_id, username, role):
        """Generate a JWT token for a user"""
        if not self.secret_key:
            raise ValueError("JWT secret key not configured")
            
        payload = {
            'user_id': user_id,
            'username': username,
            'role': role,
            'exp': datetime.utcnow() + timedelta(hours=self.token_expiry_hours),
            'iat': datetime.utcnow()
        }
        
        token = jwt.encode(payload, self.secret_key, algorithm='HS256')
        return token
    
    def verify_token(self, token):
        """Verify and decode a JWT token"""
        try:
            if not self.secret_key:
                raise ValueError("JWT secret key not configured")
                
            payload = jwt.decode(token, self.secret_key, algorithms=['HS256'])
            return payload
        except jwt.ExpiredSignatureError:
            return {'error': 'Token has expired'}
        except jwt.InvalidTokenError:
            return {'error': 'Invalid token'}
    
    def create_user(self, username, password, email, role='USER', full_name=None):
        """Create a new user account"""
        try:
            # Check if username already exists
            existing_user = User.query.filter_by(username=username).first()
            if existing_user:
                return {'success': False, 'error': 'Username already exists'}
            
            # Check if email already exists
            existing_email = User.query.filter_by(email=email).first()
            if existing_email:
                return {'success': False, 'error': 'Email already exists'}
            
            # Hash password
            hashed_password = self.hash_password(password)
            
            # Create user
            user = User(
                username=username,
                password_hash=hashed_password,
                email=email,
                full_name=full_name or username,
                role=role,
                is_active=True,
                created_at=datetime.utcnow()
            )
            
            db.session.add(user)
            db.session.commit()
            
            return {
                'success': True,
                'user': {
                    'id': user.id,
                    'username': user.username,
                    'email': user.email,
                    'full_name': user.full_name,
                    'role': user.role,
                    'created_at': user.created_at.isoformat()
                }
            }
            
        except Exception as e:
            db.session.rollback()
            return {'success': False, 'error': str(e)}
    
    def authenticate_user(self, username, password):
        """Authenticate a user with username and password"""
        try:
            user = User.query.filter_by(username=username, is_active=True).first()
            
            if not user:
                return {'success': False, 'error': 'Invalid username or password'}
            
            if not self.verify_password(password, user.password_hash):
                return {'success': False, 'error': 'Invalid username or password'}
            
            # Update last login
            user.last_login = datetime.utcnow()
            db.session.commit()
            
            # Generate token
            token = self.generate_token(user.id, user.username, user.role)
            
            return {
                'success': True,
                'token': token,
                'user': {
                    'id': user.id,
                    'username': user.username,
                    'email': user.email,
                    'full_name': user.full_name,
                    'role': user.role,
                    'last_login': user.last_login.isoformat()
                }
            }
            
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def get_user_by_token(self, token):
        """Get user information from a JWT token"""
        payload = self.verify_token(token)
        
        if 'error' in payload:
            return payload
        
        user = User.query.filter_by(id=payload['user_id'], is_active=True).first()
        if not user:
            return {'error': 'User not found or inactive'}
        
        return {
            'success': True,
            'user': {
                'id': user.id,
                'username': user.username,
                'email': user.email,
                'full_name': user.full_name,
                'role': user.role,
                'last_login': user.last_login.isoformat() if user.last_login else None
            }
        }
    
    def change_password(self, user_id, old_password, new_password):
        """Change a user's password"""
        try:
            user = User.query.filter_by(id=user_id, is_active=True).first()
            if not user:
                return {'success': False, 'error': 'User not found'}
            
            if not self.verify_password(old_password, user.password_hash):
                return {'success': False, 'error': 'Current password is incorrect'}
            
            # Hash new password
            user.password_hash = self.hash_password(new_password)
            user.updated_at = datetime.utcnow()
            db.session.commit()
            
            return {'success': True, 'message': 'Password changed successfully'}
            
        except Exception as e:
            db.session.rollback()
            return {'success': False, 'error': str(e)}
    
    def deactivate_user(self, user_id):
        """Deactivate a user account"""
        try:
            user = User.query.filter_by(id=user_id).first()
            if not user:
                return {'success': False, 'error': 'User not found'}
            
            user.is_active = False
            user.updated_at = datetime.utcnow()
            db.session.commit()
            
            return {'success': True, 'message': 'User deactivated successfully'}
            
        except Exception as e:
            db.session.rollback()
            return {'success': False, 'error': str(e)}
    
    def list_users(self, limit=50, offset=0):
        """List all users with pagination"""
        try:
            users = User.query.offset(offset).limit(limit).all()
            total_users = User.query.count()
            
            user_list = []
            for user in users:
                user_list.append({
                    'id': user.id,
                    'username': user.username,
                    'email': user.email,
                    'full_name': user.full_name,
                    'role': user.role,
                    'is_active': user.is_active,
                    'created_at': user.created_at.isoformat(),
                    'last_login': user.last_login.isoformat() if user.last_login else None
                })
            
            return {
                'success': True,
                'users': user_list,
                'total_users': total_users,
                'limit': limit,
                'offset': offset
            }
            
        except Exception as e:
            return {'success': False, 'error': str(e)}

# Authentication decorators
def token_required(f):
    """Decorator to require valid JWT token"""
    @wraps(f)
    def decorated(*args, **kwargs):
        token = None
        
        # Get token from header
        if 'Authorization' in request.headers:
            auth_header = request.headers['Authorization']
            try:
                token = auth_header.split(" ")[1]  # Bearer <token>
            except IndexError:
                return jsonify({'error': 'Invalid token format'}), 401
        
        if not token:
            return jsonify({'error': 'Token is missing'}), 401
        
        # Verify token
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        payload = auth_service.verify_token(token)
        if 'error' in payload:
            return jsonify({'error': payload['error']}), 401
        
        # Add user info to request context
        request.current_user = payload
        return f(*args, **kwargs)
    
    return decorated

def admin_required(f):
    """Decorator to require admin role"""
    @wraps(f)
    def decorated(*args, **kwargs):
        if not hasattr(request, 'current_user'):
            return jsonify({'error': 'Authentication required'}), 401
        
        if request.current_user.get('role') != 'ADMIN':
            return jsonify({'error': 'Admin access required'}), 403
        
        return f(*args, **kwargs)
    
    return decorated

def role_required(required_roles):
    """Decorator to require specific roles"""
    def decorator(f):
        @wraps(f)
        def decorated(*args, **kwargs):
            if not hasattr(request, 'current_user'):
                return jsonify({'error': 'Authentication required'}), 401
            
            user_role = request.current_user.get('role')
            if user_role not in required_roles:
                return jsonify({'error': f'Access denied. Required roles: {", ".join(required_roles)}'}), 403
            
            return f(*args, **kwargs)
        
        return decorated
    return decorator

# Initialize default admin user
def create_default_admin():
    """Create default admin user if it doesn't exist"""
    import os
    import secrets
    
    try:
        admin_user = User.query.filter_by(username='admin').first()
        if not admin_user:
            auth_service = AuthenticationService()
            
            secret_key = os.environ.get('JWT_SECRET_KEY')
            if not secret_key:
                secret_key = secrets.token_urlsafe(32)
                print("⚠️  Generated temporary JWT secret key. Set JWT_SECRET_KEY environment variable for production!")
            
            auth_service.initialize(secret_key)
            
            # Generate secure random password
            admin_password = secrets.token_urlsafe(16)
            
            result = auth_service.create_user(
                username='admin',
                password=admin_password,
                email='admin@masark.com',
                role='ADMIN',
                full_name='System Administrator'
            )
            
            if result['success']:
                print("✅ Default admin user created:")
                print("   Username: admin")
                print(f"   Password: {admin_password}")
                print("   ⚠️  IMPORTANT: Save this password securely and change it after first login!")
                print("   ⚠️  This password will not be shown again!")
            else:
                print(f"❌ Failed to create admin user: {result['error']}")
        else:
            print("✅ Admin user already exists")
            
    except Exception as e:
        print(f"❌ Error creating default admin user: {str(e)}")

