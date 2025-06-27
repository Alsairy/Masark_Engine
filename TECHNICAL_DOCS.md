# Masark Technical Documentation

## Table of Contents
1. [System Architecture](#system-architecture)
2. [API Reference](#api-reference)
3. [Database Schema](#database-schema)
4. [Deployment Guide](#deployment-guide)
5. [Configuration Reference](#configuration-reference)
6. [Troubleshooting](#troubleshooting)
7. [Performance Optimization](#performance-optimization)
8. [Security Guidelines](#security-guidelines)

## System Architecture

### Overview
Masark is built using a modern microservices architecture with the following components:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Admin Panel   │    │   User Portal   │    │  Mobile App     │
│    (React)      │    │    (React)      │    │   (Future)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────────┐
                    │  Nginx Proxy    │
                    │  Load Balancer  │
                    └─────────────────┘
                                 │
                    ┌─────────────────┐
                    │  Flask Backend  │
                    │   (Python)      │
                    └─────────────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         │                       │                       │
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   PostgreSQL    │    │     Redis       │    │  File Storage   │
│   Database      │    │     Cache       │    │   (Reports)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Component Details

#### Frontend Layer
- **Admin Panel**: React-based administrative interface
- **User Portal**: Public-facing assessment interface
- **Mobile App**: Future mobile application support

#### API Gateway
- **Nginx**: Reverse proxy, load balancer, SSL termination
- **Rate Limiting**: API request throttling and protection
- **Static Files**: Efficient serving of reports and assets

#### Application Layer
- **Flask Backend**: RESTful API with modular design
- **Authentication**: JWT-based stateless authentication
- **Localization**: Bilingual support with RTL handling
- **Report Generation**: PDF creation with professional layouts

#### Data Layer
- **PostgreSQL**: Primary database with ACID compliance
- **Redis**: Caching, session storage, and rate limiting
- **File System**: Report storage with organized structure

### Service Communication
- **HTTP/HTTPS**: RESTful API communication
- **WebSocket**: Real-time updates (future enhancement)
- **Message Queue**: Asynchronous processing (future enhancement)

## API Reference

### Authentication Endpoints

#### POST /api/auth/login
Authenticate user and receive JWT token.

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "success": true,
  "token": "jwt_token_string",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@masark.com",
    "full_name": "System Administrator",
    "role": "ADMIN",
    "is_active": true
  }
}
```

#### POST /api/auth/logout
Logout user and invalidate token.

**Headers:**
```
Authorization: Bearer <jwt_token>
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully logged out"
}
```

### Assessment Endpoints

#### POST /api/assessment/start-session
Start a new personality assessment session.

**Request Body:**
```json
{
  "student_name": "Ahmed Al-Rashid",
  "student_email": "ahmed@example.com",
  "language": "ar"
}
```

**Response:**
```json
{
  "success": true,
  "session_id": 123,
  "session_token": "unique_session_token",
  "expires_at": "2024-01-15T10:30:00Z"
}
```

#### GET /api/assessment/questions
Retrieve assessment questions for a session.

**Query Parameters:**
- `session_token`: Session token from start-session
- `language`: Language code (en/ar)

**Response:**
```json
{
  "success": true,
  "questions": [
    {
      "id": 1,
      "text": "You prefer to work alone rather than in groups",
      "option_a": "Strongly Agree",
      "option_b": "Strongly Disagree",
      "dimension_a": "I",
      "dimension_b": "E"
    }
  ],
  "total_questions": 36,
  "session_progress": 0
}
```

#### POST /api/assessment/submit-answer
Submit an answer for a specific question.

**Request Body:**
```json
{
  "session_token": "unique_session_token",
  "question_id": 1,
  "selected_option": "A"
}
```

**Response:**
```json
{
  "success": true,
  "progress": 2.78,
  "questions_answered": 1,
  "questions_remaining": 35
}
```

#### POST /api/assessment/calculate-results
Calculate personality type from completed assessment.

**Request Body:**
```json
{
  "session_token": "unique_session_token"
}
```

**Response:**
```json
{
  "success": true,
  "personality_type": "INTJ",
  "personality_name": "The Strategist",
  "preference_strengths": {
    "I": "clear",
    "N": "moderate", 
    "T": "very_clear",
    "J": "slight"
  },
  "borderline_dimensions": ["J"],
  "description": "Strategic and analytical thinker...",
  "strengths": ["Strategic thinking", "Problem solving"],
  "challenges": ["May be too critical", "Perfectionist tendencies"]
}
```

### Career Matching Endpoints

#### POST /api/careers/match
Get career recommendations based on personality type.

**Request Body:**
```json
{
  "personality_type": "INTJ",
  "deployment_mode": "standard",
  "limit": 10
}
```

**Response:**
```json
{
  "success": true,
  "matches": [
    {
      "career": {
        "id": 1,
        "name": "Software Engineer",
        "description": "Develops software applications...",
        "cluster": "Computer & Technology",
        "ssoc_code": "2512"
      },
      "match_percentage": 95,
      "match_level": "excellent",
      "reasoning": "Strong analytical skills align perfectly..."
    }
  ],
  "total_matches": 22
}
```

#### GET /api/careers/search
Search careers by name or description.

**Query Parameters:**
- `q`: Search query
- `cluster`: Career cluster filter
- `language`: Language code (en/ar)
- `limit`: Number of results (default: 20)

**Response:**
```json
{
  "success": true,
  "careers": [
    {
      "id": 1,
      "name": "Software Engineer",
      "description": "Develops software applications...",
      "cluster": "Computer & Technology",
      "ssoc_code": "2512"
    }
  ],
  "total_results": 5
}
```

### Report Generation Endpoints

#### POST /api/reports/generate
Generate a comprehensive personality and career report.

**Request Body:**
```json
{
  "session_token": "unique_session_token",
  "language": "ar",
  "report_type": "comprehensive"
}
```

**Response:**
```json
{
  "success": true,
  "report_id": "report_20240115_103045_INTJ",
  "filename": "Ahmed_AlRashid_Personality_Report_AR.pdf",
  "file_size": 524288,
  "download_url": "/api/reports/download/Ahmed_AlRashid_Personality_Report_AR.pdf"
}
```

#### GET /api/reports/download/{filename}
Download a generated report file.

**Response:**
- Content-Type: application/pdf
- Content-Disposition: attachment; filename="report.pdf"
- Binary PDF data

### Localization Endpoints

#### GET /api/localization/languages
Get list of supported languages.

**Response:**
```json
{
  "success": true,
  "languages": [
    {
      "code": "en",
      "name": "English",
      "native_name": "English",
      "direction": "ltr",
      "locale": "en-US"
    },
    {
      "code": "ar", 
      "name": "Arabic",
      "native_name": "العربية",
      "direction": "rtl",
      "locale": "ar-SA"
    }
  ]
}
```

#### GET /api/localization/translations
Get translations for a specific category and language.

**Query Parameters:**
- `lang`: Language code (en/ar)
- `category`: Translation category (auth, assessment, careers, etc.)

**Response:**
```json
{
  "success": true,
  "language": "ar",
  "category": "auth",
  "translations": {
    "login": "تسجيل الدخول",
    "password": "كلمة المرور",
    "username": "اسم المستخدم"
  }
}
```

## Database Schema

### Core Tables

#### personality_types
Stores the 16 MBTI personality types with bilingual descriptions.

```sql
CREATE TABLE personality_types (
    id SERIAL PRIMARY KEY,
    code VARCHAR(4) UNIQUE NOT NULL,
    name_en VARCHAR(100) NOT NULL,
    name_ar VARCHAR(100) NOT NULL,
    description_en TEXT,
    description_ar TEXT,
    strengths_en TEXT,
    strengths_ar TEXT,
    challenges_en TEXT,
    challenges_ar TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### questions
Assessment questions with bilingual content and MBTI dimension mapping.

```sql
CREATE TABLE questions (
    id SERIAL PRIMARY KEY,
    text_en TEXT NOT NULL,
    text_ar TEXT NOT NULL,
    option_a_en VARCHAR(200) NOT NULL,
    option_a_ar VARCHAR(200) NOT NULL,
    option_b_en VARCHAR(200) NOT NULL,
    option_b_ar VARCHAR(200) NOT NULL,
    option_a_dimension CHAR(1) NOT NULL,
    option_b_dimension CHAR(1) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### assessment_sessions
User assessment sessions with progress tracking.

```sql
CREATE TABLE assessment_sessions (
    id SERIAL PRIMARY KEY,
    session_token VARCHAR(255) UNIQUE NOT NULL,
    student_name VARCHAR(200),
    student_email VARCHAR(200),
    language VARCHAR(2) DEFAULT 'en',
    status VARCHAR(20) DEFAULT 'active',
    personality_type VARCHAR(4),
    preference_strengths JSONB,
    borderline_dimensions TEXT[],
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP,
    expires_at TIMESTAMP
);
```

#### careers
Career information with cluster relationships.

```sql
CREATE TABLE careers (
    id SERIAL PRIMARY KEY,
    name_en VARCHAR(200) NOT NULL,
    name_ar VARCHAR(200) NOT NULL,
    description_en TEXT,
    description_ar TEXT,
    cluster_id INTEGER REFERENCES career_clusters(id),
    ssoc_code VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Relationship Tables

#### personality_career_matches
Matrix of personality type to career compatibility scores.

```sql
CREATE TABLE personality_career_matches (
    id SERIAL PRIMARY KEY,
    personality_type VARCHAR(4) NOT NULL,
    career_id INTEGER REFERENCES careers(id),
    match_score INTEGER CHECK (match_score >= 0 AND match_score <= 100),
    match_level VARCHAR(20),
    reasoning_en TEXT,
    reasoning_ar TEXT,
    UNIQUE(personality_type, career_id)
);
```

### Indexes for Performance

```sql
-- Assessment session lookups
CREATE INDEX idx_assessment_sessions_token ON assessment_sessions(session_token);
CREATE INDEX idx_assessment_sessions_status ON assessment_sessions(status);

-- Career matching queries
CREATE INDEX idx_personality_career_matches_type ON personality_career_matches(personality_type);
CREATE INDEX idx_personality_career_matches_score ON personality_career_matches(match_score DESC);

-- Career search
CREATE INDEX idx_careers_name_en ON careers USING GIN(to_tsvector('english', name_en));
CREATE INDEX idx_careers_name_ar ON careers USING GIN(to_tsvector('arabic', name_ar));
```

## Deployment Guide

### Prerequisites

#### System Requirements
- **CPU**: 4+ cores (8+ recommended for production)
- **RAM**: 8GB minimum (16GB+ recommended)
- **Storage**: 100GB+ SSD storage
- **Network**: Stable internet connection with static IP

#### Software Requirements
- **Docker**: Version 20.10+
- **Docker Compose**: Version 2.0+
- **Git**: For source code management
- **SSL Certificate**: For HTTPS (Let's Encrypt recommended)

### Production Deployment Steps

#### 1. Server Preparation
```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Reboot to apply changes
sudo reboot
```

#### 2. Application Setup
```bash
# Clone repository
git clone <repository-url> masark
cd masark

# Copy environment configuration
cp .env.example .env

# Edit configuration (IMPORTANT!)
nano .env
```

#### 3. SSL Certificate Setup
```bash
# Option 1: Let's Encrypt (Recommended)
sudo apt install certbot
sudo certbot certonly --standalone -d yourdomain.com
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem ssl/masark.crt
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem ssl/masark.key

# Option 2: Self-signed (Development only)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout ssl/masark.key -out ssl/masark.crt
```

#### 4. Deploy Application
```bash
# Run deployment script
./deploy.sh

# Or manual deployment
docker-compose up -d
```

#### 5. Verify Deployment
```bash
# Check service status
docker-compose ps

# Test API endpoints
curl -k https://localhost/api/system/health
curl -k https://localhost/api/system/info

# View logs
docker-compose logs -f backend
```

### Development Deployment

#### Quick Start
```bash
# Clone and setup
git clone <repository-url> masark-dev
cd masark-dev

# Use development environment
cp .env.example .env.dev
export FLASK_ENV=development

# Start services
docker-compose -f docker-compose.dev.yml up -d

# Access application
open http://localhost:5000/api/system/info
```

### Scaling and Load Balancing

#### Horizontal Scaling
```yaml
# docker-compose.prod.yml
services:
  backend:
    deploy:
      replicas: 3
    environment:
      - FLASK_ENV=production
  
  nginx:
    depends_on:
      - backend
    ports:
      - "80:80"
      - "443:443"
```

#### Database Scaling
```yaml
# Add read replicas
services:
  db-primary:
    image: postgres:15-alpine
    environment:
      POSTGRES_REPLICATION_MODE: master
  
  db-replica:
    image: postgres:15-alpine
    environment:
      POSTGRES_REPLICATION_MODE: slave
      POSTGRES_MASTER_SERVICE: db-primary
```

## Configuration Reference

### Environment Variables

#### Required Variables
```bash
# Database
DATABASE_URL=postgresql://user:pass@host:port/db
REDIS_URL=redis://user:pass@host:port/db

# Security
SECRET_KEY=64-character-random-string
JWT_SECRET_KEY=64-character-random-string

# Application
FLASK_ENV=production|development
```

#### Optional Variables
```bash
# Performance
CACHE_TYPE=redis|simple|null
SQLALCHEMY_POOL_SIZE=10
SQLALCHEMY_MAX_OVERFLOW=20

# Localization
DEFAULT_LANGUAGE=en
SUPPORTED_LANGUAGES=en,ar
TIMEZONE=Asia/Riyadh

# File Storage
UPLOAD_FOLDER=/app/uploads
REPORTS_FOLDER=/app/reports
MAX_CONTENT_LENGTH=16777216

# Monitoring
SENTRY_DSN=your-sentry-dsn
LOG_LEVEL=INFO|DEBUG|WARNING|ERROR
```

### Flask Configuration

#### Production Settings
```python
class ProductionConfig:
    DEBUG = False
    TESTING = False
    SQLALCHEMY_DATABASE_URI = os.environ.get('DATABASE_URL')
    REDIS_URL = os.environ.get('REDIS_URL')
    SECRET_KEY = os.environ.get('SECRET_KEY')
    
    # Security
    SESSION_COOKIE_SECURE = True
    SESSION_COOKIE_HTTPONLY = True
    SESSION_COOKIE_SAMESITE = 'Lax'
    
    # Performance
    SQLALCHEMY_ENGINE_OPTIONS = {
        'pool_pre_ping': True,
        'pool_recycle': 300,
        'pool_size': 10,
        'max_overflow': 20
    }
```

#### Development Settings
```python
class DevelopmentConfig:
    DEBUG = True
    TESTING = False
    SQLALCHEMY_DATABASE_URI = 'sqlite:///masark_dev.db'
    REDIS_URL = 'redis://localhost:6379/0'
    SECRET_KEY = 'dev-secret-key'
    
    # Development helpers
    SQLALCHEMY_ECHO = True
    TEMPLATES_AUTO_RELOAD = True
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check database status
docker-compose exec db pg_isready -U masark_user -d masark

# View database logs
docker-compose logs db

# Reset database connection
docker-compose restart db backend
```

#### Redis Connection Issues
```bash
# Check Redis status
docker-compose exec redis redis-cli ping

# View Redis logs
docker-compose logs redis

# Clear Redis cache
docker-compose exec redis redis-cli FLUSHALL
```

#### SSL Certificate Issues
```bash
# Check certificate validity
openssl x509 -in ssl/masark.crt -text -noout

# Renew Let's Encrypt certificate
sudo certbot renew
sudo systemctl reload nginx
```

#### Performance Issues
```bash
# Monitor resource usage
docker stats

# Check slow queries
docker-compose exec db psql -U masark_user -d masark -c "
SELECT query, mean_time, calls 
FROM pg_stat_statements 
ORDER BY mean_time DESC LIMIT 10;"

# Analyze Redis memory usage
docker-compose exec redis redis-cli --bigkeys
```

### Debugging Commands

#### Application Debugging
```bash
# Enter backend container
docker-compose exec backend bash

# Run Python shell with app context
docker-compose exec backend python -c "
from src.main import app
with app.app_context():
    # Debug commands here
    pass
"

# Check application logs
docker-compose logs -f backend | grep ERROR
```

#### Database Debugging
```bash
# Connect to database
docker-compose exec db psql -U masark_user -d masark

# Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

# Check active connections
SELECT count(*) FROM pg_stat_activity;
```

### Log Analysis

#### Application Logs
```bash
# Real-time log monitoring
docker-compose logs -f backend

# Filter by log level
docker-compose logs backend | grep "ERROR\|CRITICAL"

# Export logs to file
docker-compose logs --no-color backend > masark-backend.log
```

#### System Logs
```bash
# Check Docker daemon logs
journalctl -u docker.service

# Monitor system resources
htop
iotop
nethogs
```

## Performance Optimization

### Database Optimization

#### Query Optimization
```sql
-- Add indexes for common queries
CREATE INDEX CONCURRENTLY idx_assessment_sessions_created 
ON assessment_sessions(created_at DESC);

CREATE INDEX CONCURRENTLY idx_careers_cluster_score 
ON careers(cluster_id) INCLUDE (name_en, name_ar);

-- Analyze query performance
EXPLAIN ANALYZE SELECT * FROM careers 
WHERE cluster_id = 1 
ORDER BY name_en;
```

#### Connection Pooling
```python
# SQLAlchemy configuration
SQLALCHEMY_ENGINE_OPTIONS = {
    'pool_size': 20,
    'max_overflow': 30,
    'pool_pre_ping': True,
    'pool_recycle': 3600
}
```

### Application Optimization

#### Caching Strategy
```python
# Redis caching configuration
CACHE_CONFIG = {
    'CACHE_TYPE': 'redis',
    'CACHE_REDIS_URL': os.environ.get('REDIS_URL'),
    'CACHE_DEFAULT_TIMEOUT': 300
}

# Cache frequently accessed data
@cache.cached(timeout=3600, key_prefix='personality_types')
def get_personality_types():
    return PersonalityType.query.all()
```

#### API Response Optimization
```python
# Implement pagination
@app.route('/api/careers')
def get_careers():
    page = request.args.get('page', 1, type=int)
    per_page = min(request.args.get('per_page', 20, type=int), 100)
    
    careers = Career.query.paginate(
        page=page, per_page=per_page, error_out=False
    )
    
    return jsonify({
        'careers': [career.to_dict() for career in careers.items],
        'pagination': {
            'page': page,
            'per_page': per_page,
            'total': careers.total,
            'pages': careers.pages
        }
    })
```

### Infrastructure Optimization

#### Nginx Configuration
```nginx
# Enable gzip compression
gzip on;
gzip_vary on;
gzip_min_length 1024;
gzip_comp_level 6;

# Enable caching
location ~* \.(css|js|png|jpg|jpeg|gif|ico|svg)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

# Connection optimization
keepalive_timeout 65;
keepalive_requests 100;
```

#### Docker Optimization
```dockerfile
# Multi-stage build for smaller images
FROM python:3.11-slim as builder
WORKDIR /app
COPY requirements.txt .
RUN pip install --user -r requirements.txt

FROM python:3.11-slim
WORKDIR /app
COPY --from=builder /root/.local /root/.local
COPY . .
```

## Security Guidelines

### Authentication Security

#### JWT Configuration
```python
JWT_CONFIG = {
    'JWT_SECRET_KEY': os.environ.get('JWT_SECRET_KEY'),
    'JWT_ACCESS_TOKEN_EXPIRES': timedelta(hours=24),
    'JWT_ALGORITHM': 'HS256',
    'JWT_BLACKLIST_ENABLED': True,
    'JWT_BLACKLIST_TOKEN_CHECKS': ['access', 'refresh']
}
```

#### Password Security
```python
# Strong password requirements
PASSWORD_REQUIREMENTS = {
    'min_length': 12,
    'require_uppercase': True,
    'require_lowercase': True,
    'require_numbers': True,
    'require_special_chars': True
}

# bcrypt configuration
BCRYPT_LOG_ROUNDS = 12
```

### Input Validation

#### Request Validation
```python
from marshmallow import Schema, fields, validate

class AssessmentSessionSchema(Schema):
    student_name = fields.Str(
        required=True,
        validate=validate.Length(min=2, max=200)
    )
    student_email = fields.Email(required=False)
    language = fields.Str(
        validate=validate.OneOf(['en', 'ar']),
        missing='en'
    )
```

#### SQL Injection Prevention
```python
# Always use parameterized queries
def get_career_by_name(name):
    return Career.query.filter(
        Career.name_en.ilike(f'%{name}%')
    ).all()

# Never use string formatting
# BAD: f"SELECT * FROM careers WHERE name = '{name}'"
```

### Infrastructure Security

#### Docker Security
```dockerfile
# Use non-root user
RUN useradd -m -u 1000 masark
USER masark

# Minimize attack surface
FROM python:3.11-slim
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc libpq-dev \
    && rm -rf /var/lib/apt/lists/*
```

#### Network Security
```yaml
# docker-compose.yml
networks:
  masark-network:
    driver: bridge
    internal: true  # Prevent external access
    
services:
  db:
    networks:
      - masark-network
    # No ports exposed externally
```

### Monitoring and Alerting

#### Security Monitoring
```python
# Log security events
@app.after_request
def log_security_events(response):
    if response.status_code in [401, 403, 429]:
        app.logger.warning(
            f'Security event: {response.status_code} '
            f'from {request.remote_addr} '
            f'for {request.endpoint}'
        )
    return response
```

#### Rate Limiting
```python
from flask_limiter import Limiter

limiter = Limiter(
    app,
    key_func=lambda: request.remote_addr,
    default_limits=["1000 per hour"]
)

@app.route('/api/auth/login', methods=['POST'])
@limiter.limit("5 per minute")
def login():
    # Login logic
    pass
```

This comprehensive technical documentation provides detailed information for developers, system administrators, and DevOps engineers working with the Masark system. It covers all aspects from architecture to security, ensuring successful deployment and maintenance of the platform.

