"""
Report API Routes for Masark Personality-Career Matching Engine
Handles PDF report generation, download, and management
"""

from flask import Blueprint, request, jsonify, current_app, send_file
from src.models.masark_models import AssessmentSession
from src.services.report_generation import ReportGenerationService
from datetime import datetime
import os

reports_bp = Blueprint('reports', __name__)

@reports_bp.route('/generate', methods=['POST'])
def generate_report():
    """
    Generate a comprehensive personality and career report
    Expected payload:
    {
        "session_token": "session_token",
        "language": "en" or "ar" (optional, default: "en"),
        "report_type": "comprehensive" or "summary" (optional, default: "comprehensive"),
        "include_career_details": true/false (optional, default: true)
    }
    """
    try:
        data = request.get_json() or {}
        
        session_token = data.get('session_token')
        language = data.get('language', 'en').lower()
        report_type = data.get('report_type', 'comprehensive').lower()
        include_career_details = data.get('include_career_details', True)
        
        # Validate inputs
        if not session_token:
            return jsonify({
                'success': False,
                'error': 'session_token is required'
            }), 400
        
        if language not in ['en', 'ar']:
            language = 'en'
        
        if report_type not in ['comprehensive', 'summary']:
            report_type = 'comprehensive'
        
        # Validate session exists and is completed
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        if not session.is_completed or not session.personality_type_id:
            return jsonify({
                'success': False,
                'error': 'Assessment must be completed before generating report'
            }), 400
        
        # Generate report
        report_service = ReportGenerationService()
        
        if report_type == 'summary':
            file_path, filename = report_service.generate_summary_report(session_token, language)
        else:
            file_path, filename = report_service.generate_comprehensive_report(
                session_token, language, include_career_details
            )
        
        # Get file info
        file_size = os.path.getsize(file_path) if os.path.exists(file_path) else 0
        
        return jsonify({
            'success': True,
            'report': {
                'filename': filename,
                'file_path': file_path,
                'file_size_bytes': file_size,
                'report_type': report_type,
                'language': language,
                'session_token': session_token,
                'personality_type': session.personality_type.code,
                'student_name': session.student_name,
                'generated_at': datetime.utcnow().isoformat()
            }
        })
        
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': 'Validation error',
            'message': str(e)
        }), 400
        
    except Exception as e:
        current_app.logger.error(f"Error generating report: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to generate report',
            'message': str(e)
        }), 500

@reports_bp.route('/download/<filename>', methods=['GET'])
def download_report(filename):
    """
    Download a generated report file
    """
    try:
        # Validate filename
        if not filename.endswith('.pdf') or '..' in filename or '/' in filename:
            return jsonify({
                'success': False,
                'error': 'Invalid filename'
            }), 400
        
        report_service = ReportGenerationService()
        file_path = os.path.join(report_service.reports_dir, filename)
        
        if not os.path.exists(file_path):
            return jsonify({
                'success': False,
                'error': 'Report file not found'
            }), 404
        
        return send_file(
            file_path,
            as_attachment=True,
            download_name=filename,
            mimetype='application/pdf'
        )
        
    except Exception as e:
        current_app.logger.error(f"Error downloading report {filename}: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to download report',
            'message': str(e)
        }), 500

@reports_bp.route('/list', methods=['GET'])
def list_reports():
    """
    List generated reports
    Query parameters:
    - limit: number of reports to return (default: 50, max: 200)
    """
    try:
        limit = min(int(request.args.get('limit', 50)), 200)
        
        report_service = ReportGenerationService()
        reports = report_service.list_generated_reports(limit)
        
        return jsonify({
            'success': True,
            'reports': reports,
            'total_reports': len(reports),
            'limit': limit
        })
        
    except Exception as e:
        current_app.logger.error(f"Error listing reports: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to list reports',
            'message': str(e)
        }), 500

@reports_bp.route('/delete/<filename>', methods=['DELETE'])
def delete_report(filename):
    """
    Delete a generated report file
    """
    try:
        # Validate filename
        if not filename.endswith('.pdf') or '..' in filename or '/' in filename:
            return jsonify({
                'success': False,
                'error': 'Invalid filename'
            }), 400
        
        report_service = ReportGenerationService()
        success = report_service.delete_report(filename)
        
        if success:
            return jsonify({
                'success': True,
                'message': f'Report {filename} deleted successfully'
            })
        else:
            return jsonify({
                'success': False,
                'error': 'Report file not found or could not be deleted'
            }), 404
        
    except Exception as e:
        current_app.logger.error(f"Error deleting report {filename}: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to delete report',
            'message': str(e)
        }), 500

@reports_bp.route('/session/<session_token>', methods=['GET'])
def get_session_reports(session_token):
    """
    Get all reports generated for a specific session
    """
    try:
        # Validate session exists
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        # List all reports and filter by session token
        report_service = ReportGenerationService()
        all_reports = report_service.list_generated_reports(200)
        
        # Filter reports that contain the session token in filename
        session_reports = [
            report for report in all_reports 
            if session_token in report['filename']
        ]
        
        return jsonify({
            'success': True,
            'session_token': session_token,
            'personality_type': session.personality_type.code if session.personality_type else None,
            'student_name': session.student_name,
            'reports': session_reports,
            'total_reports': len(session_reports)
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting session reports for {session_token}: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get session reports',
            'message': str(e)
        }), 500

@reports_bp.route('/stats', methods=['GET'])
def get_report_stats():
    """Get report generation statistics"""
    try:
        report_service = ReportGenerationService()
        all_reports = report_service.list_generated_reports(1000)
        
        # Calculate statistics
        total_reports = len(all_reports)
        total_size = sum(report['size_bytes'] for report in all_reports)
        
        # Group by date
        reports_by_date = {}
        for report in all_reports:
            date = report['created_at'][:10]  # Extract date part
            reports_by_date[date] = reports_by_date.get(date, 0) + 1
        
        # Recent activity (last 7 days)
        from datetime import datetime, timedelta
        week_ago = datetime.now() - timedelta(days=7)
        recent_reports = [
            report for report in all_reports
            if datetime.fromisoformat(report['created_at']) > week_ago
        ]
        
        return jsonify({
            'success': True,
            'statistics': {
                'total_reports': total_reports,
                'total_size_bytes': total_size,
                'total_size_mb': round(total_size / (1024 * 1024), 2),
                'reports_last_7_days': len(recent_reports),
                'reports_by_date': reports_by_date,
                'average_report_size_kb': round((total_size / total_reports) / 1024, 2) if total_reports > 0 else 0
            },
            'generated_at': datetime.utcnow().isoformat()
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting report stats: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get report statistics',
            'message': str(e)
        }), 500

