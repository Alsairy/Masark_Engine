# Repository Secrets Configuration Guide

This document provides comprehensive instructions for setting up all required secrets for the Masark Engine CI/CD workflows.

## Overview

The Masark Engine CI/CD pipeline requires various secrets to authenticate with cloud providers, databases, and other services. These secrets must be configured in the GitHub repository settings before running any deployment workflows.

## Required Secrets by Workflow

### Main CI/CD Pipeline (`ci-cd.yml`)

The main CI/CD pipeline requires minimal secrets as it focuses on building, testing, and creating artifacts.

**Optional Secrets:**
- `CODECOV_TOKEN` - For code coverage reporting (optional but recommended)

### Azure Deployment Workflow (`deploy-azure.yml`)

**Required Secrets:**
- `AZURE_CREDENTIALS` - JSON object containing Azure service principal credentials
- `AZURE_SUBSCRIPTION_ID` - Azure subscription identifier
- `AZURE_CLIENT_ID` - Azure service principal client ID
- `AZURE_CLIENT_SECRET` - Azure service principal client secret
- `AZURE_TENANT_ID` - Azure Active Directory tenant ID
- `SQL_ADMIN_PASSWORD` - Password for Azure SQL Server administrator account

### AWS Deployment Workflow (`deploy-aws.yml`)

**Required Secrets:**
- `AWS_ACCESS_KEY_ID` - AWS access key ID for programmatic access
- `AWS_SECRET_ACCESS_KEY` - AWS secret access key for programmatic access
- `AWS_ACCOUNT_ID` - AWS account identifier (12-digit number)
- `AWS_VPC_ID` - VPC ID where resources will be deployed
- `AWS_SUBNET_IDS` - Comma-separated list of subnet IDs for deployment
- `DB_PASSWORD` - Password for RDS database instance
- `JWT_SECRET_KEY` - Secret key for JWT token generation and validation

### Environment Configuration Workflow (`environments.yml`)

**Required Secrets:**
- All Azure secrets (same as Azure deployment workflow)
- All AWS secrets (same as AWS deployment workflow)

## Detailed Secret Descriptions

### Azure Secrets

#### `AZURE_CREDENTIALS`
**Purpose**: Service principal credentials for Azure authentication
**Format**: JSON object
**Example Structure**:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "your-client-secret-here",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```
**How to Generate**:
1. Create a service principal in Azure CLI:
   ```bash
   az ad sp create-for-rbac --name "masark-github-actions" --role contributor --scopes /subscriptions/{subscription-id} --sdk-auth
   ```
2. Copy the entire JSON output as the secret value

#### `AZURE_SUBSCRIPTION_ID`
**Purpose**: Identifies the Azure subscription for resource deployment
**Format**: GUID (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
**How to Find**: 
```bash
az account show --query id --output tsv
```

#### `AZURE_CLIENT_ID`
**Purpose**: Service principal application ID
**Format**: GUID
**How to Find**: Extract from `AZURE_CREDENTIALS` JSON or Azure portal

#### `AZURE_CLIENT_SECRET`
**Purpose**: Service principal password/secret
**Format**: String
**Security**: High - Never expose in logs or code

#### `AZURE_TENANT_ID`
**Purpose**: Azure Active Directory tenant identifier
**Format**: GUID
**How to Find**: 
```bash
az account show --query tenantId --output tsv
```

#### `SQL_ADMIN_PASSWORD`
**Purpose**: Administrator password for Azure SQL Server
**Format**: String (must meet Azure SQL complexity requirements)
**Requirements**:
- At least 8 characters
- Contains uppercase, lowercase, numbers, and special characters
- Cannot contain username or common words

### AWS Secrets

#### `AWS_ACCESS_KEY_ID`
**Purpose**: AWS programmatic access key identifier
**Format**: 20-character alphanumeric string (AKIA...)
**How to Generate**:
1. Create IAM user with programmatic access
2. Attach necessary policies (EC2, ECS, CloudFormation, ECR permissions)
3. Generate access key pair

#### `AWS_SECRET_ACCESS_KEY`
**Purpose**: AWS secret access key for authentication
**Format**: 40-character base64-encoded string
**Security**: Critical - Never expose or log this value

#### `AWS_ACCOUNT_ID`
**Purpose**: AWS account identifier for resource ARNs
**Format**: 12-digit number
**How to Find**:
```bash
aws sts get-caller-identity --query Account --output text
```

#### `AWS_VPC_ID`
**Purpose**: Virtual Private Cloud ID for resource deployment
**Format**: vpc-xxxxxxxxx
**How to Find**:
```bash
aws ec2 describe-vpcs --query 'Vpcs[0].VpcId' --output text
```

#### `AWS_SUBNET_IDS`
**Purpose**: Subnet IDs for multi-AZ deployment
**Format**: Comma-separated list (subnet-xxxxxxxx,subnet-yyyyyyyy)
**Requirements**: At least 2 subnets in different availability zones
**How to Find**:
```bash
aws ec2 describe-subnets --query 'Subnets[*].SubnetId' --output text
```

#### `DB_PASSWORD`
**Purpose**: RDS database master password
**Format**: String (must meet RDS complexity requirements)
**Requirements**:
- 8-128 characters
- Cannot contain /, ", @, or space
- Must contain characters from at least 3 of: uppercase, lowercase, digits, special chars

#### `JWT_SECRET_KEY`
**Purpose**: Secret key for JWT token signing and validation
**Format**: Base64-encoded string (recommended 256-bit key)
**How to Generate**:
```bash
openssl rand -base64 32
```

## Security Best Practices

### Secret Generation
- **Use Strong Passwords**: Generate complex, unique passwords for all services
- **Rotate Regularly**: Implement a rotation schedule for all secrets
- **Minimum Privileges**: Grant only necessary permissions to service accounts
- **Separate Environments**: Use different secrets for staging and production

### Secret Storage
- **GitHub Secrets Only**: Never store secrets in code, configuration files, or documentation
- **Environment Separation**: Use GitHub Environments to separate staging and production secrets
- **Access Control**: Limit repository access to authorized personnel only

### Secret Management
- **Audit Access**: Regularly review who has access to secrets
- **Monitor Usage**: Track secret usage through cloud provider logs
- **Incident Response**: Have procedures for secret compromise scenarios

## How to Configure Secrets in GitHub

### Step-by-Step Instructions

1. **Navigate to Repository Settings**
   - Go to your GitHub repository
   - Click on "Settings" tab
   - Select "Secrets and variables" → "Actions"

2. **Add Repository Secrets**
   - Click "New repository secret"
   - Enter the secret name (exactly as listed above)
   - Paste the secret value
   - Click "Add secret"

3. **Add Environment Secrets** (Recommended)
   - Go to "Environments" in repository settings
   - Create environments: `staging` and `production`
   - Add environment-specific secrets
   - Configure protection rules for production

### Environment-Specific Configuration

#### Staging Environment
Create a `staging` environment with:
- All required secrets with staging values
- Less restrictive protection rules
- Automatic deployments allowed

#### Production Environment
Create a `production` environment with:
- All required secrets with production values
- Required reviewers for deployments
- Deployment branch restrictions (main only)
- Wait timer before deployment

## Validation and Testing

### Secret Validation Checklist

Before running workflows, verify:
- [ ] All required secrets are configured
- [ ] Secret values are correct and properly formatted
- [ ] Service principals have necessary permissions
- [ ] Database passwords meet complexity requirements
- [ ] JWT secret is properly generated
- [ ] Environment-specific secrets are configured

### Testing Secret Configuration

1. **Run Environment Validation Workflow**
   ```
   Workflow: environments.yml
   Action: validate
   Environment: staging
   ```

2. **Check Workflow Logs**
   - Review authentication steps
   - Verify resource access
   - Confirm no secret exposure in logs

3. **Test Deployment**
   - Run a staging deployment
   - Monitor for authentication errors
   - Verify resource creation

## Troubleshooting Common Issues

### Azure Authentication Failures
**Symptoms**: "Authentication failed" or "Insufficient privileges"
**Solutions**:
- Verify service principal has Contributor role
- Check subscription ID is correct
- Ensure tenant ID matches Azure AD
- Regenerate service principal credentials

### AWS Authentication Failures
**Symptoms**: "Access denied" or "Invalid credentials"
**Solutions**:
- Verify IAM user has necessary policies
- Check access key is active
- Ensure account ID is correct
- Verify region settings

### Database Connection Issues
**Symptoms**: "Connection failed" or "Authentication failed"
**Solutions**:
- Verify password meets complexity requirements
- Check firewall rules allow GitHub Actions IPs
- Ensure database server is running
- Verify connection string format

### JWT Token Issues
**Symptoms**: "Invalid token" or "Token verification failed"
**Solutions**:
- Ensure JWT secret is base64-encoded
- Verify secret length (minimum 256 bits)
- Check for special characters in secret
- Regenerate JWT secret if corrupted

## Emergency Procedures

### Secret Compromise Response
1. **Immediate Actions**:
   - Disable compromised credentials immediately
   - Revoke access tokens and API keys
   - Change all related passwords

2. **Investigation**:
   - Review access logs for unauthorized usage
   - Identify scope of potential breach
   - Document incident details

3. **Recovery**:
   - Generate new credentials
   - Update GitHub secrets
   - Test all workflows
   - Monitor for suspicious activity

### Service Principal Rotation
1. **Create New Service Principal**:
   ```bash
   az ad sp create-for-rbac --name "masark-github-actions-new" --role contributor --scopes /subscriptions/{subscription-id} --sdk-auth
   ```

2. **Update GitHub Secrets**:
   - Replace `AZURE_CREDENTIALS` with new JSON
   - Update individual credential secrets

3. **Test and Verify**:
   - Run validation workflow
   - Perform test deployment
   - Delete old service principal

### AWS Access Key Rotation
1. **Create New Access Key**:
   - Generate new key pair in IAM console
   - Keep old key active initially

2. **Update GitHub Secrets**:
   - Replace `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`

3. **Test and Cleanup**:
   - Run validation workflow
   - Perform test deployment
   - Deactivate old access key

## Compliance and Auditing

### Regular Audits
- **Monthly**: Review secret usage logs
- **Quarterly**: Rotate non-critical secrets
- **Annually**: Full security audit and penetration testing

### Compliance Requirements
- **SOC 2**: Implement access controls and monitoring
- **ISO 27001**: Document secret management procedures
- **GDPR**: Ensure proper data protection for EU users

### Documentation Requirements
- Maintain secret inventory
- Document rotation procedures
- Record access permissions
- Track compliance activities

## Support and Contacts

### Internal Support
- **DevOps Team**: For CI/CD pipeline issues
- **Security Team**: For secret management and compliance
- **Cloud Team**: For Azure/AWS configuration

### External Support
- **GitHub Support**: For GitHub Actions and secrets issues
- **Azure Support**: For Azure service principal and authentication
- **AWS Support**: For IAM and access management

---

**Important**: This document contains sensitive information about secret management. Ensure it's only accessible to authorized personnel and regularly review access permissions.

**Last Updated**: $(date -u)
**Version**: 1.0
**Owner**: Masark Engine DevOps Team
