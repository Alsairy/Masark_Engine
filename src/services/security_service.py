"""
Production-Grade Security Service for Masark Engine
Implements comprehensive security features including authentication, authorization,
input validation, and security monitoring
"""

import re
import hashlib
import secrets
import time
import jwt
from typing import Dict, List, Optional, Tuple, Any
from datetime import datetime, timedelta
from dataclasses import dataclass, field
import logging
import threading
from collections import defaultdict

logger = logging.getLogger(__name__)

@dataclass
class SecurityEvent:
    """Security event for monitoring"""
    event_type: str
    client_id: str
    timestamp: datetime
    details: Dict[str, Any]
    severity: str  # low, medium, high, critical

@dataclass
class AuthenticationResult:
    """Result of authentication attempt"""
    success: bool
    user_id: Optional[str] = None
    session_token: Optional[str] = None
    error_message: Optional[str] = None
    requires_2fa: bool = False

@dataclass
class ValidationResult:
    """Result of input validation"""
    is_valid: bool
    sanitized_input: Optional[str] = None
    violations: List[str] = field(default_factory=list)

class SecurityService:
    """
    Production-grade security service with comprehensive protection features
    """
    
    def __init__(self, jwt_secret: Optional[str] = None):
        self.jwt_secret = jwt_secret or secrets.token_urlsafe(32)
        
        # Security configuration
        self.config = {
            'session_timeout_minutes': 30,
            'max_login_attempts': 5,
            'lockout_duration_minutes': 15,
            'password_min_length': 8,
            'require_special_chars': True,
            'jwt_expiry_hours': 24,
            'enable_2fa': False,  # Can be enabled for admin accounts
            'max_input_length': 1000,
            'allowed_file_types': ['.pdf', '.jpg', '.png'],
            'max_file_size_mb': 10
        }
        
        # Security monitoring
        self.security_events: List[SecurityEvent] = []
        self.failed_login_attempts: Dict[str, List[datetime]] = defaultdict(list)
        self.locked_accounts: Dict[str, datetime] = {}
        self.active_sessions: Dict[str, Dict[str, Any]] = {}
        
        # Input validation patterns
        self.validation_patterns = {
            'email': re.compile(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'),
            'student_id': re.compile(r'^[A-Z0-9]{6,12}$'),
            'name': re.compile(r'^[a-zA-Z\u0600-\u06FF\s\-\'\.]{2,50}$'),  # Supports Arabic
            'phone': re.compile(r'^\+?[1-9]\d{1,14}$'),
            'safe_text': re.compile(r'^[a-zA-Z0-9\u0600-\u06FF\s\-_.,!?()]+$')
        }
        
        # XSS and injection patterns to block
        self.malicious_patterns = [
            re.compile(r'<script[^>]*>.*?</script>', re.IGNORECASE | re.DOTALL),
            re.compile(r'javascript:', re.IGNORECASE),
            re.compile(r'on\w+\s*=', re.IGNORECASE),
            re.compile(r'<iframe[^>]*>', re.IGNORECASE),
            re.compile(r'<object[^>]*>', re.IGNORECASE),
            re.compile(r'<embed[^>]*>', re.IGNORECASE),
            re.compile(r'union\s+select', re.IGNORECASE),
            re.compile(r'drop\s+table', re.IGNORECASE),
            re.compile(r'delete\s+from', re.IGNORECASE),
            re.compile(r'insert\s+into', re.IGNORECASE),
            re.compile(r'update\s+set', re.IGNORECASE)
        ]
        
        # Cleanup thread
        self.cleanup_thread = threading.Thread(target=self._security_cleanup_loop, daemon=True)
        self.cleanup_thread.start()
        
        logger.info("Security service initialized")
    
    def authenticate_user(self, identifier: str, password: str, 
                         client_ip: Optional[str] = None) -> AuthenticationResult:
        """
        Authenticate user with comprehensive security checks
        
        Args:
            identifier: Email, student ID, or username
            password: User password
            client_ip: Client IP address for monitoring
            
        Returns:
            AuthenticationResult with authentication status
        """
        client_id = client_ip or 'unknown'
        
        # Check if account is locked
        if identifier in self.locked_accounts:
            if datetime.now() < self.locked_accounts[identifier]:
                self._log_security_event(
                    'authentication_blocked',
                    client_id,
                    {'identifier': identifier, 'reason': 'account_locked'},
                    'medium'
                )
                return AuthenticationResult(
                    success=False,
                    error_message="Account temporarily locked due to multiple failed attempts"
                )
            else:
                # Unlock account
                del self.locked_accounts[identifier]
        
        # Validate input format
        if not self._is_valid_identifier(identifier):
            self._log_security_event(
                'authentication_invalid_format',
                client_id,
                {'identifier': identifier},
                'low'
            )
            return AuthenticationResult(
                success=False,
                error_message="Invalid identifier format"
            )
        
        # In production, this would check against database
        # For now, we'll simulate authentication
        auth_success = self._simulate_authentication(identifier, password)
        
        if auth_success:
            # Clear failed attempts
            if identifier in self.failed_login_attempts:
                del self.failed_login_attempts[identifier]
            
            # Generate session token
            session_token = self._generate_session_token(identifier)
            
            # Create session
            self._create_session(session_token, identifier, client_ip)
            
            self._log_security_event(
                'authentication_success',
                client_id,
                {'identifier': identifier},
                'low'
            )
            
            return AuthenticationResult(
                success=True,
                user_id=identifier,
                session_token=session_token
            )
        else:
            # Record failed attempt
            self.failed_login_attempts[identifier].append(datetime.now())
            
            # Check if should lock account
            recent_attempts = [
                attempt for attempt in self.failed_login_attempts[identifier]
                if attempt > datetime.now() - timedelta(minutes=15)
            ]
            
            if len(recent_attempts) >= self.config['max_login_attempts']:
                # Lock account
                lockout_until = datetime.now() + timedelta(
                    minutes=self.config['lockout_duration_minutes']
                )
                self.locked_accounts[identifier] = lockout_until
                
                self._log_security_event(
                    'account_locked',
                    client_id,
                    {'identifier': identifier, 'attempts': len(recent_attempts)},
                    'high'
                )
            
            self._log_security_event(
                'authentication_failed',
                client_id,
                {'identifier': identifier, 'attempts': len(recent_attempts)},
                'medium'
            )
            
            return AuthenticationResult(
                success=False,
                error_message="Invalid credentials"
            )
    
    def validate_session(self, session_token: str) -> Tuple[bool, Optional[str]]:
        """
        Validate session token and return user ID if valid
        
        Args:
            session_token: JWT session token
            
        Returns:
            Tuple of (is_valid, user_id)
        """
        try:
            # Decode JWT token
            payload = jwt.decode(session_token, self.jwt_secret, algorithms=['HS256'])
            
            user_id = payload.get('user_id')
            exp = payload.get('exp')
            
            # Check if token is expired
            if exp and datetime.fromtimestamp(exp) < datetime.now():
                return False, None
            
            # Check if session exists and is active
            if session_token in self.active_sessions:
                session = self.active_sessions[session_token]
                
                # Update last activity
                session['last_activity'] = datetime.now()
                
                return True, user_id
            
            return False, None
            
        except jwt.InvalidTokenError:
            return False, None
        except Exception as e:
            logger.error(f"Session validation error: {str(e)}")
            return False, None
    
    def validate_input(self, input_text: str, input_type: str = 'safe_text',
                      max_length: Optional[int] = None) -> ValidationResult:
        """
        Validate and sanitize user input
        
        Args:
            input_text: Input to validate
            input_type: Type of input (email, name, safe_text, etc.)
            max_length: Maximum allowed length
            
        Returns:
            ValidationResult with validation status and sanitized input
        """
        violations = []
        
        # Check length
        max_len = max_length or self.config['max_input_length']
        if len(input_text) > max_len:
            violations.append(f"Input exceeds maximum length of {max_len}")
        
        # Check for malicious patterns
        for pattern in self.malicious_patterns:
            if pattern.search(input_text):
                violations.append("Input contains potentially malicious content")
                break
        
        # Validate against specific pattern
        if input_type in self.validation_patterns:
            pattern = self.validation_patterns[input_type]
            if not pattern.match(input_text):
                violations.append(f"Input does not match required format for {input_type}")
        
        # Sanitize input
        sanitized = self._sanitize_input(input_text)
        
        is_valid = len(violations) == 0
        
        if not is_valid:
            self._log_security_event(
                'input_validation_failed',
                'system',
                {'input_type': input_type, 'violations': violations},
                'medium'
            )
        
        return ValidationResult(
            is_valid=is_valid,
            sanitized_input=sanitized if is_valid else None,
            violations=violations
        )
    
    def check_file_security(self, filename: str, file_size: int, 
                          file_content: Optional[bytes] = None) -> ValidationResult:
        """
        Check uploaded file security
        
        Args:
            filename: Name of the uploaded file
            file_size: Size of file in bytes
            file_content: File content for deep inspection
            
        Returns:
            ValidationResult with security check results
        """
        violations = []
        
        # Check file extension
        file_ext = '.' + filename.split('.')[-1].lower() if '.' in filename else ''
        if file_ext not in self.config['allowed_file_types']:
            violations.append(f"File type {file_ext} not allowed")
        
        # Check file size
        max_size_bytes = self.config['max_file_size_mb'] * 1024 * 1024
        if file_size > max_size_bytes:
            violations.append(f"File size exceeds {self.config['max_file_size_mb']}MB limit")
        
        # Check filename for malicious patterns
        filename_validation = self.validate_input(filename, 'safe_text')
        if not filename_validation.is_valid:
            violations.extend(filename_validation.violations)
        
        # Deep content inspection (if content provided)
        if file_content:
            # Check for embedded scripts or malicious content
            content_str = file_content.decode('utf-8', errors='ignore')
            for pattern in self.malicious_patterns:
                if pattern.search(content_str):
                    violations.append("File contains potentially malicious content")
                    break
        
        is_valid = len(violations) == 0
        
        if not is_valid:
            self._log_security_event(
                'file_security_violation',
                'system',
                {'filename': filename, 'violations': violations},
                'high'
            )
        
        return ValidationResult(
            is_valid=is_valid,
            violations=violations
        )
    
    def logout_user(self, session_token: str):
        """Logout user and invalidate session"""
        if session_token in self.active_sessions:
            user_id = self.active_sessions[session_token].get('user_id')
            del self.active_sessions[session_token]
            
            self._log_security_event(
                'user_logout',
                'system',
                {'user_id': user_id},
                'low'
            )
    
    def get_security_dashboard(self) -> Dict[str, Any]:
        """Get security dashboard data"""
        now = datetime.now()
        last_24h = now - timedelta(hours=24)
        
        # Filter recent events
        recent_events = [
            event for event in self.security_events
            if event.timestamp > last_24h
        ]
        
        # Count events by type
        event_counts = defaultdict(int)
        severity_counts = defaultdict(int)
        
        for event in recent_events:
            event_counts[event.event_type] += 1
            severity_counts[event.severity] += 1
        
        return {
            'active_sessions': len(self.active_sessions),
            'locked_accounts': len(self.locked_accounts),
            'recent_events_24h': len(recent_events),
            'event_counts': dict(event_counts),
            'severity_counts': dict(severity_counts),
            'failed_login_attempts': len(self.failed_login_attempts),
            'security_alerts': self._get_security_alerts()
        }
    
    def get_security_events(self, hours: int = 24, 
                          severity: Optional[str] = None) -> List[Dict[str, Any]]:
        """Get security events for analysis"""
        cutoff_time = datetime.now() - timedelta(hours=hours)
        
        filtered_events = [
            event for event in self.security_events
            if event.timestamp > cutoff_time and
            (severity is None or event.severity == severity)
        ]
        
        return [
            {
                'event_type': event.event_type,
                'client_id': event.client_id,
                'timestamp': event.timestamp.isoformat(),
                'details': event.details,
                'severity': event.severity
            }
            for event in filtered_events
        ]
    
    def _simulate_authentication(self, identifier: str, password: str) -> bool:
        """Secure authentication - no demo bypasses allowed"""
        # Always return False - this method should not be used for real authentication
        return False
    
    def _is_valid_identifier(self, identifier: str) -> bool:
        """Check if identifier format is valid"""
        # Check email format
        if '@' in identifier:
            return self.validation_patterns['email'].match(identifier) is not None
        
        # Check student ID format
        return self.validation_patterns['student_id'].match(identifier) is not None
    
    def _generate_session_token(self, user_id: str) -> str:
        """Generate JWT session token"""
        payload = {
            'user_id': user_id,
            'iat': datetime.now(),
            'exp': datetime.now() + timedelta(hours=self.config['jwt_expiry_hours'])
        }
        
        return jwt.encode(payload, self.jwt_secret, algorithm='HS256')
    
    def _create_session(self, session_token: str, user_id: str, client_ip: Optional[str] = None):
        """Create new session"""
        self.active_sessions[session_token] = {
            'user_id': user_id,
            'created_at': datetime.now(),
            'last_activity': datetime.now(),
            'client_ip': client_ip
        }
    
    def _sanitize_input(self, input_text: str) -> str:
        """Sanitize input by removing/escaping dangerous characters"""
        # Remove null bytes
        sanitized = input_text.replace('\x00', '')
        
        # Escape HTML entities
        html_escape_table = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#x27;',
            '/': '&#x2F;'
        }
        
        for char, escape in html_escape_table.items():
            sanitized = sanitized.replace(char, escape)
        
        return sanitized.strip()
    
    def _log_security_event(self, event_type: str, client_id: str, 
                          details: Dict[str, Any], severity: str):
        """Log security event"""
        event = SecurityEvent(
            event_type=event_type,
            client_id=client_id,
            timestamp=datetime.now(),
            details=details,
            severity=severity
        )
        
        self.security_events.append(event)
        
        # Log to system logger
        log_level = {
            'low': logging.INFO,
            'medium': logging.WARNING,
            'high': logging.ERROR,
            'critical': logging.CRITICAL
        }.get(severity, logging.INFO)
        
        logger.log(log_level, f"Security event: {event_type} - {details}")
        
        # Keep only recent events (last 7 days)
        cutoff_time = datetime.now() - timedelta(days=7)
        self.security_events = [
            e for e in self.security_events if e.timestamp > cutoff_time
        ]
    
    def _get_security_alerts(self) -> List[Dict[str, Any]]:
        """Get current security alerts"""
        alerts = []
        
        # Check for high number of failed logins
        recent_failed_logins = sum(
            len(attempts) for attempts in self.failed_login_attempts.values()
        )
        
        if recent_failed_logins > 50:  # Threshold for alert
            alerts.append({
                'type': 'high_failed_logins',
                'severity': 'medium',
                'message': f'{recent_failed_logins} failed login attempts detected',
                'count': recent_failed_logins
            })
        
        # Check for locked accounts
        if len(self.locked_accounts) > 5:
            alerts.append({
                'type': 'multiple_locked_accounts',
                'severity': 'high',
                'message': f'{len(self.locked_accounts)} accounts currently locked',
                'count': len(self.locked_accounts)
            })
        
        # Check for recent high-severity events
        recent_critical = [
            event for event in self.security_events
            if event.severity in ['high', 'critical'] and
            event.timestamp > datetime.now() - timedelta(hours=1)
        ]
        
        if len(recent_critical) > 10:
            alerts.append({
                'type': 'high_severity_events',
                'severity': 'critical',
                'message': f'{len(recent_critical)} high-severity events in last hour',
                'count': len(recent_critical)
            })
        
        return alerts
    
    def _security_cleanup_loop(self):
        """Background cleanup of security data"""
        while True:
            try:
                time.sleep(300)  # Run every 5 minutes
                self._cleanup_security_data()
            except Exception as e:
                logger.error(f"Security cleanup error: {str(e)}")
    
    def _cleanup_security_data(self):
        """Clean up old security data"""
        now = datetime.now()
        
        # Remove expired sessions
        expired_sessions = []
        session_timeout = timedelta(minutes=self.config['session_timeout_minutes'])
        
        for token, session in self.active_sessions.items():
            if now - session['last_activity'] > session_timeout:
                expired_sessions.append(token)
        
        for token in expired_sessions:
            del self.active_sessions[token]
        
        # Remove old failed login attempts
        cutoff_time = now - timedelta(hours=24)
        for identifier in list(self.failed_login_attempts.keys()):
            self.failed_login_attempts[identifier] = [
                attempt for attempt in self.failed_login_attempts[identifier]
                if attempt > cutoff_time
            ]
            
            # Remove empty lists
            if not self.failed_login_attempts[identifier]:
                del self.failed_login_attempts[identifier]
        
        # Remove expired account locks
        expired_locks = [
            identifier for identifier, unlock_time in self.locked_accounts.items()
            if now > unlock_time
        ]
        
        for identifier in expired_locks:
            del self.locked_accounts[identifier]
        
        if expired_sessions or expired_locks:
            logger.info(f"Security cleanup: removed {len(expired_sessions)} expired sessions "
                       f"and {len(expired_locks)} expired locks")

# Global security service instance
security_service = SecurityService()

# Decorator for authentication required
def authentication_required(func):
    """Decorator to require authentication for endpoint"""
    def wrapper(*args, **kwargs):
        # In a web framework, this would extract token from request headers
        # For now, we'll assume token is passed as first argument
        if not args or not isinstance(args[0], str):
            raise Exception("Authentication token required")
        
        token = args[0]
        is_valid, user_id = security_service.validate_session(token)
        
        if not is_valid:
            raise Exception("Invalid or expired session token")
        
        # Add user_id to kwargs for use in function
        kwargs['authenticated_user_id'] = user_id
        
        return func(*args, **kwargs)
    
    return wrapper

# Decorator for input validation
def validate_input(input_type: str = 'safe_text', max_length: Optional[int] = None):
    """Decorator for automatic input validation"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # Validate all string arguments
            validated_args = []
            for arg in args:
                if isinstance(arg, str):
                    validation = security_service.validate_input(arg, input_type, max_length)
                    if not validation.is_valid:
                        raise Exception(f"Input validation failed: {validation.violations}")
                    validated_args.append(validation.sanitized_input)
                else:
                    validated_args.append(arg)
            
            # Validate string values in kwargs
            validated_kwargs = {}
            for key, value in kwargs.items():
                if isinstance(value, str):
                    validation = security_service.validate_input(value, input_type, max_length)
                    if not validation.is_valid:
                        raise Exception(f"Input validation failed for {key}: {validation.violations}")
                    validated_kwargs[key] = validation.sanitized_input
                else:
                    validated_kwargs[key] = value
            
            return func(*validated_args, **validated_kwargs)
        
        return wrapper
    return decorator

