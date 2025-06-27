"""
Assessment API Routes for Masark Personality-Career Matching Engine
Handles assessment sessions, question retrieval, answer submission, and results
"""

from flask import Blueprint, request, jsonify, current_app
from src.models.masark_models import (
    db, AssessmentSession, Question, AssessmentAnswer, PersonalityType,
    DeploymentMode, PersonalityDimension
)
from src.services.personality_scoring import PersonalityScoringService
import uuid
from datetime import datetime
import json

assessment_bp = Blueprint('assessment', __name__)

@assessment_bp.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'service': 'Masark Assessment Engine',
        'timestamp': datetime.utcnow().isoformat()
    })

@assessment_bp.route('/start-session', methods=['POST'])
def start_assessment_session():
    """
    Start a new assessment session
    Expected payload:
    {
        "student_name": "Optional student name",
        "student_email": "Optional student email", 
        "student_id": "Optional student ID",
        "deployment_mode": "STANDARD" or "MAWHIBA",
        "language_preference": "en" or "ar"
    }
    """
    try:
        data = request.get_json() or {}
        
        # Generate unique session token
        session_token = str(uuid.uuid4())
        
        # Get client info
        ip_address = request.remote_addr
        user_agent = request.headers.get('User-Agent', '')
        
        # Validate deployment mode
        deployment_mode = data.get('deployment_mode', 'STANDARD').upper()
        if deployment_mode not in ['STANDARD', 'MAWHIBA']:
            deployment_mode = 'STANDARD'
        
        # Validate language preference
        language_preference = data.get('language_preference', 'en').lower()
        if language_preference not in ['en', 'ar']:
            language_preference = 'en'
        
        # Create new session
        session = AssessmentSession(
            session_token=session_token,
            student_name=data.get('student_name'),
            student_email=data.get('student_email'),
            student_id=data.get('student_id'),
            deployment_mode=DeploymentMode(deployment_mode),
            language_preference=language_preference,
            ip_address=ip_address,
            user_agent=user_agent,
            started_at=datetime.utcnow()
        )
        
        db.session.add(session)
        db.session.commit()
        
        return jsonify({
            'success': True,
            'session_token': session_token,
            'session_id': session.id,
            'deployment_mode': deployment_mode,
            'language_preference': language_preference,
            'message': 'Assessment session started successfully'
        }), 201
        
    except Exception as e:
        db.session.rollback()
        current_app.logger.error(f"Error starting assessment session: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to start assessment session',
            'message': str(e)
        }), 500

@assessment_bp.route('/questions', methods=['GET'])
def get_assessment_questions():
    """
    Get all assessment questions
    Query parameters:
    - language: 'en' or 'ar' (default: 'en')
    - session_token: required for session validation
    """
    try:
        session_token = request.args.get('session_token')
        language = request.args.get('language', 'en').lower()
        
        if not session_token:
            return jsonify({
                'success': False,
                'error': 'Session token is required'
            }), 400
        
        # Validate session
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        if session.is_completed:
            return jsonify({
                'success': False,
                'error': 'Assessment already completed'
            }), 400
        
        # Validate language
        if language not in ['en', 'ar']:
            language = 'en'
        
        # Get all active questions ordered by order_number
        questions = Question.query.filter_by(is_active=True).order_by(Question.order_number).all()
        
        # Format questions for response
        questions_data = []
        for question in questions:
            questions_data.append({
                'id': question.id,
                'order_number': question.order_number,
                'dimension': question.dimension.value,
                'text': question.text_en if language == 'en' else question.text_ar,
                'options': {
                    'A': question.option_a_text_en if language == 'en' else question.option_a_text_ar,
                    'B': question.option_b_text_en if language == 'en' else question.option_b_text_ar
                }
            })
        
        return jsonify({
            'success': True,
            'questions': questions_data,
            'total_questions': len(questions_data),
            'language': language,
            'session_token': session_token
        })
        
    except Exception as e:
        current_app.logger.error(f"Error retrieving questions: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to retrieve questions',
            'message': str(e)
        }), 500

@assessment_bp.route('/submit-answer', methods=['POST'])
def submit_answer():
    """
    Submit an answer for a specific question
    Expected payload:
    {
        "session_token": "session_token",
        "question_id": 1,
        "selected_option": "A" or "B"
    }
    """
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                'success': False,
                'error': 'Request body is required'
            }), 400
        
        session_token = data.get('session_token')
        question_id = data.get('question_id')
        selected_option = data.get('selected_option')
        
        # Validate required fields
        if not all([session_token, question_id, selected_option]):
            return jsonify({
                'success': False,
                'error': 'session_token, question_id, and selected_option are required'
            }), 400
        
        # Validate selected_option
        if selected_option not in ['A', 'B']:
            return jsonify({
                'success': False,
                'error': 'selected_option must be either "A" or "B"'
            }), 400
        
        # Validate session
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        if session.is_completed:
            return jsonify({
                'success': False,
                'error': 'Assessment already completed'
            }), 400
        
        # Validate question
        question = Question.query.filter_by(id=question_id, is_active=True).first()
        if not question:
            return jsonify({
                'success': False,
                'error': 'Invalid question ID'
            }), 404
        
        # Check if answer already exists (update if it does)
        existing_answer = AssessmentAnswer.query.filter_by(
            session_id=session.id,
            question_id=question_id
        ).first()
        
        if existing_answer:
            existing_answer.selected_option = selected_option
            existing_answer.answered_at = datetime.utcnow()
        else:
            # Create new answer
            answer = AssessmentAnswer(
                session_id=session.id,
                question_id=question_id,
                selected_option=selected_option,
                answered_at=datetime.utcnow()
            )
            db.session.add(answer)
        
        db.session.commit()
        
        # Check if all questions are answered
        total_questions = Question.query.filter_by(is_active=True).count()
        answered_questions = AssessmentAnswer.query.filter_by(session_id=session.id).count()
        
        return jsonify({
            'success': True,
            'message': 'Answer submitted successfully',
            'progress': {
                'answered': answered_questions,
                'total': total_questions,
                'percentage': round((answered_questions / total_questions) * 100, 2)
            },
            'is_complete': answered_questions >= total_questions
        })
        
    except Exception as e:
        db.session.rollback()
        current_app.logger.error(f"Error submitting answer: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to submit answer',
            'message': str(e)
        }), 500

@assessment_bp.route('/submit-assessment', methods=['POST'])
def submit_complete_assessment():
    """
    Submit all answers at once and complete the assessment
    Expected payload:
    {
        "session_token": "session_token",
        "answers": [
            {"question_id": 1, "selected_option": "A"},
            {"question_id": 2, "selected_option": "B"},
            ...
        ]
    }
    """
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                'success': False,
                'error': 'Request body is required'
            }), 400
        
        session_token = data.get('session_token')
        answers = data.get('answers', [])
        
        if not session_token:
            return jsonify({
                'success': False,
                'error': 'session_token is required'
            }), 400
        
        if not answers or not isinstance(answers, list):
            return jsonify({
                'success': False,
                'error': 'answers array is required'
            }), 400
        
        # Validate session
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        if session.is_completed:
            return jsonify({
                'success': False,
                'error': 'Assessment already completed'
            }), 400
        
        # Validate all answers
        for answer_data in answers:
            if not all(key in answer_data for key in ['question_id', 'selected_option']):
                return jsonify({
                    'success': False,
                    'error': 'Each answer must have question_id and selected_option'
                }), 400
            
            if answer_data['selected_option'] not in ['A', 'B']:
                return jsonify({
                    'success': False,
                    'error': 'selected_option must be either "A" or "B"'
                }), 400
        
        # Check if we have all 36 questions answered
        if len(answers) != 36:
            return jsonify({
                'success': False,
                'error': 'All 36 questions must be answered'
            }), 400
        
        # Clear existing answers for this session
        AssessmentAnswer.query.filter_by(session_id=session.id).delete()
        
        # Save all answers
        for answer_data in answers:
            # Validate question exists
            question = Question.query.filter_by(
                id=answer_data['question_id'],
                is_active=True
            ).first()
            
            if not question:
                return jsonify({
                    'success': False,
                    'error': f'Invalid question ID: {answer_data["question_id"]}'
                }), 404
            
            answer = AssessmentAnswer(
                session_id=session.id,
                question_id=answer_data['question_id'],
                selected_option=answer_data['selected_option'],
                answered_at=datetime.utcnow()
            )
            db.session.add(answer)
        
        # Mark session as completed
        session.completed_at = datetime.utcnow()
        session.is_completed = True
        
        db.session.commit()
        
        return jsonify({
            'success': True,
            'message': 'Assessment completed successfully',
            'session_token': session_token,
            'completed_at': session.completed_at.isoformat(),
            'next_step': 'Call /api/assessment/calculate-results to get personality type and career matches'
        })
        
    except Exception as e:
        db.session.rollback()
        current_app.logger.error(f"Error submitting complete assessment: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to submit assessment',
            'message': str(e)
        }), 500

@assessment_bp.route('/session-status/<session_token>', methods=['GET'])
def get_session_status(session_token):
    """Get the status of an assessment session"""
    try:
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        
        if not session:
            return jsonify({
                'success': False,
                'error': 'Session not found'
            }), 404
        
        # Get answer progress
        total_questions = Question.query.filter_by(is_active=True).count()
        answered_questions = AssessmentAnswer.query.filter_by(session_id=session.id).count()
        
        return jsonify({
            'success': True,
            'session': session.to_dict(session.language_preference),
            'progress': {
                'answered': answered_questions,
                'total': total_questions,
                'percentage': round((answered_questions / total_questions) * 100, 2)
            },
            'is_complete': session.is_completed
        })
        
    except Exception as e:
        current_app.logger.error(f"Error getting session status: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get session status',
            'message': str(e)
        }), 500

@assessment_bp.route('/sessions', methods=['GET'])
def list_sessions():
    """List recent assessment sessions (for admin/monitoring)"""
    try:
        # Get query parameters
        page = request.args.get('page', 1, type=int)
        per_page = min(request.args.get('per_page', 20, type=int), 100)
        
        # Query sessions with pagination
        sessions = AssessmentSession.query.order_by(
            AssessmentSession.created_at.desc()
        ).paginate(
            page=page,
            per_page=per_page,
            error_out=False
        )
        
        sessions_data = []
        for session in sessions.items:
            session_dict = session.to_dict(session.language_preference)
            # Add progress info
            answered_questions = AssessmentAnswer.query.filter_by(session_id=session.id).count()
            total_questions = Question.query.filter_by(is_active=True).count()
            session_dict['progress'] = {
                'answered': answered_questions,
                'total': total_questions,
                'percentage': round((answered_questions / total_questions) * 100, 2) if total_questions > 0 else 0
            }
            sessions_data.append(session_dict)
        
        return jsonify({
            'success': True,
            'sessions': sessions_data,
            'pagination': {
                'page': page,
                'per_page': per_page,
                'total': sessions.total,
                'pages': sessions.pages,
                'has_next': sessions.has_next,
                'has_prev': sessions.has_prev
            }
        })
        
    except Exception as e:
        current_app.logger.error(f"Error listing sessions: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to list sessions',
            'message': str(e)
        }), 500



@assessment_bp.route('/calculate-results', methods=['POST'])
def calculate_personality_results():
    """
    Calculate personality type and results for a completed assessment
    Expected payload:
    {
        "session_token": "session_token"
    }
    """
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                'success': False,
                'error': 'Request body is required'
            }), 400
        
        session_token = data.get('session_token')
        
        if not session_token:
            return jsonify({
                'success': False,
                'error': 'session_token is required'
            }), 400
        
        # Validate session
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Invalid session token'
            }), 404
        
        if not session.is_completed:
            return jsonify({
                'success': False,
                'error': 'Assessment must be completed before calculating results'
            }), 400
        
        # Initialize scoring service
        scoring_service = PersonalityScoringService()
        
        # Validate answers completeness
        is_valid, validation_message = scoring_service.validate_answers_completeness(session.id)
        if not is_valid:
            return jsonify({
                'success': False,
                'error': 'Invalid or incomplete answers',
                'message': validation_message
            }), 400
        
        # Calculate personality type
        result = scoring_service.calculate_personality_type(session.id)
        
        # Get personality type description
        personality_description = scoring_service.get_personality_description(
            result.type_code, 
            session.language_preference
        )
        
        # Format response
        response_data = {
            'success': True,
            'session_token': session_token,
            'results': {
                'personality_type': {
                    'code': result.type_code,
                    'name': result.personality_type,
                    'description': personality_description
                },
                'dimension_scores': {
                    'extraversion': result.dimension_scores.e_score,
                    'introversion': result.dimension_scores.i_score,
                    'sensing': result.dimension_scores.s_score,
                    'intuition': result.dimension_scores.n_score,
                    'thinking': result.dimension_scores.t_score,
                    'feeling': result.dimension_scores.f_score,
                    'judging': result.dimension_scores.j_score,
                    'perceiving': result.dimension_scores.p_score
                },
                'preference_strengths': {
                    'E': round(result.preference_strengths.get('E', 0) * 100, 1),
                    'I': round(result.preference_strengths.get('I', 0) * 100, 1),
                    'S': round(result.preference_strengths.get('S', 0) * 100, 1),
                    'N': round(result.preference_strengths.get('N', 0) * 100, 1),
                    'T': round(result.preference_strengths.get('T', 0) * 100, 1),
                    'F': round(result.preference_strengths.get('F', 0) * 100, 1),
                    'J': round(result.preference_strengths.get('J', 0) * 100, 1),
                    'P': round(result.preference_strengths.get('P', 0) * 100, 1)
                },
                'preference_clarity': {
                    'EI': (ei_clarity := result.preference_clarity.get('EI')) and ei_clarity.value,
                    'SN': (sn_clarity := result.preference_clarity.get('SN')) and sn_clarity.value,
                    'TF': (tf_clarity := result.preference_clarity.get('TF')) and tf_clarity.value,
                    'JP': (jp_clarity := result.preference_clarity.get('JP')) and jp_clarity.value
                },
                'borderline_dimensions': result.borderline_dimensions,
                'questions_per_dimension': result.total_questions_per_dimension
            },
            'calculated_at': datetime.utcnow().isoformat(),
            'next_step': 'Call /api/careers/match to get career recommendations'
        }
        
        return jsonify(response_data)
        
    except ValueError as e:
        return jsonify({
            'success': False,
            'error': 'Validation error',
            'message': str(e)
        }), 400
        
    except Exception as e:
        current_app.logger.error(f"Error calculating personality results: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to calculate personality results',
            'message': str(e)
        }), 500

@assessment_bp.route('/results/<session_token>', methods=['GET'])
def get_assessment_results(session_token):
    """Get previously calculated assessment results"""
    try:
        # Validate session
        session = AssessmentSession.query.filter_by(session_token=session_token).first()
        if not session:
            return jsonify({
                'success': False,
                'error': 'Session not found'
            }), 404
        
        if not session.is_completed:
            return jsonify({
                'success': False,
                'error': 'Assessment not completed'
            }), 400
        
        if not session.personality_type_id:
            return jsonify({
                'success': False,
                'error': 'Results not calculated yet. Call /calculate-results first.'
            }), 400
        
        # Get language preference
        language = request.args.get('language', session.language_preference)
        if language not in ['en', 'ar']:
            language = session.language_preference
        
        # Get personality type description
        scoring_service = PersonalityScoringService()
        personality_description = scoring_service.get_personality_description(
            session.personality_type.code, 
            language
        )
        
        # Format response
        response_data = {
            'success': True,
            'session_token': session_token,
            'results': {
                'personality_type': {
                    'code': session.personality_type.code,
                    'name': personality_description.get('name') if personality_description else session.personality_type.code,
                    'description': personality_description
                },
                'preference_strengths': {
                    'E': round(session.e_strength * 100, 1) if session.e_strength else 0,
                    'I': round((1 - session.e_strength) * 100, 1) if session.e_strength else 0,
                    'S': round(session.s_strength * 100, 1) if session.s_strength else 0,
                    'N': round((1 - session.s_strength) * 100, 1) if session.s_strength else 0,
                    'T': round(session.t_strength * 100, 1) if session.t_strength else 0,
                    'F': round((1 - session.t_strength) * 100, 1) if session.t_strength else 0,
                    'J': round(session.j_strength * 100, 1) if session.j_strength else 0,
                    'P': round((1 - session.j_strength) * 100, 1) if session.j_strength else 0
                },
                'preference_clarity': {
                    'EI': session.ei_clarity.value if session.ei_clarity else None,
                    'SN': session.sn_clarity.value if session.sn_clarity else None,
                    'TF': session.tf_clarity.value if session.tf_clarity else None,
                    'JP': session.jp_clarity.value if session.jp_clarity else None
                }
            },
            'language': language,
            'completed_at': session.completed_at.isoformat() if session.completed_at else None
        }
        
        return jsonify(response_data)
        
    except Exception as e:
        current_app.logger.error(f"Error getting assessment results: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get assessment results',
            'message': str(e)
        }), 500

