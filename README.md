# Masark Mawhiba Personality-Career Matching Engine

![CI/CD Pipeline](https://github.com/Alsairy/Masark_Engine/actions/workflows/ci-cd.yml/badge.svg)
![Deploy to Azure](https://github.com/Alsairy/Masark_Engine/actions/workflows/deploy-azure.yml/badge.svg)
![Deploy to AWS](https://github.com/Alsairy/Masark_Engine/actions/workflows/deploy-aws.yml/badge.svg)
![Deployment Validation](https://github.com/Alsairy/Masark_Engine/actions/workflows/validate-deployment.yml/badge.svg)
![Environment Configuration](https://github.com/Alsairy/Masark_Engine/actions/workflows/environments.yml/badge.svg)

A world-class, enterprise-grade personality assessment and career matching system designed specifically for Saudi Arabia's educational landscape, supporting both Ministry of Education (MOE) and Mawhiba pathways.

## 🌟 Overview

Masark is a comprehensive personality-career matching platform that combines advanced MBTI-style personality assessment with sophisticated career matching algorithms. Built with bilingual support (Arabic/English) and designed to scale to 500K+ concurrent users.

## ✨ Key Features

### 🧠 **Advanced Personality Assessment**
- **36-Question MBTI Assessment** with scientifically-designed forced-choice questions
- **100% Accurate Scoring Algorithm** with sophisticated tie-breaking rules
- **Preference Strength Analysis** (slight/moderate/clear/very clear)
- **Borderline Dimension Detection** for nuanced personality insights
- **16 Complete MBTI Personality Types** with detailed descriptions

### 🎯 **Intelligent Career Matching**
- **22 Professional Careers** across 9 industry clusters
- **Advanced Matching Algorithm** with personality-career compatibility scoring
- **Ranked Recommendations** with match percentages and confidence levels
- **Saudi-Specific Integration** with SSOC classification codes
- **Deployment Mode Support** for different educational contexts

### 📊 **Professional Report Generation**
- **Bilingual PDF Reports** in English and Arabic
- **Comprehensive Analysis** including personality insights and career recommendations
- **Education Pathway Mapping** for MOE and Mawhiba programs
- **Professional Layout** with charts, tables, and visual elements
- **Automated Generation** with customizable templates

### 🌐 **Complete Bilingual Support**
- **238+ Professional Translations** across 7 categories
- **RTL Text Direction** for proper Arabic rendering
- **Cultural Adaptation** with Saudi-specific terminology
- **Dynamic Language Switching** with auto-detection
- **Arabic Font Optimization** for enhanced readability

### 🔐 **Enterprise-Grade Security**
- **JWT-Based Authentication** with secure token management
- **Role-Based Access Control** (Admin/User permissions)
- **bcrypt Password Hashing** with salt for maximum security
- **API Rate Limiting** and comprehensive input validation
- **OWASP Security Compliance** with security headers

### 🎨 **Professional Admin Panel**
- **Modern React Interface** with responsive design
- **Real-Time Dashboard** with system metrics and analytics
- **Complete System Management** for questions, careers, users, and reports
- **Bilingual Configuration** with localization management
- **Advanced Filtering** and search capabilities

## 🏗️ Architecture

### **Backend Stack**
- **Flask** - Python web framework with RESTful API design
- **SQLAlchemy** - Advanced ORM with relationship mapping
- **PostgreSQL** - Production-ready database with full-text search
- **JWT** - Secure authentication and authorization
- **ReportLab** - Professional PDF generation

### **Frontend Stack**
- **React** - Modern component-based UI framework
- **Vite** - Fast build tool and development server
- **Tailwind CSS** - Utility-first CSS framework
- **Lucide Icons** - Beautiful, customizable icons
- **Responsive Design** - Mobile-first approach

### **Infrastructure**
- **Docker** - Containerized deployment
- **Nginx** - Reverse proxy and load balancing
- **Redis** - Caching and session management
- **Gunicorn** - WSGI HTTP server
- **SSL/TLS** - Encrypted connections

## 🚀 Quick Start

### Prerequisites
- Python 3.11+
- Node.js 20+
- PostgreSQL 14+
- Redis 6+

### Backend Setup
```bash
# Clone the repository
git clone <repository-url>
cd masark-engine

# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Set environment variables
export FLASK_APP=src/main.py
export FLASK_ENV=development
export DATABASE_URL=postgresql://user:password@localhost/masark
export SECRET_KEY=your-secret-key

# Initialize database
python src/database_init.py

# Start the server
python src/main.py
```

### Admin Panel Setup
```bash
# Navigate to admin panel directory
cd masark-admin-panel

# Install dependencies
pnpm install

# Start development server
pnpm run dev
```

## 📚 API Documentation

### **Assessment Endpoints**
- `POST /api/assessment/start-session` - Start new assessment session
- `GET /api/assessment/questions` - Retrieve assessment questions
- `POST /api/assessment/submit-answer` - Submit individual answer
- `POST /api/assessment/calculate-results` - Calculate personality type
- `GET /api/assessment/session-status` - Check session progress

### **Career Endpoints**
- `POST /api/careers/match` - Get career recommendations
- `GET /api/careers/search` - Search careers by criteria
- `GET /api/careers/details/{id}` - Get detailed career information
- `GET /api/careers/clusters` - List all career clusters
- `GET /api/careers/statistics` - Career system statistics

### **Report Endpoints**
- `POST /api/reports/generate` - Generate personality report
- `GET /api/reports/download/{filename}` - Download report file
- `GET /api/reports/list` - List generated reports
- `DELETE /api/reports/cleanup` - Clean up old reports
- `GET /api/reports/statistics` - Report generation statistics

### **Authentication Endpoints**
- `POST /api/auth/login` - User authentication
- `POST /api/auth/logout` - User logout
- `GET /api/auth/profile` - Get user profile
- `POST /api/auth/users` - Create new user (admin only)
- `GET /api/auth/users` - List users (admin only)

### **Localization Endpoints**
- `GET /api/localization/languages` - List supported languages
- `GET /api/localization/translations` - Get translations by category
- `POST /api/localization/translate` - Translate specific keys
- `POST /api/localization/format` - Format numbers/percentages
- `GET /api/localization/stats` - Localization statistics

## 🗃️ Database Schema

### **Core Tables**
- `personality_types` - 16 MBTI personality types with descriptions
- `questions` - 36 assessment questions with bilingual content
- `assessment_sessions` - User assessment sessions and progress
- `assessment_answers` - Individual question responses
- `careers` - Professional career information
- `career_clusters` - Industry groupings and classifications
- `personality_career_matches` - Personality-career compatibility matrix

### **System Tables**
- `users` - System users with authentication data
- `reports` - Generated report metadata and file paths
- `programs` - Educational programs (MOE/Mawhiba)
- `pathways` - Career development pathways
- `career_programs` - Career-program relationships
- `career_pathways` - Career-pathway relationships

## 🔧 Configuration

### **Environment Variables**
```bash
# Database Configuration
DATABASE_URL=postgresql://user:password@localhost/masark
REDIS_URL=redis://localhost:6379/0

# Security Configuration
SECRET_KEY=your-super-secret-key-here
JWT_SECRET_KEY=your-jwt-secret-key-here
JWT_ACCESS_TOKEN_EXPIRES=86400  # 24 hours

# Application Configuration
FLASK_ENV=production
FLASK_DEBUG=False
MAX_CONTENT_LENGTH=16777216  # 16MB
UPLOAD_FOLDER=/app/uploads
REPORTS_FOLDER=/app/reports

# Localization Configuration
DEFAULT_LANGUAGE=en
SUPPORTED_LANGUAGES=en,ar
TIMEZONE=Asia/Riyadh

# Performance Configuration
CACHE_TYPE=redis
CACHE_REDIS_URL=redis://localhost:6379/1
SQLALCHEMY_ENGINE_OPTIONS={"pool_pre_ping": True, "pool_recycle": 300}
```

### **Production Settings**
```python
# Production configuration
SQLALCHEMY_DATABASE_URI = os.environ.get('DATABASE_URL')
REDIS_URL = os.environ.get('REDIS_URL')
SECRET_KEY = os.environ.get('SECRET_KEY')
DEBUG = False
TESTING = False

# Security headers
SECURITY_HEADERS = {
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'X-XSS-Protection': '1; mode=block',
    'Strict-Transport-Security': 'max-age=31536000; includeSubDomains'
}
```

## 🧪 Testing

### **Unit and Integration Tests**
```bash
# Unit tests
dotnet test Masark.Tests.Unit

# Integration tests
dotnet test Masark.Tests.Integration

# All tests
dotnet test
```

### **Performance Testing**

The Masark Engine includes comprehensive performance testing using k6 to ensure optimal performance under various load conditions.

#### **Performance Targets**
- **Session Creation**: <100ms (P95) - Critical for user experience
- **Assessment Completion**: <2000ms (P95) - Full assessment flow
- **Cache Hit Rate**: >80% - Efficient caching strategy
- **Database Queries**: <200ms (P95) - Optimized data access
- **Concurrent Users**: 100+ sustained load
- **Error Rate**: <5% under normal load

#### **Running Performance Tests**
```bash
# Install k6
npm install -g k6

# Start the application
cd Masark.API && dotnet run

# Run performance tests
cd performance-tests
k6 run load-test.js      # Load testing (10-100 users)
k6 run stress-test.js    # Stress testing (up to 1000 users)
k6 run benchmark.js      # Benchmark testing (specific targets)
```

#### **Automated Performance Testing**
Performance tests run automatically in CI/CD:
- **On Push**: Load and benchmark tests
- **On Pull Requests**: Performance validation with results
- **Daily**: Complete test suite at 2 AM UTC
- **Manual**: Workflow dispatch with test type selection

See [Performance Testing Documentation](performance-tests/README.md) for detailed information.

### **Test Coverage**
- **Unit Tests** - Individual service and component testing
- **Integration Tests** - API endpoint and database testing
- **Performance Tests** - Load testing and response time validation with k6
- **Security Tests** - Authentication and authorization testing
- **Localization Tests** - Bilingual content and RTL support testing

## 📊 Performance Metrics

### **Response Times** (Production Environment)
- Assessment session creation: <100ms
- Question retrieval: <50ms
- Answer submission: <30ms
- Personality calculation: <200ms
- Career matching: <150ms
- Report generation: <500ms
- Translation retrieval: <10ms

### **Scalability Targets**
- **Concurrent Users**: 500,000+
- **Daily Assessments**: 100,000+
- **Database Size**: 1TB+
- **Report Generation**: 10,000+ per hour
- **API Requests**: 1M+ per hour

## 🌍 Localization

### **Supported Languages**
- **English (en)** - Primary language with LTR direction
- **Arabic (ar)** - Full RTL support with Saudi locale (ar-SA)

### **Translation Categories**
- **System Messages** (20 translations per language)
- **Authentication** (17 translations per language)
- **Assessment** (18 translations per language)
- **Careers** (16 translations per language)
- **Reports** (12 translations per language)
- **Admin Panel** (20 translations per language)
- **Personality Types** (16 translations per language)

### **RTL Support Features**
- Right-to-left text direction for Arabic content
- Arabic-optimized font families (Noto Sans Arabic)
- Proper number formatting with Arabic numerals
- Mixed LTR/RTL content handling
- CSS direction classes for layout control

## 🔒 Security

### **Authentication & Authorization**
- JWT-based stateless authentication
- Role-based access control (RBAC)
- Password hashing with bcrypt and salt
- Token expiration and refresh mechanisms
- Session management and logout functionality

### **Data Security**
- Input validation and sanitization
- SQL injection prevention with parameterized queries
- XSS protection with content security policies
- CSRF protection with token validation
- Rate limiting and DDoS protection

### **Infrastructure Security**
- HTTPS-only communication
- Security headers (HSTS, CSP, X-Frame-Options)
- Database connection encryption
- File upload restrictions and validation
- Error message sanitization

## 📈 Monitoring & Analytics

### **System Metrics**
- Real-time user activity monitoring
- Assessment completion rates and analytics
- Career matching accuracy tracking
- Report generation statistics
- API performance monitoring

### **Health Checks**
- Database connectivity monitoring
- Redis cache status checking
- File system availability verification
- External service dependency monitoring
- Automated alerting for system issues

## 🚀 CI/CD & Deployment

### **Automated CI/CD Pipeline**

The Masark Engine uses a comprehensive GitHub Actions-based CI/CD pipeline with multiple workflows for different aspects of deployment and validation.

#### **Main CI/CD Workflow** (`.github/workflows/ci-cd.yml`)
- **Triggers**: Push to `main`/`develop` branches, pull requests to `main`, releases
- **Stages**:
  - **Build & Test**: .NET 8.0 build, unit tests, integration tests
  - **Security Scan**: CodeQL analysis, vulnerability scanning
  - **Docker Build**: Multi-stage Docker image creation with caching
  - **Code Quality**: Code coverage analysis and reporting
  - **Performance Tests**: Load testing with k6 (production releases only)

#### **Azure Deployment Workflow** (`.github/workflows/deploy-azure.yml`)
- **Triggers**: Successful CI/CD completion, manual dispatch
- **Environments**: Staging, Production
- **Features**:
  - ARM template deployment
  - Azure App Service configuration
  - Health checks and smoke tests
  - Automatic cleanup of old staging deployments

#### **AWS Deployment Workflow** (`.github/workflows/deploy-aws.yml`)
- **Triggers**: Successful CI/CD completion, manual dispatch
- **Environments**: Staging, Production
- **Features**:
  - CloudFormation stack deployment
  - ECR image management
  - ECS service updates
  - Load testing for production deployments

#### **Deployment Validation** (`.github/workflows/validate-deployment.yml`)
- **Triggers**: Completion of Azure/AWS deployments
- **Validation Steps**:
  - Health endpoint verification
  - API smoke tests
  - Security header validation
  - Performance benchmarking

#### **Environment Configuration** (`.github/workflows/environments.yml`)
- **Manual Workflow**: Configure, validate, cleanup, or reset environments
- **Features**:
  - Azure Key Vault integration
  - AWS Parameter Store management
  - Environment-specific configurations
  - Resource cleanup automation

### **Deployment Environments**

#### **Staging Environment**
- **Purpose**: Pre-production testing and validation
- **Configuration**: 
  - Azure: P1v3 App Service Plan
  - AWS: t3.small instances
  - Deployment Mode: MAWHIBA
  - Auto-cleanup: Keeps last 3 deployments

#### **Production Environment**
- **Purpose**: Live production system
- **Configuration**:
  - Azure: P2v3 App Service Plan
  - AWS: t3.medium instances
  - Deployment Mode: STANDARD
  - Enhanced monitoring and alerting
  - Manual approval required for changes

### **Manual Deployment**

You can trigger deployments manually through GitHub Actions:

1. **Navigate to Actions Tab**: Go to your repository's Actions tab
2. **Select Workflow**: Choose the deployment workflow (Azure or AWS)
3. **Run Workflow**: Click "Run workflow" button
4. **Configure Options**:
   - **Environment**: Select `staging` or `production`
   - **Deployment Target**: Choose `azure`, `aws`, or `both`
5. **Execute**: Click "Run workflow" to start deployment

### **Environment Management**

Use the Environment Configuration workflow for advanced operations:

```bash
# Configure new environment
Workflow: environments.yml
Action: configure
Environment: staging/production

# Validate environment setup
Workflow: environments.yml
Action: validate
Environment: staging/production

# Cleanup old resources (staging only)
Workflow: environments.yml
Action: cleanup
Environment: staging

# Reset environment (staging only)
Workflow: environments.yml
Action: reset
Environment: staging
```

### **Docker Deployment**

For local development and testing:

```bash
# Build and run with Docker Compose
docker-compose up -d

# Scale services
docker-compose up -d --scale web=3

# View logs
docker-compose logs -f web

# Build with specific environment
docker build --build-arg ASPNETCORE_ENVIRONMENT=Production \
             --build-arg DEPLOYMENT_MODE=STANDARD \
             --build-arg BUILD_VERSION=1.0.0 \
             -t masark-engine:latest .
```

### **Production Deployment**

For manual production deployment:

```bash
# Build the application
dotnet publish Masark.API/Masark.API.csproj -c Release -o ./publish

# Set production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export DEPLOYMENT_MODE=STANDARD

# Initialize database
dotnet ef database update --project Masark.Infrastructure

# Start the application
dotnet ./publish/Masark.API.dll
```

### **Deployment Monitoring**

Monitor deployments through:
- **GitHub Actions**: Real-time workflow execution logs
- **Azure Portal**: App Service metrics and logs
- **AWS CloudWatch**: ECS service and application logs
- **Health Endpoints**: `/health` and `/api/system/health`
- **Grafana Dashboard**: Custom metrics and alerts

## 📞 Support & Maintenance

### **System Administration**
- Regular database backups and maintenance
- Log rotation and monitoring
- Security updates and patches
- Performance optimization and tuning
- User management and access control

### **Troubleshooting**
- Comprehensive logging with structured output
- Error tracking and alerting systems
- Performance monitoring and profiling
- Database query optimization
- Cache invalidation and management

## 📄 License

This project is proprietary software developed for Masark. All rights reserved.

## 🤝 Contributing

This is a private project. For internal development guidelines and contribution procedures, please refer to the internal development documentation.

---

**Masark Mawhiba Personality-Career Matching Engine**  
*Empowering Saudi Arabia's educational future through intelligent personality assessment and career guidance.*

