"""
Production-Grade Caching Service for Masark Engine
Implements intelligent caching for improved performance and scalability
"""

import json
import time
import hashlib
from typing import Any, Optional, Dict, List, Callable
from datetime import datetime, timedelta
from dataclasses import dataclass
import threading
import logging

logger = logging.getLogger(__name__)

@dataclass
class CacheEntry:
    """Cache entry with metadata"""
    key: str
    value: Any
    created_at: datetime
    expires_at: Optional[datetime]
    access_count: int = 0
    last_accessed: Optional[datetime] = None
    size_bytes: int = 0

class InMemoryCache:
    """High-performance in-memory cache with TTL and LRU eviction"""
    
    def __init__(self, max_size: int = 10000, default_ttl: int = 3600):
        self.max_size = max_size
        self.default_ttl = default_ttl
        self.cache: Dict[str, CacheEntry] = {}
        self.access_order: List[str] = []  # For LRU tracking
        self.lock = threading.RLock()
        self.stats = {
            'hits': 0,
            'misses': 0,
            'evictions': 0,
            'size': 0
        }
    
    def get(self, key: str) -> Optional[Any]:
        """Get value from cache"""
        with self.lock:
            if key not in self.cache:
                self.stats['misses'] += 1
                return None
            
            entry = self.cache[key]
            
            # Check if expired
            if entry.expires_at and datetime.now() > entry.expires_at:
                self._remove_entry(key)
                self.stats['misses'] += 1
                return None
            
            # Update access statistics
            entry.access_count += 1
            entry.last_accessed = datetime.now()
            
            # Update LRU order
            if key in self.access_order:
                self.access_order.remove(key)
            self.access_order.append(key)
            
            self.stats['hits'] += 1
            return entry.value
    
    def set(self, key: str, value: Any, ttl: Optional[int] = None) -> bool:
        """Set value in cache"""
        with self.lock:
            # Calculate expiration
            if ttl is None:
                ttl = self.default_ttl
            
            expires_at = datetime.now() + timedelta(seconds=ttl) if ttl > 0 else None
            
            # Calculate size (rough estimate)
            size_bytes = len(str(value).encode('utf-8'))
            
            # Create cache entry
            entry = CacheEntry(
                key=key,
                value=value,
                created_at=datetime.now(),
                expires_at=expires_at,
                size_bytes=size_bytes
            )
            
            # Check if we need to evict entries
            if len(self.cache) >= self.max_size and key not in self.cache:
                self._evict_lru()
            
            # Store entry
            self.cache[key] = entry
            
            # Update access order
            if key in self.access_order:
                self.access_order.remove(key)
            self.access_order.append(key)
            
            self.stats['size'] = len(self.cache)
            return True
    
    def delete(self, key: str) -> bool:
        """Delete key from cache"""
        with self.lock:
            if key in self.cache:
                self._remove_entry(key)
                return True
            return False
    
    def clear(self):
        """Clear all cache entries"""
        with self.lock:
            self.cache.clear()
            self.access_order.clear()
            self.stats['size'] = 0
    
    def _remove_entry(self, key: str):
        """Remove entry and update tracking"""
        if key in self.cache:
            del self.cache[key]
        if key in self.access_order:
            self.access_order.remove(key)
        self.stats['size'] = len(self.cache)
    
    def _evict_lru(self):
        """Evict least recently used entry"""
        if self.access_order:
            lru_key = self.access_order[0]
            self._remove_entry(lru_key)
            self.stats['evictions'] += 1
    
    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        with self.lock:
            total_requests = self.stats['hits'] + self.stats['misses']
            hit_rate = self.stats['hits'] / total_requests if total_requests > 0 else 0
            
            return {
                'hits': self.stats['hits'],
                'misses': self.stats['misses'],
                'hit_rate': hit_rate,
                'evictions': self.stats['evictions'],
                'size': self.stats['size'],
                'max_size': self.max_size
            }

class CachingService:
    """
    Production-grade caching service with multiple cache layers
    """
    
    def __init__(self):
        # Different cache instances for different data types
        self.question_cache = InMemoryCache(max_size=1000, default_ttl=3600)  # 1 hour
        self.career_cache = InMemoryCache(max_size=5000, default_ttl=1800)   # 30 minutes
        self.personality_cache = InMemoryCache(max_size=1000, default_ttl=7200)  # 2 hours
        self.session_cache = InMemoryCache(max_size=10000, default_ttl=1800)  # 30 minutes
        self.report_cache = InMemoryCache(max_size=2000, default_ttl=900)    # 15 minutes
        
        # Cache warming status
        self.cache_warmed = False
        self.warming_in_progress = False
        
        logger.info("Caching service initialized")
    
    def get_questions(self, language: str = 'en') -> Optional[List[Dict]]:
        """Get cached questions"""
        cache_key = f"questions_{language}"
        return self.question_cache.get(cache_key)
    
    def cache_questions(self, questions: List[Dict], language: str = 'en'):
        """Cache questions"""
        cache_key = f"questions_{language}"
        self.question_cache.set(cache_key, questions, ttl=3600)  # 1 hour
    
    def get_personality_types(self, language: str = 'en') -> Optional[List[Dict]]:
        """Get cached personality types"""
        cache_key = f"personality_types_{language}"
        return self.personality_cache.get(cache_key)
    
    def cache_personality_types(self, personality_types: List[Dict], language: str = 'en'):
        """Cache personality types"""
        cache_key = f"personality_types_{language}"
        self.personality_cache.set(cache_key, personality_types, ttl=7200)  # 2 hours
    
    def get_career_matches(self, personality_type: str, filters: Dict = None) -> Optional[List[Dict]]:
        """Get cached career matches"""
        cache_key = self._generate_career_cache_key(personality_type, filters)
        return self.career_cache.get(cache_key)
    
    def cache_career_matches(self, personality_type: str, matches: List[Dict], 
                           filters: Dict = None):
        """Cache career matches"""
        cache_key = self._generate_career_cache_key(personality_type, filters)
        self.career_cache.set(cache_key, matches, ttl=1800)  # 30 minutes
    
    def get_session_data(self, session_id: str) -> Optional[Dict]:
        """Get cached session data"""
        cache_key = f"session_{session_id}"
        return self.session_cache.get(cache_key)
    
    def cache_session_data(self, session_id: str, session_data: Dict):
        """Cache session data"""
        cache_key = f"session_{session_id}"
        self.session_cache.set(cache_key, session_data, ttl=1800)  # 30 minutes
    
    def get_assessment_report(self, session_id: str, report_type: str) -> Optional[Dict]:
        """Get cached assessment report"""
        cache_key = f"report_{session_id}_{report_type}"
        return self.report_cache.get(cache_key)
    
    def cache_assessment_report(self, session_id: str, report_type: str, report_data: Dict):
        """Cache assessment report"""
        cache_key = f"report_{session_id}_{report_type}"
        self.report_cache.set(cache_key, report_data, ttl=900)  # 15 minutes
    
    def _generate_career_cache_key(self, personality_type: str, filters: Dict = None) -> str:
        """Generate cache key for career matches"""
        key_parts = [f"careers_{personality_type}"]
        
        if filters:
            # Sort filters for consistent key generation
            sorted_filters = sorted(filters.items())
            filter_str = json.dumps(sorted_filters, sort_keys=True)
            filter_hash = hashlib.md5(filter_str.encode()).hexdigest()[:8]
            key_parts.append(filter_hash)
        
        return "_".join(key_parts)
    
    def warm_cache(self):
        """Warm up cache with frequently accessed data"""
        if self.warming_in_progress:
            return
        
        self.warming_in_progress = True
        
        try:
            logger.info("Starting cache warming...")
            
            # This would normally load data from database
            # For now, we'll simulate the warming process
            
            # Warm questions cache
            self._warm_questions_cache()
            
            # Warm personality types cache
            self._warm_personality_types_cache()
            
            # Warm career data cache
            self._warm_career_cache()
            
            self.cache_warmed = True
            logger.info("Cache warming completed successfully")
            
        except Exception as e:
            logger.error(f"Cache warming failed: {str(e)}")
        finally:
            self.warming_in_progress = False
    
    def _warm_questions_cache(self):
        """Warm questions cache"""
        # Simulate loading questions for both languages
        for language in ['en', 'ar']:
            cache_key = f"questions_{language}"
            if not self.question_cache.get(cache_key):
                # In production, this would load from database
                questions = [{"id": i, "text": f"Question {i}"} for i in range(1, 37)]
                self.question_cache.set(cache_key, questions)
    
    def _warm_personality_types_cache(self):
        """Warm personality types cache"""
        # Simulate loading personality types
        for language in ['en', 'ar']:
            cache_key = f"personality_types_{language}"
            if not self.personality_cache.get(cache_key):
                # In production, this would load from database
                types = [{"code": f"TYPE{i}", "name": f"Type {i}"} for i in range(1, 17)]
                self.personality_cache.set(cache_key, types)
    
    def _warm_career_cache(self):
        """Warm career cache for common personality types"""
        common_types = ["INTJ", "ENFP", "ISTJ", "ESFP"]
        
        for personality_type in common_types:
            cache_key = f"careers_{personality_type}"
            if not self.career_cache.get(cache_key):
                # In production, this would load from database
                careers = [{"id": i, "name": f"Career {i}"} for i in range(1, 21)]
                self.career_cache.set(cache_key, careers)
    
    def invalidate_cache(self, cache_type: str = None, pattern: str = None):
        """Invalidate cache entries"""
        if cache_type == "questions":
            self.question_cache.clear()
        elif cache_type == "careers":
            self.career_cache.clear()
        elif cache_type == "personality":
            self.personality_cache.clear()
        elif cache_type == "sessions":
            self.session_cache.clear()
        elif cache_type == "reports":
            self.report_cache.clear()
        elif cache_type is None:
            # Clear all caches
            self.question_cache.clear()
            self.career_cache.clear()
            self.personality_cache.clear()
            self.session_cache.clear()
            self.report_cache.clear()
        
        logger.info(f"Cache invalidated: {cache_type or 'all'}")
    
    def get_cache_statistics(self) -> Dict[str, Any]:
        """Get comprehensive cache statistics"""
        return {
            "questions": self.question_cache.get_stats(),
            "careers": self.career_cache.get_stats(),
            "personality": self.personality_cache.get_stats(),
            "sessions": self.session_cache.get_stats(),
            "reports": self.report_cache.get_stats(),
            "cache_warmed": self.cache_warmed,
            "warming_in_progress": self.warming_in_progress
        }
    
    def cleanup_expired_entries(self):
        """Clean up expired cache entries"""
        caches = [
            ("questions", self.question_cache),
            ("careers", self.career_cache),
            ("personality", self.personality_cache),
            ("sessions", self.session_cache),
            ("reports", self.report_cache)
        ]
        
        total_cleaned = 0
        
        for cache_name, cache in caches:
            cleaned = self._cleanup_cache(cache)
            total_cleaned += cleaned
            if cleaned > 0:
                logger.debug(f"Cleaned {cleaned} expired entries from {cache_name} cache")
        
        if total_cleaned > 0:
            logger.info(f"Cleaned up {total_cleaned} expired cache entries")
    
    def _cleanup_cache(self, cache: InMemoryCache) -> int:
        """Clean up expired entries from a specific cache"""
        with cache.lock:
            now = datetime.now()
            expired_keys = []
            
            for key, entry in cache.cache.items():
                if entry.expires_at and now > entry.expires_at:
                    expired_keys.append(key)
            
            for key in expired_keys:
                cache._remove_entry(key)
            
            return len(expired_keys)

# Global caching service instance
cache_service = CachingService()

# Decorator for automatic caching
def cached(cache_type: str = "general", ttl: int = 300, key_func: Optional[Callable] = None):
    """Decorator for automatic function result caching"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            # Generate cache key
            if key_func:
                cache_key = key_func(*args, **kwargs)
            else:
                # Default key generation
                key_parts = [func.__name__]
                if args:
                    key_parts.extend([str(arg) for arg in args])
                if kwargs:
                    key_parts.extend([f"{k}={v}" for k, v in sorted(kwargs.items())])
                cache_key = "_".join(key_parts)
                cache_key = hashlib.md5(cache_key.encode()).hexdigest()
            
            # Try to get from cache
            if cache_type == "questions":
                cached_result = cache_service.question_cache.get(cache_key)
            elif cache_type == "careers":
                cached_result = cache_service.career_cache.get(cache_key)
            elif cache_type == "personality":
                cached_result = cache_service.personality_cache.get(cache_key)
            else:
                cached_result = cache_service.session_cache.get(cache_key)
            
            if cached_result is not None:
                return cached_result
            
            # Execute function and cache result
            result = func(*args, **kwargs)
            
            # Cache the result
            if cache_type == "questions":
                cache_service.question_cache.set(cache_key, result, ttl)
            elif cache_type == "careers":
                cache_service.career_cache.set(cache_key, result, ttl)
            elif cache_type == "personality":
                cache_service.personality_cache.set(cache_key, result, ttl)
            else:
                cache_service.session_cache.set(cache_key, result, ttl)
            
            return result
        
        return wrapper
    return decorator

