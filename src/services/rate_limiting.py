"""
Production-Grade Rate Limiting Service for Masark Engine
Implements intelligent rate limiting to protect against abuse and ensure fair usage
"""

import time
import threading
from typing import Dict, Optional, Tuple, List
from datetime import datetime, timedelta
from dataclasses import dataclass
from collections import defaultdict, deque
import logging

logger = logging.getLogger(__name__)

@dataclass
class RateLimit:
    """Rate limit configuration"""
    requests_per_window: int
    window_seconds: int
    burst_allowance: int = 0  # Additional requests allowed in burst

@dataclass
class RateLimitStatus:
    """Current rate limit status for a client"""
    allowed: bool
    remaining_requests: int
    reset_time: datetime
    retry_after_seconds: Optional[int] = None

class TokenBucket:
    """Token bucket algorithm implementation for rate limiting"""
    
    def __init__(self, capacity: int, refill_rate: float):
        self.capacity = capacity
        self.tokens = capacity
        self.refill_rate = refill_rate  # tokens per second
        self.last_refill = time.time()
        self.lock = threading.Lock()
    
    def consume(self, tokens: int = 1) -> bool:
        """Try to consume tokens from the bucket"""
        with self.lock:
            now = time.time()
            
            # Refill tokens based on elapsed time
            elapsed = now - self.last_refill
            self.tokens = min(self.capacity, self.tokens + elapsed * self.refill_rate)
            self.last_refill = now
            
            # Check if we have enough tokens
            if self.tokens >= tokens:
                self.tokens -= tokens
                return True
            
            return False
    
    def get_tokens(self) -> float:
        """Get current token count"""
        with self.lock:
            now = time.time()
            elapsed = now - self.last_refill
            self.tokens = min(self.capacity, self.tokens + elapsed * self.refill_rate)
            self.last_refill = now
            return self.tokens

class SlidingWindowCounter:
    """Sliding window counter for precise rate limiting"""
    
    def __init__(self, window_seconds: int, max_requests: int):
        self.window_seconds = window_seconds
        self.max_requests = max_requests
        self.requests = deque()
        self.lock = threading.Lock()
    
    def is_allowed(self) -> Tuple[bool, int]:
        """Check if request is allowed and return remaining count"""
        with self.lock:
            now = time.time()
            cutoff_time = now - self.window_seconds
            
            # Remove old requests outside the window
            while self.requests and self.requests[0] <= cutoff_time:
                self.requests.popleft()
            
            # Check if we're under the limit
            if len(self.requests) < self.max_requests:
                self.requests.append(now)
                remaining = self.max_requests - len(self.requests)
                return True, remaining
            
            remaining = 0
            return False, remaining
    
    def get_reset_time(self) -> datetime:
        """Get when the window resets"""
        with self.lock:
            if self.requests:
                oldest_request = self.requests[0]
                reset_time = oldest_request + self.window_seconds
                return datetime.fromtimestamp(reset_time)
            return datetime.now()

class RateLimitingService:
    """
    Production-grade rate limiting service with multiple algorithms and tiers
    """
    
    def __init__(self):
        # Rate limit configurations for different endpoints/tiers
        self.rate_limits = {
            # Assessment endpoints
            'assessment_start': RateLimit(10, 60),  # 10 starts per minute
            'assessment_answer': RateLimit(100, 60),  # 100 answers per minute
            'assessment_complete': RateLimit(5, 60),  # 5 completions per minute
            
            # Career endpoints
            'career_search': RateLimit(30, 60),  # 30 searches per minute
            'career_match': RateLimit(20, 60),  # 20 matches per minute
            
            # Report endpoints
            'report_generate': RateLimit(10, 60),  # 10 reports per minute
            'report_download': RateLimit(20, 60),  # 20 downloads per minute
            
            # General API
            'api_general': RateLimit(100, 60),  # 100 requests per minute
            
            # Admin endpoints (more restrictive)
            'admin_operations': RateLimit(50, 60),  # 50 operations per minute
        }
        
        # Client tracking
        self.client_counters: Dict[str, Dict[str, SlidingWindowCounter]] = defaultdict(dict)
        self.client_buckets: Dict[str, Dict[str, TokenBucket]] = defaultdict(dict)
        
        # Blocked clients (temporary bans)
        self.blocked_clients: Dict[str, datetime] = {}
        
        # Statistics
        self.stats = {
            'total_requests': 0,
            'blocked_requests': 0,
            'rate_limited_requests': 0
        }
        
        # Cleanup thread
        self.cleanup_thread = threading.Thread(target=self._cleanup_loop, daemon=True)
        self.cleanup_thread.start()
        
        logger.info("Rate limiting service initialized")
    
    def check_rate_limit(self, client_id: str, endpoint: str, 
                        method: str = 'sliding_window') -> RateLimitStatus:
        """
        Check if client is within rate limits for endpoint
        
        Args:
            client_id: Unique identifier for the client (IP, user ID, etc.)
            endpoint: Endpoint being accessed
            method: Rate limiting method ('sliding_window' or 'token_bucket')
            
        Returns:
            RateLimitStatus with decision and metadata
        """
        self.stats['total_requests'] += 1
        
        # Check if client is temporarily blocked
        if client_id in self.blocked_clients:
            if datetime.now() < self.blocked_clients[client_id]:
                self.stats['blocked_requests'] += 1
                retry_after = int((self.blocked_clients[client_id] - datetime.now()).total_seconds())
                return RateLimitStatus(
                    allowed=False,
                    remaining_requests=0,
                    reset_time=self.blocked_clients[client_id],
                    retry_after_seconds=retry_after
                )
            else:
                # Unblock client
                del self.blocked_clients[client_id]
        
        # Get rate limit configuration
        rate_limit = self.rate_limits.get(endpoint, self.rate_limits['api_general'])
        
        if method == 'token_bucket':
            return self._check_token_bucket(client_id, endpoint, rate_limit)
        else:
            return self._check_sliding_window(client_id, endpoint, rate_limit)
    
    def _check_sliding_window(self, client_id: str, endpoint: str, 
                            rate_limit: RateLimit) -> RateLimitStatus:
        """Check rate limit using sliding window algorithm"""
        # Get or create counter for this client/endpoint
        if endpoint not in self.client_counters[client_id]:
            self.client_counters[client_id][endpoint] = SlidingWindowCounter(
                rate_limit.window_seconds, rate_limit.requests_per_window
            )
        
        counter = self.client_counters[client_id][endpoint]
        allowed, remaining = counter.is_allowed()
        
        if not allowed:
            self.stats['rate_limited_requests'] += 1
            
            # Check for abuse (multiple rate limit violations)
            self._check_for_abuse(client_id)
        
        reset_time = counter.get_reset_time()
        retry_after = None if allowed else int((reset_time - datetime.now()).total_seconds())
        
        return RateLimitStatus(
            allowed=allowed,
            remaining_requests=remaining,
            reset_time=reset_time,
            retry_after_seconds=retry_after
        )
    
    def _check_token_bucket(self, client_id: str, endpoint: str, 
                          rate_limit: RateLimit) -> RateLimitStatus:
        """Check rate limit using token bucket algorithm"""
        # Get or create bucket for this client/endpoint
        if endpoint not in self.client_buckets[client_id]:
            refill_rate = rate_limit.requests_per_window / rate_limit.window_seconds
            capacity = rate_limit.requests_per_window + rate_limit.burst_allowance
            self.client_buckets[client_id][endpoint] = TokenBucket(capacity, refill_rate)
        
        bucket = self.client_buckets[client_id][endpoint]
        allowed = bucket.consume(1)
        
        if not allowed:
            self.stats['rate_limited_requests'] += 1
            self._check_for_abuse(client_id)
        
        remaining_tokens = int(bucket.get_tokens())
        reset_time = datetime.now() + timedelta(seconds=rate_limit.window_seconds)
        retry_after = None if allowed else rate_limit.window_seconds
        
        return RateLimitStatus(
            allowed=allowed,
            remaining_requests=remaining_tokens,
            reset_time=reset_time,
            retry_after_seconds=retry_after
        )
    
    def _check_for_abuse(self, client_id: str):
        """Check if client should be temporarily blocked for abuse"""
        # Count recent rate limit violations
        now = time.time()
        violation_window = 300  # 5 minutes
        violation_threshold = 10  # 10 violations in 5 minutes = temporary block
        
        # This is a simplified abuse detection
        # In production, you'd want more sophisticated tracking
        
        # For now, we'll implement a simple counter
        if not hasattr(self, 'violation_counts'):
            self.violation_counts = defaultdict(list)
        
        # Add current violation
        self.violation_counts[client_id].append(now)
        
        # Remove old violations
        cutoff_time = now - violation_window
        self.violation_counts[client_id] = [
            t for t in self.violation_counts[client_id] if t > cutoff_time
        ]
        
        # Check if threshold exceeded
        if len(self.violation_counts[client_id]) >= violation_threshold:
            # Block client for 15 minutes
            block_duration = timedelta(minutes=15)
            self.blocked_clients[client_id] = datetime.now() + block_duration
            
            logger.warning(f"Client {client_id} temporarily blocked for rate limit abuse")
    
    def get_client_status(self, client_id: str) -> Dict[str, any]:
        """Get comprehensive status for a client"""
        status = {
            'client_id': client_id,
            'blocked': client_id in self.blocked_clients,
            'endpoints': {}
        }
        
        if client_id in self.blocked_clients:
            status['blocked_until'] = self.blocked_clients[client_id].isoformat()
            status['retry_after_seconds'] = int(
                (self.blocked_clients[client_id] - datetime.now()).total_seconds()
            )
        
        # Get status for each endpoint the client has accessed
        for endpoint in self.client_counters.get(client_id, {}):
            counter = self.client_counters[client_id][endpoint]
            rate_limit = self.rate_limits.get(endpoint, self.rate_limits['api_general'])
            
            # Check current status without consuming
            with counter.lock:
                now = time.time()
                cutoff_time = now - rate_limit.window_seconds
                
                # Count current requests in window
                current_requests = sum(1 for req_time in counter.requests if req_time > cutoff_time)
                remaining = rate_limit.requests_per_window - current_requests
                
                status['endpoints'][endpoint] = {
                    'requests_in_window': current_requests,
                    'remaining_requests': max(0, remaining),
                    'window_seconds': rate_limit.window_seconds,
                    'max_requests': rate_limit.requests_per_window,
                    'reset_time': counter.get_reset_time().isoformat()
                }
        
        return status
    
    def reset_client_limits(self, client_id: str, endpoint: Optional[str] = None):
        """Reset rate limits for a client"""
        if endpoint:
            # Reset specific endpoint
            if client_id in self.client_counters and endpoint in self.client_counters[client_id]:
                del self.client_counters[client_id][endpoint]
            if client_id in self.client_buckets and endpoint in self.client_buckets[client_id]:
                del self.client_buckets[client_id][endpoint]
        else:
            # Reset all endpoints for client
            if client_id in self.client_counters:
                del self.client_counters[client_id]
            if client_id in self.client_buckets:
                del self.client_buckets[client_id]
            if client_id in self.blocked_clients:
                del self.blocked_clients[client_id]
        
        logger.info(f"Reset rate limits for client {client_id}, endpoint: {endpoint or 'all'}")
    
    def update_rate_limit(self, endpoint: str, requests_per_window: int, 
                         window_seconds: int, burst_allowance: int = 0):
        """Update rate limit configuration for an endpoint"""
        self.rate_limits[endpoint] = RateLimit(
            requests_per_window=requests_per_window,
            window_seconds=window_seconds,
            burst_allowance=burst_allowance
        )
        
        logger.info(f"Updated rate limit for {endpoint}: {requests_per_window}/{window_seconds}s")
    
    def get_statistics(self) -> Dict[str, any]:
        """Get rate limiting statistics"""
        total_requests = self.stats['total_requests']
        blocked_rate = (self.stats['blocked_requests'] / total_requests * 100 
                       if total_requests > 0 else 0)
        rate_limited_rate = (self.stats['rate_limited_requests'] / total_requests * 100 
                           if total_requests > 0 else 0)
        
        return {
            'total_requests': total_requests,
            'blocked_requests': self.stats['blocked_requests'],
            'rate_limited_requests': self.stats['rate_limited_requests'],
            'blocked_rate_percent': round(blocked_rate, 2),
            'rate_limited_rate_percent': round(rate_limited_rate, 2),
            'active_clients': len(self.client_counters),
            'blocked_clients': len(self.blocked_clients),
            'configured_endpoints': list(self.rate_limits.keys())
        }
    
    def get_top_clients(self, limit: int = 10) -> List[Dict[str, any]]:
        """Get top clients by request volume"""
        client_request_counts = {}
        
        for client_id, endpoints in self.client_counters.items():
            total_requests = 0
            for endpoint, counter in endpoints.items():
                total_requests += len(counter.requests)
            client_request_counts[client_id] = total_requests
        
        # Sort by request count
        sorted_clients = sorted(client_request_counts.items(), 
                              key=lambda x: x[1], reverse=True)
        
        return [
            {
                'client_id': client_id,
                'total_requests': request_count,
                'blocked': client_id in self.blocked_clients
            }
            for client_id, request_count in sorted_clients[:limit]
        ]
    
    def _cleanup_loop(self):
        """Background cleanup of old data"""
        while True:
            try:
                time.sleep(300)  # Run every 5 minutes
                self._cleanup_old_data()
            except Exception as e:
                logger.error(f"Error in rate limiting cleanup: {str(e)}")
    
    def _cleanup_old_data(self):
        """Clean up old rate limiting data"""
        now = datetime.now()
        
        # Remove expired blocks
        expired_blocks = [
            client_id for client_id, block_time in self.blocked_clients.items()
            if now > block_time
        ]
        
        for client_id in expired_blocks:
            del self.blocked_clients[client_id]
        
        # Clean up old counters (remove clients with no recent activity)
        inactive_threshold = now - timedelta(hours=1)
        inactive_clients = []
        
        for client_id, endpoints in self.client_counters.items():
            # Check if client has any recent activity
            has_recent_activity = False
            for endpoint, counter in endpoints.items():
                if counter.requests and counter.requests[-1] > inactive_threshold.timestamp():
                    has_recent_activity = True
                    break
            
            if not has_recent_activity:
                inactive_clients.append(client_id)
        
        for client_id in inactive_clients:
            if client_id in self.client_counters:
                del self.client_counters[client_id]
            if client_id in self.client_buckets:
                del self.client_buckets[client_id]
        
        if expired_blocks or inactive_clients:
            logger.info(f"Cleaned up {len(expired_blocks)} expired blocks and "
                       f"{len(inactive_clients)} inactive clients")

# Global rate limiting service instance
rate_limiter = RateLimitingService()

# Decorator for automatic rate limiting
def rate_limited(endpoint: str, method: str = 'sliding_window', 
                client_id_func: Optional[callable] = None):
    """Decorator for automatic rate limiting"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # Extract client ID (this would be more sophisticated in production)
            if client_id_func:
                client_id = client_id_func(*args, **kwargs)
            else:
                # Default: use first argument as client ID or 'default'
                client_id = str(args[0]) if args else 'default'
            
            # Check rate limit
            status = rate_limiter.check_rate_limit(client_id, endpoint, method)
            
            if not status.allowed:
                # In a web framework, this would raise an HTTP 429 error
                raise Exception(f"Rate limit exceeded. Retry after {status.retry_after_seconds} seconds")
            
            # Execute function
            return func(*args, **kwargs)
        
        return wrapper
    return decorator

