# CI/CD Documentation for Masark Engine

## Overview

The Masark Engine implements a comprehensive CI/CD pipeline using GitHub Actions, supporting automated builds, testing, security scanning, and deployments to both Azure and AWS cloud platforms.

## Workflow Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Code Push     │───▶│   CI/CD Pipeline │───▶│   Deployments   │
│                 │    │                  │    │                 │
│ • main/develop  │    │ • Build & Test   │    │ • Azure         │
│ • Pull Request  │    │ • Security Scan  │    │ • AWS           │
│ • Release       │    │ • Docker Build   │    │ • Validation    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Workflows Detail

### 1. Main CI/CD Pipeline (`ci-cd.yml`)

**Purpose**: Core build, test, and quality assurance pipeline

**Triggers**:
- Push to `main` or `develop` branches
- Pull requests to `main` branch
- Release publications

**Jobs**:

#### Build and Test
- **Matrix Strategy**: Tests against .NET 8.0
- **Dependencies**: Restore NuGet packages with caching
- **Build**: Release configuration with no-restore optimization
- **Testing**: Unit and integration tests with result artifacts
- **Artifacts**: Published application and test results

#### Security Scan
- **CodeQL Analysis**: Automated security vulnerability detection
- **Dependency Audit**: Check for vulnerable packages
- **SARIF Upload**: Security findings integration

#### Docker Build
- **Multi-stage Build**: Optimized Docker image creation
- **Build Cache**: GitHub Actions cache integration
- **Image Artifacts**: Compressed Docker image storage

#### Code Quality
- **Coverage Analysis**: XPlat Code Coverage collection
- **Codecov Integration**: Coverage reporting and tracking
- **Quality Gates**: Configurable failure thresholds

#### Integration Tests
- **Service Dependencies**: SQL Server 2022 and Redis 7
- **Health Checks**: Database and cache connectivity
- **Environment Variables**: Test-specific configurations

#### Performance Tests (Production Only)
- **Load Testing**: k6-based performance validation
- **Metrics Collection**: Response time and throughput analysis
- **Threshold Validation**: Performance regression detection

### 2. Azure Deployment (`deploy-azure.yml`)

**Purpose**: Automated deployment to Azure App Service

**Triggers**:
- Successful completion of CI/CD pipeline
- Manual workflow dispatch

**Environment Support**:
- **Staging**: P1v3 App Service Plan, MAWHIBA mode
- **Production**: P2v3 App Service Plan, STANDARD mode

**Deployment Process**:
1. **Artifact Download**: Retrieve build artifacts from CI/CD
2. **Azure Authentication**: Service principal login
3. **Resource Group Creation**: Environment-specific resource groups
4. **ARM Template Deployment**: Infrastructure as Code
5. **Application Deployment**: Web app package deployment
6. **Configuration**: Environment-specific app settings
7. **Health Validation**: Multi-endpoint health checks
8. **Smoke Testing**: API functionality verification

**Features**:
- **Auto-cleanup**: Removes old staging deployments (keeps last 3)
- **Health Monitoring**: Comprehensive endpoint testing
- **Rollback Support**: Deployment slot management
- **Monitoring Integration**: Application Insights configuration

### 3. AWS Deployment (`deploy-aws.yml`)

**Purpose**: Automated deployment to AWS ECS/Fargate

**Triggers**:
- Successful completion of CI/CD pipeline
- Manual workflow dispatch

**Environment Support**:
- **Staging**: t3.small instances, 1-3 replicas
- **Production**: t3.medium instances, 2-10 replicas

**Deployment Process**:
1. **Docker Image**: Download and load from CI/CD artifacts
2. **ECR Management**: Repository creation and image pushing
3. **CloudFormation**: Infrastructure stack deployment
4. **ECS Service Update**: Rolling deployment with health checks
5. **Load Balancer**: Target group health validation
6. **Performance Testing**: Production load testing with k6

**Features**:
- **Blue-Green Deployment**: Zero-downtime deployments
- **Auto-scaling**: Dynamic capacity management
- **Load Testing**: Production performance validation
- **Resource Cleanup**: Automated old resource removal

### 4. Deployment Validation (`validate-deployment.yml`)

**Purpose**: Post-deployment validation and testing

**Triggers**:
- Completion of Azure or AWS deployment workflows
- Manual validation requests

**Validation Steps**:

#### Health Checks
- **Multiple Endpoints**: `/health`, `/api/system/health`, `/api/system/status`
- **Retry Logic**: 10-15 attempts with exponential backoff
- **Timeout Handling**: Environment-specific timeout configurations

#### API Smoke Tests
- **Core Endpoints**: Assessment, careers, system, localization APIs
- **Response Validation**: Status codes and content type verification
- **Error Reporting**: Detailed failure analysis

#### Performance Testing (Production)
- **Load Generation**: k6-based concurrent user simulation
- **Response Time**: Sub-2000ms response time validation
- **Throughput**: Request per second benchmarking

#### Security Validation
- **Security Headers**: X-Frame-Options, X-Content-Type-Options
- **SSL/TLS**: Certificate and encryption validation
- **OWASP Compliance**: Security best practices verification

### 5. Environment Configuration (`environments.yml`)

**Purpose**: Environment management and configuration

**Triggers**: Manual workflow dispatch only

**Actions**:

#### Configure
- **Azure**: Resource group and Key Vault setup
- **AWS**: Parameter Store and ECR repository configuration
- **Secrets Management**: Environment-specific secret storage

#### Validate
- **Resource Verification**: Check all required resources exist
- **Configuration Audit**: Validate all parameters and settings
- **Connectivity Testing**: Verify service integrations

#### Cleanup (Staging Only)
- **Resource Pruning**: Remove old staging deployments
- **Cost Optimization**: Clean up unused resources
- **Storage Management**: ECR image cleanup

#### Reset (Staging Only)
- **Complete Reset**: Full environment recreation
- **Data Purging**: Remove all staging data
- **Fresh Start**: Clean slate for testing

## Environment Configuration

### Staging Environment

**Purpose**: Pre-production testing and validation

**Configuration**:
```yaml
DEPLOYMENT_MODE: MAWHIBA
LOG_LEVEL: Information
AZURE_SKU: P1v3
AWS_INSTANCE_TYPE: t3.small
MIN_REPLICAS: 1
MAX_REPLICAS: 3
HEALTH_CHECK_TIMEOUT: 30s
```

**Features**:
- Automatic cleanup of old deployments
- Relaxed security policies for testing
- Enhanced logging for debugging
- Integration with test data sets

### Production Environment

**Purpose**: Live production system serving end users

**Configuration**:
```yaml
DEPLOYMENT_MODE: STANDARD
LOG_LEVEL: Warning
AZURE_SKU: P2v3
AWS_INSTANCE_TYPE: t3.medium
MIN_REPLICAS: 2
MAX_REPLICAS: 10
HEALTH_CHECK_TIMEOUT: 60s
```

**Features**:
- Manual approval gates for changes
- Enhanced monitoring and alerting
- Performance optimization
- High availability configuration

## Security and Compliance

### Secret Management

**GitHub Secrets Required**:

#### Azure Deployment
- `AZURE_CREDENTIALS`: Service principal JSON
- `AZURE_SUBSCRIPTION_ID`: Azure subscription identifier
- `AZURE_CLIENT_ID`: Service principal client ID
- `AZURE_CLIENT_SECRET`: Service principal secret
- `AZURE_TENANT_ID`: Azure AD tenant ID
- `SQL_ADMIN_PASSWORD`: Database administrator password

#### AWS Deployment
- `AWS_ACCESS_KEY_ID`: AWS access key
- `AWS_SECRET_ACCESS_KEY`: AWS secret key
- `AWS_ACCOUNT_ID`: AWS account identifier
- `AWS_VPC_ID`: VPC for resource deployment
- `AWS_SUBNET_IDS`: Comma-separated subnet IDs
- `DB_PASSWORD`: RDS database password
- `JWT_SECRET_KEY`: JWT token signing key

### Security Scanning

**Automated Security Measures**:
- CodeQL static analysis
- Dependency vulnerability scanning
- Container image security scanning
- Infrastructure security validation
- Runtime security monitoring

## Monitoring and Observability

### Deployment Monitoring

**GitHub Actions**:
- Real-time workflow execution logs
- Artifact management and retention
- Deployment history and rollback capability
- Performance metrics collection

**Azure Monitoring**:
- Application Insights integration
- App Service metrics and logs
- Azure Monitor alerts and dashboards
- Log Analytics workspace

**AWS Monitoring**:
- CloudWatch logs and metrics
- ECS service monitoring
- Application Load Balancer health checks
- X-Ray distributed tracing

### Health Checks

**Endpoint Monitoring**:
- `/health`: Basic application health
- `/api/system/health`: Detailed system status
- `/api/system/status`: Component-level health
- Database connectivity validation
- Cache service availability

## Troubleshooting

### Common Issues

#### Build Failures
- **Dependency Issues**: Check NuGet package restoration
- **Test Failures**: Review test logs and fix failing tests
- **Security Scan Failures**: Address CodeQL findings

#### Deployment Failures
- **Authentication**: Verify service principal credentials
- **Resource Limits**: Check Azure/AWS quotas and limits
- **Network Issues**: Validate VPC and subnet configurations

#### Health Check Failures
- **Application Startup**: Check application logs for errors
- **Database Connectivity**: Verify connection strings
- **External Dependencies**: Validate third-party service availability

### Debug Commands

```bash
# View workflow logs
gh run list --workflow=ci-cd.yml
gh run view <run-id> --log

# Check deployment status
az webapp show --name masark-engine-staging --resource-group masark-rg-staging
aws ecs describe-services --cluster masark-engine-staging --services masark-engine-service

# Validate health endpoints
curl -f https://masark-engine-staging.azurewebsites.net/health
curl -f https://your-aws-load-balancer.com/api/system/health
```

## Best Practices

### Development Workflow

1. **Feature Development**: Create feature branches from `develop`
2. **Pull Requests**: Target `develop` branch for feature integration
3. **Testing**: Ensure all tests pass before merging
4. **Code Review**: Require approval before merging to `main`
5. **Release**: Merge `develop` to `main` for production deployment

### Deployment Strategy

1. **Staging First**: Always deploy to staging before production
2. **Validation**: Run full validation suite after staging deployment
3. **Production Approval**: Require manual approval for production deployments
4. **Monitoring**: Monitor deployments for at least 30 minutes post-deployment
5. **Rollback Plan**: Have rollback procedures ready for production issues

### Security Guidelines

1. **Secret Rotation**: Regularly rotate all secrets and credentials
2. **Least Privilege**: Use minimal required permissions for service principals
3. **Audit Logging**: Enable comprehensive audit logging for all environments
4. **Vulnerability Management**: Address security findings promptly
5. **Compliance**: Ensure all deployments meet security compliance requirements

## Support and Maintenance

### Regular Maintenance Tasks

- **Weekly**: Review deployment logs and metrics
- **Monthly**: Update dependencies and security patches
- **Quarterly**: Review and optimize resource configurations
- **Annually**: Audit security configurations and access controls

### Emergency Procedures

1. **Production Issues**: Use emergency rollback procedures
2. **Security Incidents**: Follow incident response playbook
3. **Service Outages**: Activate disaster recovery procedures
4. **Data Issues**: Execute data recovery protocols

For additional support, refer to the main README.md file or contact the development team.
