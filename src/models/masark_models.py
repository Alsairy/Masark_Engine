from flask_sqlalchemy import SQLAlchemy
from datetime import datetime
from enum import Enum

db = SQLAlchemy()

# Enums for better type safety
class PersonalityDimension(Enum):
    EI = "E-I"  # Extraversion vs Introversion
    SN = "S-N"  # Sensing vs Intuition
    TF = "T-F"  # Thinking vs Feeling
    JP = "J-P"  # Judging vs Perceiving

class PathwaySource(Enum):
    MOE = "MOE"
    MAWHIBA = "MAWHIBA"

class DeploymentMode(Enum):
    STANDARD = "STANDARD"
    MAWHIBA = "MAWHIBA"

class PreferenceStrength(Enum):
    SLIGHT = "SLIGHT"      # <60%
    MODERATE = "MODERATE"  # 60-75%
    CLEAR = "CLEAR"        # 76-90%
    VERY_CLEAR = "VERY_CLEAR"  # >90%

# Core Assessment Models
class Question(db.Model):
    __tablename__ = 'questions'
    
    id = db.Column(db.Integer, primary_key=True)
    order_number = db.Column(db.Integer, nullable=False, unique=True)
    dimension = db.Column(db.Enum(PersonalityDimension), nullable=False)
    
    # Question text in both languages
    text_en = db.Column(db.Text, nullable=False)
    text_ar = db.Column(db.Text, nullable=False)
    
    # Option A
    option_a_text_en = db.Column(db.Text, nullable=False)
    option_a_text_ar = db.Column(db.Text, nullable=False)
    option_a_maps_to_first = db.Column(db.Boolean, nullable=False)  # True if A maps to first letter of dimension (E, S, T, J)
    
    # Option B
    option_b_text_en = db.Column(db.Text, nullable=False)
    option_b_text_ar = db.Column(db.Text, nullable=False)
    
    # Metadata
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    is_active = db.Column(db.Boolean, default=True)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'order_number': self.order_number,
            'dimension': self.dimension.value,
            'text': self.text_en if language == 'en' else self.text_ar,
            'option_a': self.option_a_text_en if language == 'en' else self.option_a_text_ar,
            'option_b': self.option_b_text_en if language == 'en' else self.option_b_text_ar,
            'option_a_maps_to_first': self.option_a_maps_to_first
        }

class PersonalityType(db.Model):
    __tablename__ = 'personality_types'
    
    id = db.Column(db.Integer, primary_key=True)
    code = db.Column(db.String(4), unique=True, nullable=False)  # e.g., "INTJ"
    
    # Names and descriptions in both languages
    name_en = db.Column(db.String(100))  # e.g., "The Strategist"
    name_ar = db.Column(db.String(100))
    
    description_en = db.Column(db.Text)
    description_ar = db.Column(db.Text)
    
    strengths_en = db.Column(db.Text)
    strengths_ar = db.Column(db.Text)
    
    challenges_en = db.Column(db.Text)
    challenges_ar = db.Column(db.Text)
    
    # Metadata
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'code': self.code,
            'name': self.name_en if language == 'en' else self.name_ar,
            'description': self.description_en if language == 'en' else self.description_ar,
            'strengths': self.strengths_en if language == 'en' else self.strengths_ar,
            'challenges': self.challenges_en if language == 'en' else self.challenges_ar
        }

# Career-related Models
class CareerCluster(db.Model):
    __tablename__ = 'career_clusters'
    
    id = db.Column(db.Integer, primary_key=True)
    name_en = db.Column(db.String(100), nullable=False)
    name_ar = db.Column(db.String(100), nullable=False)
    description_en = db.Column(db.Text)
    description_ar = db.Column(db.Text)
    
    # Relationships
    careers = db.relationship('Career', backref='cluster', lazy=True)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'name': self.name_en if language == 'en' else self.name_ar,
            'description': self.description_en if language == 'en' else self.description_ar
        }

class Program(db.Model):
    __tablename__ = 'programs'
    
    id = db.Column(db.Integer, primary_key=True)
    name_en = db.Column(db.String(200), nullable=False)
    name_ar = db.Column(db.String(200), nullable=False)
    description_en = db.Column(db.Text)
    description_ar = db.Column(db.Text)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'name': self.name_en if language == 'en' else self.name_ar,
            'description': self.description_en if language == 'en' else self.description_ar
        }

class Pathway(db.Model):
    __tablename__ = 'pathways'
    
    id = db.Column(db.Integer, primary_key=True)
    name_en = db.Column(db.String(200), nullable=False)
    name_ar = db.Column(db.String(200), nullable=False)
    source = db.Column(db.Enum(PathwaySource), nullable=False)
    description_en = db.Column(db.Text)
    description_ar = db.Column(db.Text)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'name': self.name_en if language == 'en' else self.name_ar,
            'source': self.source.value,
            'description': self.description_en if language == 'en' else self.description_ar
        }

class Career(db.Model):
    __tablename__ = 'careers'
    
    id = db.Column(db.Integer, primary_key=True)
    name_en = db.Column(db.String(200), nullable=False)
    name_ar = db.Column(db.String(200), nullable=False)
    description_en = db.Column(db.Text)
    description_ar = db.Column(db.Text)
    ssoc_code = db.Column(db.String(20))  # Saudi Standard Classification of Occupations code
    
    # Foreign keys
    cluster_id = db.Column(db.Integer, db.ForeignKey('career_clusters.id'), nullable=False)
    
    # Metadata
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    is_active = db.Column(db.Boolean, default=True)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'name': self.name_en if language == 'en' else self.name_ar,
            'description': self.description_en if language == 'en' else self.description_ar,
            'ssoc_code': self.ssoc_code,
            'cluster': self.cluster.to_dict(language) if self.cluster else None
        }

# Association Tables for Many-to-Many relationships
class CareerProgram(db.Model):
    __tablename__ = 'career_programs'
    
    id = db.Column(db.Integer, primary_key=True)
    career_id = db.Column(db.Integer, db.ForeignKey('careers.id'), nullable=False)
    program_id = db.Column(db.Integer, db.ForeignKey('programs.id'), nullable=False)
    
    # Relationships
    career = db.relationship('Career', backref='career_programs')
    program = db.relationship('Program', backref='program_careers')
    
    # Unique constraint
    __table_args__ = (db.UniqueConstraint('career_id', 'program_id'),)

class CareerPathway(db.Model):
    __tablename__ = 'career_pathways'
    
    id = db.Column(db.Integer, primary_key=True)
    career_id = db.Column(db.Integer, db.ForeignKey('careers.id'), nullable=False)
    pathway_id = db.Column(db.Integer, db.ForeignKey('pathways.id'), nullable=False)
    
    # Relationships
    career = db.relationship('Career', backref='career_pathways')
    pathway = db.relationship('Pathway', backref='pathway_careers')
    
    # Unique constraint
    __table_args__ = (db.UniqueConstraint('career_id', 'pathway_id'),)

# Personality-Career Match Score Matrix
class PersonalityCareerMatch(db.Model):
    __tablename__ = 'personality_career_matches'
    
    id = db.Column(db.Integer, primary_key=True)
    personality_type_id = db.Column(db.Integer, db.ForeignKey('personality_types.id'), nullable=False)
    career_id = db.Column(db.Integer, db.ForeignKey('careers.id'), nullable=False)
    match_score = db.Column(db.Float, nullable=False)  # 0.00 to 1.00
    
    # Relationships
    personality_type = db.relationship('PersonalityType', backref='career_matches')
    career = db.relationship('Career', backref='personality_matches')
    
    # Metadata
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Unique constraint and validation
    __table_args__ = (
        db.UniqueConstraint('personality_type_id', 'career_id'),
        db.CheckConstraint('match_score >= 0.0 AND match_score <= 1.0')
    )

# Assessment Session Models
class AssessmentSession(db.Model):
    __tablename__ = 'assessment_sessions'
    
    id = db.Column(db.Integer, primary_key=True)
    session_token = db.Column(db.String(100), unique=True, nullable=False)
    
    # Student information (optional)
    student_name = db.Column(db.String(200))
    student_email = db.Column(db.String(200))
    student_id = db.Column(db.String(50))
    
    # Assessment results
    personality_type_id = db.Column(db.Integer, db.ForeignKey('personality_types.id'))
    personality_type = db.relationship('PersonalityType', backref='sessions')
    
    # Preference strengths (percentages)
    e_strength = db.Column(db.Float)  # Extraversion strength (0-1)
    s_strength = db.Column(db.Float)  # Sensing strength (0-1)
    t_strength = db.Column(db.Float)  # Thinking strength (0-1)
    j_strength = db.Column(db.Float)  # Judging strength (0-1)
    
    # Preference clarity categories
    ei_clarity = db.Column(db.Enum(PreferenceStrength))
    sn_clarity = db.Column(db.Enum(PreferenceStrength))
    tf_clarity = db.Column(db.Enum(PreferenceStrength))
    jp_clarity = db.Column(db.Enum(PreferenceStrength))
    
    # Session metadata
    deployment_mode = db.Column(db.Enum(DeploymentMode), default=DeploymentMode.STANDARD)
    language_preference = db.Column(db.String(2), default='en')  # 'en' or 'ar'
    ip_address = db.Column(db.String(45))
    user_agent = db.Column(db.Text)
    
    # Timestamps
    started_at = db.Column(db.DateTime, default=datetime.utcnow)
    completed_at = db.Column(db.DateTime)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    # Status
    is_completed = db.Column(db.Boolean, default=False)
    
    def to_dict(self, language='en'):
        return {
            'id': self.id,
            'session_token': self.session_token,
            'student_name': self.student_name,
            'personality_type': self.personality_type.to_dict(language) if self.personality_type else None,
            'preference_strengths': {
                'e_strength': self.e_strength,
                's_strength': self.s_strength,
                't_strength': self.t_strength,
                'j_strength': self.j_strength
            },
            'preference_clarity': {
                'ei_clarity': self.ei_clarity.value if self.ei_clarity else None,
                'sn_clarity': self.sn_clarity.value if self.sn_clarity else None,
                'tf_clarity': self.tf_clarity.value if self.tf_clarity else None,
                'jp_clarity': self.jp_clarity.value if self.jp_clarity else None
            },
            'deployment_mode': self.deployment_mode.value,
            'language_preference': self.language_preference,
            'started_at': self.started_at.isoformat() if self.started_at else None,
            'completed_at': self.completed_at.isoformat() if self.completed_at else None,
            'is_completed': self.is_completed
        }

class AssessmentAnswer(db.Model):
    __tablename__ = 'assessment_answers'
    
    id = db.Column(db.Integer, primary_key=True)
    session_id = db.Column(db.Integer, db.ForeignKey('assessment_sessions.id'), nullable=False)
    question_id = db.Column(db.Integer, db.ForeignKey('questions.id'), nullable=False)
    selected_option = db.Column(db.String(1), nullable=False)  # 'A' or 'B'
    answered_at = db.Column(db.DateTime, default=datetime.utcnow)
    
    # Relationships
    session = db.relationship('AssessmentSession', backref='answers')
    question = db.relationship('Question', backref='answers')
    
    # Unique constraint
    __table_args__ = (db.UniqueConstraint('session_id', 'question_id'),)

# Admin and User Management
class AdminUser(db.Model):
    __tablename__ = 'admin_users'
    
    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(80), unique=True, nullable=False)
    email = db.Column(db.String(120), unique=True, nullable=False)
    password_hash = db.Column(db.String(255), nullable=False)
    
    # Role and permissions
    role = db.Column(db.String(50), default='admin')
    is_active = db.Column(db.Boolean, default=True)
    is_super_admin = db.Column(db.Boolean, default=False)
    
    # Profile information
    first_name = db.Column(db.String(100))
    last_name = db.Column(db.String(100))
    
    # Timestamps
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    last_login_at = db.Column(db.DateTime)
    
    def to_dict(self):
        return {
            'id': self.id,
            'username': self.username,
            'email': self.email,
            'role': self.role,
            'is_active': self.is_active,
            'is_super_admin': self.is_super_admin,
            'first_name': self.first_name,
            'last_name': self.last_name,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'last_login_at': self.last_login_at.isoformat() if self.last_login_at else None
        }

# Configuration and Settings
class SystemConfiguration(db.Model):
    __tablename__ = 'system_configurations'
    
    id = db.Column(db.Integer, primary_key=True)
    key = db.Column(db.String(100), unique=True, nullable=False)
    value = db.Column(db.Text)
    description = db.Column(db.Text)
    deployment_mode = db.Column(db.Enum(DeploymentMode))
    
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

# Audit and Logging
class AuditLog(db.Model):
    __tablename__ = 'audit_logs'
    
    id = db.Column(db.Integer, primary_key=True)
    admin_user_id = db.Column(db.Integer, db.ForeignKey('admin_users.id'))
    action = db.Column(db.String(100), nullable=False)
    entity_type = db.Column(db.String(50))  # e.g., 'Question', 'Career', etc.
    entity_id = db.Column(db.Integer)
    old_values = db.Column(db.Text)  # JSON string of old values
    new_values = db.Column(db.Text)  # JSON string of new values
    ip_address = db.Column(db.String(45))
    user_agent = db.Column(db.Text)
    timestamp = db.Column(db.DateTime, default=datetime.utcnow)
    
    # Relationships
    admin_user = db.relationship('AdminUser', backref='audit_logs')

# Legacy User model (keeping for compatibility)
class User(db.Model):
    __tablename__ = 'users'
    
    id = db.Column(db.Integer, primary_key=True)
    username = db.Column(db.String(80), unique=True, nullable=False)
    email = db.Column(db.String(120), unique=True, nullable=False)
    password_hash = db.Column(db.String(255), nullable=False)
    full_name = db.Column(db.String(200))
    role = db.Column(db.String(20), nullable=False, default='USER')  # USER, ADMIN
    is_active = db.Column(db.Boolean, nullable=False, default=True)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    last_login = db.Column(db.DateTime)

    def __repr__(self):
        return f'<User {self.username}>'

    def to_dict(self):
        return {
            'id': self.id,
            'username': self.username,
            'email': self.email,
            'full_name': self.full_name,
            'role': self.role,
            'is_active': self.is_active,
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'last_login': self.last_login.isoformat() if self.last_login else None
        }