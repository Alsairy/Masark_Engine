#!/usr/bin/env python3
"""
Production-Ready Masark Engine Main Application
Integrates all production services: caching, rate limiting, security, monitoring
"""

import sys
import os

# Add the src directory to the path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

import logging
from datetime import datetime
from flask import Flask, request, jsonify, g, current_app
from flask_cors import CORS
from functools import wraps

# Import all services
from services.caching_service import cache_service
from services.rate_limiting import rate_limiter
from services.security_service import security_service
from services.performance_monitoring import performance_monitor
from services.enhanced_personality_scoring import EnhancedPersonalityScoringService
from services.enhanced_assessment_validation import EnhancedAssessmentValidationService

# Import existing routes
from routes.assessment import assessment_bp
from routes.careers import careers_bp
from routes.reports import reports_bp
from routes.system import system_bp
from routes.auth import auth_bp
from routes.localization import localization_bp

# Import models
from models.masark_models import db

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('logs/masark_production.log'),
        logging.StreamHandler()
    ]
)

logger = logging.getLogger(__name__)

def create_production_app():
    """Create production-ready Flask application"""
    app = Flask(__name__)
    
    # Production configuration
    import secrets
    secret_key = os.environ.get('SECRET_KEY')
    if not secret_key:
        secret_key = secrets.token_urlsafe(32)
        print("⚠️  CRITICAL: Generated temporary SECRET_KEY. Set SECRET_KEY environment variable for production!")
        print("⚠️  Application may not work correctly without a persistent SECRET_KEY!")
    
    app.config.update({
        'SECRET_KEY': secret_key,
        'SQLALCHEMY_DATABASE_URI': os.environ.get('DATABASE_URL', 'sqlite:///src/database/masark.db'),
        'SQLALCHEMY_TRACK_MODIFICATIONS': False,
        'SQLALCHEMY_ENGINE_OPTIONS': {
            'pool_pre_ping': True,
            'pool_recycle': 300,
            'connect_args': {'check_same_thread': False}
        },
        'MAX_CONTENT_LENGTH': 16 * 1024 * 1024,  # 16MB max file size
        'UPLOAD_FOLDER': 'uploads',
        'CORS_ORIGINS': '*',  # Configure appropriately for production
        'RATE_LIMITING_ENABLED': True,
        'CACHING_ENABLED': True,
        'SECURITY_MONITORING_ENABLED': True,
        'PERFORMANCE_MONITORING_ENABLED': True
    })
    
    # Initialize extensions
    db.init_app(app)
    CORS(app, origins=app.config['CORS_ORIGINS'])
    
    # Create upload directory
    os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
    os.makedirs('logs', exist_ok=True)
    
    # Initialize services
    with app.app_context():
        # Warm up cache
        cache_service.warm_cache()
        
        # Initialize enhanced services
        app.enhanced_scoring = EnhancedPersonalityScoringService()
        app.enhanced_validation = EnhancedAssessmentValidationService()
    
    # Register middleware
    register_middleware(app)
    
    # Register blueprints
    register_blueprints(app)
    
    # Register error handlers
    register_error_handlers(app)
    
    # Register production routes
    register_production_routes(app)
    
    logger.info("Production Masark Engine application created successfully")
    return app

def register_middleware(app):
    """Register production middleware"""
    
    @app.before_request
    def before_request():
        """Execute before each request"""
        g.request_start_time = datetime.now()
        g.client_ip = request.environ.get('HTTP_X_FORWARDED_FOR', request.remote_addr)
        
        # Performance monitoring
        if app.config['PERFORMANCE_MONITORING_ENABLED']:
            performance_monitor.record_metric(
                'request_started', 1, 'api',
                {'endpoint': request.endpoint, 'method': request.method}
            )
    
    @app.after_request
    def after_request(response):
        """Execute after each request"""
        if hasattr(g, 'request_start_time'):
            duration = (datetime.now() - g.request_start_time).total_seconds()
            
            # Performance monitoring
            if app.config['PERFORMANCE_MONITORING_ENABLED']:
                performance_monitor.record_api_request(
                    request.endpoint or 'unknown',
                    request.method,
                    duration,
                    response.status_code
                )
        
        # Add security headers
        response.headers['X-Content-Type-Options'] = 'nosniff'
        response.headers['X-Frame-Options'] = 'DENY'
        response.headers['X-XSS-Protection'] = '1; mode=block'
        response.headers['Strict-Transport-Security'] = 'max-age=31536000; includeSubDomains'
        
        return response

def register_blueprints(app):
    """Register all blueprints"""
    app.register_blueprint(assessment_bp, url_prefix='/api/assessment')
    app.register_blueprint(careers_bp, url_prefix='/api/careers')
    app.register_blueprint(reports_bp, url_prefix='/api/reports')
    app.register_blueprint(system_bp, url_prefix='/api/system')
    app.register_blueprint(auth_bp, url_prefix='/api/auth')
    app.register_blueprint(localization_bp, url_prefix='/api/localization')

def register_error_handlers(app):
    """Register production error handlers"""
    
    @app.errorhandler(400)
    def bad_request(error):
        return jsonify({
            'error': 'Bad Request',
            'message': 'The request could not be understood by the server',
            'status_code': 400
        }), 400
    
    @app.errorhandler(401)
    def unauthorized(error):
        return jsonify({
            'error': 'Unauthorized',
            'message': 'Authentication required',
            'status_code': 401
        }), 401
    
    @app.errorhandler(403)
    def forbidden(error):
        return jsonify({
            'error': 'Forbidden',
            'message': 'Access denied',
            'status_code': 403
        }), 403
    
    @app.errorhandler(404)
    def not_found(error):
        return jsonify({
            'error': 'Not Found',
            'message': 'The requested resource was not found',
            'status_code': 404
        }), 404
    
    @app.errorhandler(429)
    def rate_limit_exceeded(error):
        return jsonify({
            'error': 'Rate Limit Exceeded',
            'message': 'Too many requests. Please try again later.',
            'status_code': 429
        }), 429
    
    @app.errorhandler(500)
    def internal_error(error):
        logger.error(f"Internal server error: {str(error)}")
        return jsonify({
            'error': 'Internal Server Error',
            'message': 'An unexpected error occurred',
            'status_code': 500
        }), 500

def register_production_routes(app):
    """Register production-specific routes"""
    
    @app.route('/api/admin/dashboard')
    @require_authentication
    @require_rate_limit('admin_operations')
    def admin_dashboard():
        """Admin dashboard with system metrics"""
        try:
            dashboard_data = {
                'system_health': performance_monitor.get_current_system_health().__dict__,
                'performance_metrics': performance_monitor.get_performance_dashboard_data(),
                'cache_statistics': cache_service.get_cache_statistics(),
                'rate_limiting_stats': rate_limiter.get_statistics(),
                'security_dashboard': security_service.get_security_dashboard(),
                'timestamp': datetime.now().isoformat()
            }
            
            return jsonify({
                'success': True,
                'data': dashboard_data
            })
            
        except Exception as e:
            logger.error(f"Admin dashboard error: {str(e)}")
            return jsonify({
                'success': False,
                'error': 'Failed to load dashboard data'
            }), 500
    
    @app.route('/api/admin/performance/report')
    @require_authentication
    @require_rate_limit('admin_operations')
    def performance_report():
        """Get detailed performance report"""
        try:
            hours = request.args.get('hours', 24, type=int)
            report = performance_monitor.get_performance_report(hours)
            
            return jsonify({
                'success': True,
                'data': {
                    'period_start': report.period_start.isoformat(),
                    'period_end': report.period_end.isoformat(),
                    'total_assessments': report.total_assessments,
                    'avg_completion_time': report.avg_completion_time,
                    'success_rate': report.success_rate,
                    'peak_concurrent_users': report.peak_concurrent_users,
                    'system_health': report.system_health.__dict__,
                    'bottlenecks': report.bottlenecks,
                    'recommendations': report.recommendations
                }
            })
            
        except Exception as e:
            logger.error(f"Performance report error: {str(e)}")
            return jsonify({
                'success': False,
                'error': 'Failed to generate performance report'
            }), 500
    
    @app.route('/api/admin/cache/invalidate', methods=['POST'])
    @require_authentication
    @require_rate_limit('admin_operations')
    def invalidate_cache():
        """Invalidate cache"""
        try:
            cache_type = request.json.get('cache_type')
            cache_service.invalidate_cache(cache_type)
            
            return jsonify({
                'success': True,
                'message': f'Cache invalidated: {cache_type or "all"}'
            })
            
        except Exception as e:
            logger.error(f"Cache invalidation error: {str(e)}")
            return jsonify({
                'success': False,
                'error': 'Failed to invalidate cache'
            }), 500
    
    @app.route('/api/admin/security/events')
    @require_authentication
    @require_rate_limit('admin_operations')
    def security_events():
        """Get security events"""
        try:
            hours = request.args.get('hours', 24, type=int)
            severity = request.args.get('severity')
            
            events = security_service.get_security_events(hours, severity)
            
            return jsonify({
                'success': True,
                'data': events
            })
            
        except Exception as e:
            logger.error(f"Security events error: {str(e)}")
            return jsonify({
                'success': False,
                'error': 'Failed to retrieve security events'
            }), 500
    
    @app.route('/api/health/detailed')
    def detailed_health_check():
        """Detailed health check for monitoring systems"""
        try:
            health_data = {
                'status': 'healthy',
                'timestamp': datetime.now().isoformat(),
                'version': '1.0.0',
                'services': {
                    'database': check_database_health(),
                    'cache': check_cache_health(),
                    'rate_limiter': check_rate_limiter_health(),
                    'security': check_security_health()
                },
                'metrics': {
                    'active_sessions': len(security_service.active_sessions),
                    'cache_hit_rate': get_overall_cache_hit_rate(),
                    'requests_per_minute': get_requests_per_minute()
                }
            }
            
            # Determine overall status
            service_statuses = [service['status'] for service in health_data['services'].values()]
            if 'critical' in service_statuses:
                health_data['status'] = 'critical'
            elif 'warning' in service_statuses:
                health_data['status'] = 'warning'
            
            status_code = 200 if health_data['status'] == 'healthy' else 503
            
            return jsonify(health_data), status_code
            
        except Exception as e:
            logger.error(f"Health check error: {str(e)}")
            return jsonify({
                'status': 'critical',
                'error': str(e),
                'timestamp': datetime.now().isoformat()
            }), 503

def require_authentication(f):
    """Decorator to require authentication"""
    @wraps(f)
    def decorated_function(*args, **kwargs):
        auth_header = request.headers.get('Authorization')
        if not auth_header or not auth_header.startswith('Bearer '):
            return jsonify({'error': 'Authentication required'}), 401
        
        token = auth_header.split(' ')[1]
        is_valid, user_id = security_service.validate_session(token)
        
        if not is_valid:
            return jsonify({'error': 'Invalid or expired token'}), 401
        
        g.current_user_id = user_id
        return f(*args, **kwargs)
    
    return decorated_function

def require_rate_limit(endpoint):
    """Decorator to enforce rate limiting"""
    def decorator(f):
        @wraps(f)
        def decorated_function(*args, **kwargs):
            if not current_app.config['RATE_LIMITING_ENABLED']:
                return f(*args, **kwargs)
            
            client_id = g.get('client_ip', 'unknown')
            status = rate_limiter.check_rate_limit(client_id, endpoint)
            
            if not status.allowed:
                response = jsonify({
                    'error': 'Rate limit exceeded',
                    'retry_after_seconds': status.retry_after_seconds
                })
                response.headers['Retry-After'] = str(status.retry_after_seconds)
                return response, 429
            
            return f(*args, **kwargs)
        
        return decorated_function
    return decorator

def check_database_health():
    """Check database health"""
    try:
        # Simple database query
        db.session.execute('SELECT 1')
        return {'status': 'healthy', 'message': 'Database connection OK'}
    except Exception as e:
        return {'status': 'critical', 'message': f'Database error: {str(e)}'}

def check_cache_health():
    """Check cache health"""
    try:
        stats = cache_service.get_cache_statistics()
        if stats['cache_warmed']:
            return {'status': 'healthy', 'message': 'Cache operational'}
        else:
            return {'status': 'warning', 'message': 'Cache not warmed'}
    except Exception as e:
        return {'status': 'critical', 'message': f'Cache error: {str(e)}'}

def check_rate_limiter_health():
    """Check rate limiter health"""
    try:
        stats = rate_limiter.get_statistics()
        return {'status': 'healthy', 'message': 'Rate limiter operational', 'stats': stats}
    except Exception as e:
        return {'status': 'critical', 'message': f'Rate limiter error: {str(e)}'}

def check_security_health():
    """Check security service health"""
    try:
        dashboard = security_service.get_security_dashboard()
        alerts = dashboard.get('security_alerts', [])
        
        critical_alerts = [alert for alert in alerts if alert.get('severity') == 'critical']
        if critical_alerts:
            return {'status': 'critical', 'message': f'{len(critical_alerts)} critical security alerts'}
        
        high_alerts = [alert for alert in alerts if alert.get('severity') == 'high']
        if high_alerts:
            return {'status': 'warning', 'message': f'{len(high_alerts)} high-priority security alerts'}
        
        return {'status': 'healthy', 'message': 'Security monitoring operational'}
    except Exception as e:
        return {'status': 'critical', 'message': f'Security service error: {str(e)}'}

def get_overall_cache_hit_rate():
    """Get overall cache hit rate"""
    try:
        stats = cache_service.get_cache_statistics()
        total_hits = sum(cache_stats.get('hits', 0) for cache_stats in stats.values() if isinstance(cache_stats, dict))
        total_requests = sum(
            cache_stats.get('hits', 0) + cache_stats.get('misses', 0) 
            for cache_stats in stats.values() if isinstance(cache_stats, dict)
        )
        return total_hits / total_requests if total_requests > 0 else 0
    except:
        return 0

def get_requests_per_minute():
    """Get requests per minute"""
    try:
        metrics = performance_monitor.get_real_time_metrics(1)  # Last minute
        api_metrics = metrics.get('categories', {}).get('api', {})
        return api_metrics.get('api_request', {}).get('count', 0)
    except:
        return 0

# Create the production application
app = create_production_app()

if __name__ == '__main__':
    # Production server configuration
    port = int(os.environ.get('PORT', 5000))
    debug = os.environ.get('FLASK_ENV') == 'development'
    
    logger.info(f"Starting Masark Engine in {'development' if debug else 'production'} mode on port {port}")
    
    # In production, use a proper WSGI server like Gunicorn
    app.run(host='0.0.0.0', port=port, debug=debug, threaded=True)

