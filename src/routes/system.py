"""
System API Routes for Masark Personality-Career Matching Engine
Handles system information, API documentation, and configuration
"""

from flask import Blueprint, jsonify, current_app, request
from src.models.masark_models import (
    db, PersonalityType, CareerCluster, Question, Pathway,
    SystemConfiguration, AssessmentSession
)
from datetime import datetime

system_bp = Blueprint('system', __name__)

@system_bp.route('/info', methods=['GET'])
def get_system_info():
    """Get system information and API overview"""
    try:
        # Get database statistics
        stats = {
            'personality_types': PersonalityType.query.count(),
            'career_clusters': CareerCluster.query.count(),
            'questions': Question.query.filter_by(is_active=True).count(),
            'pathways': Pathway.query.count(),
            'total_sessions': AssessmentSession.query.count(),
            'completed_sessions': AssessmentSession.query.filter_by(is_completed=True).count()
        }
        
        return jsonify({
            'success': True,
            'system': {
                'name': 'Masark Mawhiba Personality-Career Matching Engine',
                'version': '1.0.0',
                'description': 'World-class personality assessment and career matching system',
                'features': [
                    '36-question MBTI-style personality assessment',
                    'Career matching for 261+ careers',
                    'Bilingual support (Arabic/English)',
                    'Saudi education pathway mapping',
                    'Scalable architecture for 500K+ concurrent users'
                ],
                'deployment_modes': ['STANDARD', 'MAWHIBA'],
                'supported_languages': ['en', 'ar']
            },
            'statistics': stats,
            'api_endpoints': {
                'assessment': {
                    'start_session': 'POST /api/assessment/start-session',
                    'get_questions': 'GET /api/assessment/questions',
                    'submit_answer': 'POST /api/assessment/submit-answer',
                    'submit_assessment': 'POST /api/assessment/submit-assessment',
                    'session_status': 'GET /api/assessment/session-status/{token}',
                    'list_sessions': 'GET /api/assessment/sessions'
                },
                'system': {
                    'info': 'GET /api/system/info',
                    'health': 'GET /api/system/health',
                    'config': 'GET /api/system/config'
                }
            },
            'timestamp': datetime.utcnow().isoformat()
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting system info: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get system information',
            'message': str(e)
        }), 500

@system_bp.route('/health', methods=['GET'])
def health_check():
    """Comprehensive health check endpoint"""
    try:
        # Check database connectivity
        db_healthy = True
        try:
            db.session.execute('SELECT 1')
        except Exception:
            db_healthy = False
        
        # Check critical data
        personality_types_count = PersonalityType.query.count()
        questions_count = Question.query.filter_by(is_active=True).count()
        
        health_status = {
            'database': 'healthy' if db_healthy else 'unhealthy',
            'personality_types': 'healthy' if personality_types_count == 16 else 'warning',
            'questions': 'healthy' if questions_count == 36 else 'warning'
        }
        
        overall_status = 'healthy'
        if not db_healthy or 'unhealthy' in health_status.values():
            overall_status = 'unhealthy'
        elif 'warning' in health_status.values():
            overall_status = 'warning'
        
        status_code = 200 if overall_status == 'healthy' else 503
        
        return jsonify({
            'status': overall_status,
            'timestamp': datetime.utcnow().isoformat(),
            'checks': health_status,
            'details': {
                'personality_types_count': personality_types_count,
                'questions_count': questions_count,
                'database_connected': db_healthy
            }
        }), status_code
        
    except Exception as e:
        current_app.logger.error(f"Error in health check: {str(e)}")
        return jsonify({
            'status': 'unhealthy',
            'timestamp': datetime.utcnow().isoformat(),
            'error': str(e)
        }), 503

@system_bp.route('/config', methods=['GET'])
def get_system_config():
    """Get public system configuration"""
    try:
        # Get public configurations (non-sensitive)
        configs = SystemConfiguration.query.all()
        
        public_configs = {}
        for config in configs:
            # Only expose non-sensitive configuration keys
            if not any(sensitive in config.key.lower() for sensitive in ['password', 'secret', 'key', 'token']):
                public_configs[config.key] = {
                    'value': config.value,
                    'description': config.description,
                    'deployment_mode': config.deployment_mode.value if config.deployment_mode else None
                }
        
        return jsonify({
            'success': True,
            'configurations': public_configs,
            'timestamp': datetime.utcnow().isoformat()
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting system config: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get system configuration',
            'message': str(e)
        }), 500

@system_bp.route('/personality-types', methods=['GET'])
def get_personality_types():
    """Get all personality types with descriptions"""
    try:
        language = request.args.get('language', 'en').lower()
        if language not in ['en', 'ar']:
            language = 'en'
        
        personality_types = PersonalityType.query.all()
        
        types_data = []
        for pt in personality_types:
            types_data.append(pt.to_dict(language))
        
        return jsonify({
            'success': True,
            'personality_types': types_data,
            'total_count': len(types_data),
            'language': language
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting personality types: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get personality types',
            'message': str(e)
        }), 500

@system_bp.route('/career-clusters', methods=['GET'])
def get_career_clusters():
    """Get all career clusters"""
    try:
        language = request.args.get('language', 'en').lower()
        if language not in ['en', 'ar']:
            language = 'en'
        
        clusters = CareerCluster.query.all()
        
        clusters_data = []
        for cluster in clusters:
            clusters_data.append(cluster.to_dict(language))
        
        return jsonify({
            'success': True,
            'career_clusters': clusters_data,
            'total_count': len(clusters_data),
            'language': language
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting career clusters: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get career clusters',
            'message': str(e)
        }), 500

@system_bp.route('/pathways', methods=['GET'])
def get_pathways():
    """Get all education pathways"""
    try:
        language = request.args.get('language', 'en').lower()
        source = request.args.get('source')  # Optional filter by MOE or MAWHIBA
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        query = Pathway.query
        if source and source.upper() in ['MOE', 'MAWHIBA']:
            from src.models.masark_models import PathwaySource
            query = query.filter_by(source=PathwaySource(source.upper()))
        
        pathways = query.all()
        
        pathways_data = []
        for pathway in pathways:
            pathways_data.append(pathway.to_dict(language))
        
        return jsonify({
            'success': True,
            'pathways': pathways_data,
            'total_count': len(pathways_data),
            'language': language,
            'filtered_by_source': source.upper() if source else None
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting pathways: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get pathways',
            'message': str(e)
        }), 500

