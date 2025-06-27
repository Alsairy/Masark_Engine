"""
Career Matching API Routes for Masark Personality-Career Matching Engine
Handles career recommendations, search, and detailed career information
"""

from flask import Blueprint, request, jsonify, current_app
from src.models.masark_models import (
    db, AssessmentSession, PersonalityType, Career, CareerCluster,
    DeploymentMode
)
from src.services.career_matching import CareerMatchingService
from datetime import datetime

careers_bp = Blueprint('careers', __name__)

@careers_bp.route('/match', methods=['POST'])
def get_career_matches():
    """
    Get career matches for a personality type
    Expected payload:
    {
        "session_token": "session_token",
        "personality_type": "INTJ" (optional, will be retrieved from session if not provided),
        "deployment_mode": "STANDARD" or "MAWHIBA" (optional),
        "language": "en" or "ar" (optional),
        "limit": 10 (optional)
    }
    """
    try:
        data = request.get_json() or {}
        
        session_token = data.get('session_token')
        personality_type_code = data.get('personality_type')
        deployment_mode = data.get('deployment_mode', 'STANDARD').upper()
        language = data.get('language', 'en').lower()
        limit = min(data.get('limit', 10), 50)  # Cap at 50
        
        # Validate inputs
        if deployment_mode not in ['STANDARD', 'MAWHIBA']:
            deployment_mode = 'STANDARD'
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        # If personality type not provided, get from session
        if not personality_type_code and session_token:
            session = AssessmentSession.query.filter_by(session_token=session_token).first()
            if not session:
                return jsonify({
                    'success': False,
                    'error': 'Invalid session token'
                }), 404
            
            if not session.personality_type_id:
                return jsonify({
                    'success': False,
                    'error': 'Personality type not calculated yet. Complete assessment first.'
                }), 400
            
            personality_type_code = session.personality_type.code
            # Use session's deployment mode and language if not specified
            if 'deployment_mode' not in data:
                deployment_mode = session.deployment_mode.value
            if 'language' not in data:
                language = session.language_preference
        
        if not personality_type_code:
            return jsonify({
                'success': False,
                'error': 'personality_type or session_token is required'
            }), 400
        
        # Validate personality type
        if len(personality_type_code) != 4 or not personality_type_code.isalpha():
            return jsonify({
                'success': False,
                'error': 'Invalid personality type format. Expected 4-letter code like INTJ'
            }), 400
        
        # Get career matches
        matching_service = CareerMatchingService()
        result = matching_service.get_career_matches(
            personality_type_code=personality_type_code.upper(),
            deployment_mode=DeploymentMode(deployment_mode),
            language=language,
            limit=limit
        )
        
        # Format response
        matches_data = []
        for match in result.top_matches:
            match_data = {
                'career_id': match.career_id,
                'name': match.career_name_en if language == 'en' else match.career_name_ar,
                'description': match.description_en if language == 'en' else match.description_ar,
                'match_score': round(match.match_score * 100, 1),  # Convert to percentage
                'cluster': {
                    'name': match.cluster_name_en if language == 'en' else match.cluster_name_ar
                },
                'ssoc_code': match.ssoc_code,
                'programs': match.programs,
                'pathways': match.pathways
            }
            matches_data.append(match_data)
        
        return jsonify({
            'success': True,
            'personality_type': result.personality_type,
            'deployment_mode': result.deployment_mode,
            'language': result.language,
            'total_matches': result.total_careers,
            'matches': matches_data,
            'cached': result.cached,
            'generated_at': datetime.utcnow().isoformat()
        })
        
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': 'Validation error',
            'message': str(e)
        }), 400
        
    except Exception as e:
        current_app.logger.error(f"Error getting career matches: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get career matches',
            'message': str(e)
        }), 500

@careers_bp.route('/search', methods=['GET'])
def search_careers():
    """
    Search careers by name or description
    Query parameters:
    - q: search query
    - language: 'en' or 'ar' (default: 'en')
    - limit: number of results (default: 20, max: 100)
    """
    try:
        query = request.args.get('q', '').strip()
        language = request.args.get('language', 'en').lower()
        limit = min(int(request.args.get('limit', 20)), 100)
        
        if not query:
            return jsonify({
                'success': False,
                'error': 'Search query (q) is required'
            }), 400
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        # Search careers
        matching_service = CareerMatchingService()
        results = matching_service.search_careers(query, language, limit)
        
        return jsonify({
            'success': True,
            'query': query,
            'language': language,
            'total_results': len(results),
            'careers': results
        })
        
    except Exception as e:
        current_app.logger.error(f"Error searching careers: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to search careers',
            'message': str(e)
        }), 500

@careers_bp.route('/<int:career_id>', methods=['GET'])
def get_career_details(career_id):
    """
    Get detailed information about a specific career
    Query parameters:
    - language: 'en' or 'ar' (default: 'en')
    """
    try:
        language = request.args.get('language', 'en').lower()
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        # Get career details
        matching_service = CareerMatchingService()
        career_details = matching_service.get_career_details(career_id, language)
        
        if not career_details:
            return jsonify({
                'success': False,
                'error': 'Career not found'
            }), 404
        
        return jsonify({
            'success': True,
            'career': career_details,
            'language': language
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting career details for {career_id}: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get career details',
            'message': str(e)
        }), 500

@careers_bp.route('/clusters/<int:cluster_id>/careers', methods=['GET'])
def get_careers_by_cluster(cluster_id):
    """
    Get all careers in a specific cluster
    Query parameters:
    - language: 'en' or 'ar' (default: 'en')
    """
    try:
        language = request.args.get('language', 'en').lower()
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        # Get cluster info
        cluster = CareerCluster.query.get(cluster_id)
        if not cluster:
            return jsonify({
                'success': False,
                'error': 'Career cluster not found'
            }), 404
        
        # Get careers in cluster
        matching_service = CareerMatchingService()
        careers = matching_service.get_careers_by_cluster(cluster_id, language)
        
        return jsonify({
            'success': True,
            'cluster': {
                'id': cluster.id,
                'name': cluster.name_en if language == 'en' else cluster.name_ar,
                'description': cluster.description_en if language == 'en' else cluster.description_ar
            },
            'careers': careers,
            'total_careers': len(careers),
            'language': language
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting careers for cluster {cluster_id}: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get careers for cluster',
            'message': str(e)
        }), 500

@careers_bp.route('/clusters', methods=['GET'])
def get_all_clusters():
    """
    Get all career clusters
    Query parameters:
    - language: 'en' or 'ar' (default: 'en')
    """
    try:
        language = request.args.get('language', 'en').lower()
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        clusters = CareerCluster.query.all()
        
        clusters_data = []
        for cluster in clusters:
            # Count careers in cluster
            career_count = Career.query.filter_by(cluster_id=cluster.id, is_active=True).count()
            
            clusters_data.append({
                'id': cluster.id,
                'name': cluster.name_en if language == 'en' else cluster.name_ar,
                'description': cluster.description_en if language == 'en' else cluster.description_ar,
                'career_count': career_count
            })
        
        return jsonify({
            'success': True,
            'clusters': clusters_data,
            'total_clusters': len(clusters_data),
            'language': language
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting career clusters: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get career clusters',
            'message': str(e)
        }), 500

@careers_bp.route('/stats', methods=['GET'])
def get_career_stats():
    """Get career matching statistics"""
    try:
        # Get basic statistics
        total_careers = Career.query.filter_by(is_active=True).count()
        total_clusters = CareerCluster.query.count()
        
        # Get careers per cluster
        cluster_stats = []
        clusters = CareerCluster.query.all()
        for cluster in clusters:
            career_count = Career.query.filter_by(cluster_id=cluster.id, is_active=True).count()
            cluster_stats.append({
                'cluster_id': cluster.id,
                'cluster_name': cluster.name_en,
                'career_count': career_count
            })
        
        # Get cache statistics
        matching_service = CareerMatchingService()
        cache_stats = matching_service.get_cache_stats()
        
        return jsonify({
            'success': True,
            'statistics': {
                'total_careers': total_careers,
                'total_clusters': total_clusters,
                'cluster_breakdown': cluster_stats,
                'cache_stats': cache_stats
            },
            'generated_at': datetime.utcnow().isoformat()
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting career stats: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get career statistics',
            'message': str(e)
        }), 500

