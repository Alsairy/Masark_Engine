"""
Authentication API Routes for Masark Personality-Career Matching Engine
Handles login, logout, user management, and authentication operations
"""

from flask import Blueprint, request, jsonify, current_app
from src.services.authentication import AuthenticationService, token_required, admin_required, role_required
from datetime import datetime

auth_bp = Blueprint('auth', __name__)

@auth_bp.route('/login', methods=['POST'])
def login():
    """
    User login endpoint
    Expected payload:
    {
        "username": "admin",
        "password": "admin123"
    }
    """
    try:
        data = request.get_json() or {}
        
        username = data.get('username', '').strip()
        password = data.get('password', '')
        
        # Validate inputs
        if not username or not password:
            return jsonify({
                'success': False,
                'error': 'Username and password are required'
            }), 400
        
        # Authenticate user
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        result = auth_service.authenticate_user(username, password)
        
        if result['success']:
            return jsonify({
                'success': True,
                'message': 'Login successful',
                'token': result['token'],
                'user': result['user']
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 401
            
    except Exception as e:
        current_app.logger.error(f"Login error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Login failed',
            'message': str(e)
        }), 500

@auth_bp.route('/logout', methods=['POST'])
@token_required
def logout():
    """User logout endpoint (token-based, no server-side session to clear)"""
    try:
        return jsonify({
            'success': True,
            'message': 'Logout successful'
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Logout error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Logout failed',
            'message': str(e)
        }), 500

@auth_bp.route('/me', methods=['GET'])
@token_required
def get_current_user():
    """Get current user information from token"""
    try:
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        # Get token from header
        auth_header = request.headers.get('Authorization', '')
        token = auth_header.split(" ")[1] if auth_header.startswith('Bearer ') else None
        
        if not token:
            return jsonify({
                'success': False,
                'error': 'Token not found'
            }), 401
        
        result = auth_service.get_user_by_token(token)
        
        if result.get('success'):
            return jsonify({
                'success': True,
                'user': result['user']
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result.get('error', 'Failed to get user info')
            }), 401
            
    except Exception as e:
        current_app.logger.error(f"Get current user error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get user information',
            'message': str(e)
        }), 500

@auth_bp.route('/change-password', methods=['POST'])
@token_required
def change_password():
    """
    Change user password
    Expected payload:
    {
        "old_password": "current_password",
        "new_password": "new_password"
    }
    """
    try:
        data = request.get_json() or {}
        
        old_password = data.get('old_password', '')
        new_password = data.get('new_password', '')
        
        # Validate inputs
        if not old_password or not new_password:
            return jsonify({
                'success': False,
                'error': 'Old password and new password are required'
            }), 400
        
        if len(new_password) < 6:
            return jsonify({
                'success': False,
                'error': 'New password must be at least 6 characters long'
            }), 400
        
        # Change password
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        user_id = request.current_user['user_id']
        result = auth_service.change_password(user_id, old_password, new_password)
        
        if result['success']:
            return jsonify({
                'success': True,
                'message': result['message']
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 400
            
    except Exception as e:
        current_app.logger.error(f"Change password error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to change password',
            'message': str(e)
        }), 500

@auth_bp.route('/users', methods=['GET'])
@token_required
@admin_required
def list_users():
    """
    List all users (admin only)
    Query parameters:
    - limit: number of users to return (default: 50, max: 200)
    - offset: number of users to skip (default: 0)
    """
    try:
        limit = min(int(request.args.get('limit', 50)), 200)
        offset = int(request.args.get('offset', 0))
        
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        result = auth_service.list_users(limit, offset)
        
        if result['success']:
            return jsonify({
                'success': True,
                'users': result['users'],
                'total_users': result['total_users'],
                'limit': result['limit'],
                'offset': result['offset']
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 500
            
    except Exception as e:
        current_app.logger.error(f"List users error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to list users',
            'message': str(e)
        }), 500

@auth_bp.route('/users', methods=['POST'])
@token_required
@admin_required
def create_user():
    """
    Create a new user (admin only)
    Expected payload:
    {
        "username": "newuser",
        "password": "password123",
        "email": "user@example.com",
        "full_name": "Full Name",
        "role": "USER" or "ADMIN"
    }
    """
    try:
        data = request.get_json() or {}
        
        username = data.get('username', '').strip()
        password = data.get('password', '')
        email = data.get('email', '').strip()
        full_name = data.get('full_name', '').strip()
        role = data.get('role', 'USER').upper()
        
        # Validate inputs
        if not username or not password or not email:
            return jsonify({
                'success': False,
                'error': 'Username, password, and email are required'
            }), 400
        
        if len(password) < 6:
            return jsonify({
                'success': False,
                'error': 'Password must be at least 6 characters long'
            }), 400
        
        if role not in ['USER', 'ADMIN']:
            role = 'USER'
        
        # Create user
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        result = auth_service.create_user(username, password, email, role, full_name)
        
        if result['success']:
            return jsonify({
                'success': True,
                'message': 'User created successfully',
                'user': result['user']
            }), 201
        else:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 400
            
    except Exception as e:
        current_app.logger.error(f"Create user error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to create user',
            'message': str(e)
        }), 500

@auth_bp.route('/users/<int:user_id>/deactivate', methods=['POST'])
@token_required
@admin_required
def deactivate_user(user_id):
    """Deactivate a user account (admin only)"""
    try:
        # Prevent admin from deactivating themselves
        if user_id == request.current_user['user_id']:
            return jsonify({
                'success': False,
                'error': 'Cannot deactivate your own account'
            }), 400
        
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        result = auth_service.deactivate_user(user_id)
        
        if result['success']:
            return jsonify({
                'success': True,
                'message': result['message']
            }), 200
        else:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 400
            
    except Exception as e:
        current_app.logger.error(f"Deactivate user error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to deactivate user',
            'message': str(e)
        }), 500

@auth_bp.route('/validate-token', methods=['POST'])
def validate_token():
    """
    Validate a JWT token
    Expected payload:
    {
        "token": "jwt_token_here"
    }
    """
    try:
        data = request.get_json() or {}
        token = data.get('token', '')
        
        if not token:
            return jsonify({
                'success': False,
                'error': 'Token is required'
            }), 400
        
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        payload = auth_service.verify_token(token)
        
        if 'error' in payload:
            return jsonify({
                'success': False,
                'error': payload['error']
            }), 401
        else:
            return jsonify({
                'success': True,
                'valid': True,
                'payload': {
                    'user_id': payload['user_id'],
                    'username': payload['username'],
                    'role': payload['role'],
                    'exp': payload['exp']
                }
            }), 200
            
    except Exception as e:
        current_app.logger.error(f"Validate token error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Token validation failed',
            'message': str(e)
        }), 500

@auth_bp.route('/stats', methods=['GET'])
@token_required
@admin_required
def get_auth_stats():
    """Get authentication statistics (admin only)"""
    try:
        auth_service = AuthenticationService()
        auth_service.initialize(current_app.config['SECRET_KEY'])
        
        # Get user statistics
        result = auth_service.list_users(limit=1000)  # Get all users for stats
        
        if not result['success']:
            return jsonify({
                'success': False,
                'error': result['error']
            }), 500
        
        users = result['users']
        total_users = result['total_users']
        active_users = len([u for u in users if u['is_active']])
        admin_users = len([u for u in users if u['role'] == 'ADMIN'])
        
        # Calculate recent activity (users who logged in within last 24 hours)
        from datetime import datetime, timedelta
        yesterday = datetime.utcnow() - timedelta(days=1)
        recent_logins = 0
        
        for user in users:
            if user['last_login']:
                last_login = datetime.fromisoformat(user['last_login'])
                if last_login > yesterday:
                    recent_logins += 1
        
        return jsonify({
            'success': True,
            'statistics': {
                'total_users': total_users,
                'active_users': active_users,
                'inactive_users': total_users - active_users,
                'admin_users': admin_users,
                'regular_users': total_users - admin_users,
                'recent_logins_24h': recent_logins,
                'user_activity_rate': round((recent_logins / total_users) * 100, 1) if total_users > 0 else 0
            },
            'generated_at': datetime.utcnow().isoformat()
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Get auth stats error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get authentication statistics',
            'message': str(e)
        }), 500

