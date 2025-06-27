"""
Performance Monitoring Service for Masark Engine
Provides real-time monitoring, metrics collection, and performance analysis
"""

import time
import threading
import statistics
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass, field
from collections import defaultdict, deque
import logging

logger = logging.getLogger(__name__)

@dataclass
class PerformanceMetric:
    """Individual performance metric"""
    name: str
    value: float
    timestamp: datetime
    category: str
    metadata: Dict = field(default_factory=dict)

@dataclass
class SystemHealth:
    """System health status"""
    overall_status: str  # healthy, warning, critical
    cpu_usage: float
    memory_usage: float
    database_response_time: float
    active_sessions: int
    error_rate: float
    uptime: timedelta

@dataclass
class PerformanceReport:
    """Performance analysis report"""
    period_start: datetime
    period_end: datetime
    total_assessments: int
    avg_completion_time: float
    success_rate: float
    peak_concurrent_users: int
    system_health: SystemHealth
    bottlenecks: List[str]
    recommendations: List[str]

class PerformanceMonitoringService:
    """
    Service for monitoring system performance and generating insights
    """
    
    def __init__(self, max_metrics_history: int = 10000):
        self.max_metrics_history = max_metrics_history
        self.metrics_history: deque = deque(maxlen=max_metrics_history)
        self.active_sessions: Dict[str, datetime] = {}
        self.session_metrics: Dict[str, List[PerformanceMetric]] = defaultdict(list)
        
        # Performance thresholds
        self.thresholds = {
            'response_time_warning': 2.0,  # seconds
            'response_time_critical': 5.0,  # seconds
            'error_rate_warning': 0.05,  # 5%
            'error_rate_critical': 0.10,  # 10%
            'memory_usage_warning': 0.80,  # 80%
            'memory_usage_critical': 0.90,  # 90%
            'cpu_usage_warning': 0.70,  # 70%
            'cpu_usage_critical': 0.85  # 85%
        }
        
        # Metrics aggregation
        self.metrics_lock = threading.Lock()
        self.start_time = datetime.now()
        
        logger.info("Performance monitoring service initialized")
    
    def record_metric(self, name: str, value: float, category: str = "general", 
                     metadata: Optional[Dict] = None):
        """Record a performance metric"""
        metric = PerformanceMetric(
            name=name,
            value=value,
            timestamp=datetime.now(),
            category=category,
            metadata=metadata or {}
        )
        
        with self.metrics_lock:
            self.metrics_history.append(metric)
        
        logger.debug(f"Recorded metric: {name} = {value} ({category})")
    
    def start_session_tracking(self, session_id: str):
        """Start tracking a session"""
        self.active_sessions[session_id] = datetime.now()
        self.record_metric("session_started", 1, "sessions", {"session_id": session_id})
    
    def end_session_tracking(self, session_id: str, success: bool = True):
        """End tracking a session"""
        if session_id in self.active_sessions:
            start_time = self.active_sessions.pop(session_id)
            duration = (datetime.now() - start_time).total_seconds()
            
            self.record_metric("session_duration", duration, "sessions", 
                             {"session_id": session_id, "success": success})
            self.record_metric("session_completed", 1, "sessions", 
                             {"session_id": session_id, "success": success})
    
    def record_assessment_completion(self, session_id: str, personality_type: str, 
                                   completion_time: float, quality_score: float):
        """Record assessment completion metrics"""
        self.record_metric("assessment_completion_time", completion_time, "assessments",
                         {"session_id": session_id, "personality_type": personality_type})
        self.record_metric("assessment_quality_score", quality_score, "assessments",
                         {"session_id": session_id, "personality_type": personality_type})
    
    def record_career_matching(self, session_id: str, match_count: int, 
                             matching_time: float, avg_match_score: float):
        """Record career matching metrics"""
        self.record_metric("career_matching_time", matching_time, "career_matching",
                         {"session_id": session_id, "match_count": match_count})
        self.record_metric("career_match_count", match_count, "career_matching",
                         {"session_id": session_id})
        self.record_metric("avg_match_score", avg_match_score, "career_matching",
                         {"session_id": session_id})
    
    def record_database_operation(self, operation: str, duration: float, success: bool = True):
        """Record database operation metrics"""
        self.record_metric("database_operation_time", duration, "database",
                         {"operation": operation, "success": success})
    
    def record_api_request(self, endpoint: str, method: str, response_time: float, 
                          status_code: int):
        """Record API request metrics"""
        self.record_metric("api_response_time", response_time, "api",
                         {"endpoint": endpoint, "method": method, "status_code": status_code})
        
        # Record success/error
        success = 200 <= status_code < 400
        self.record_metric("api_request", 1, "api",
                         {"endpoint": endpoint, "method": method, "success": success})
    
    def get_current_system_health(self) -> SystemHealth:
        """Get current system health status"""
        now = datetime.now()
        
        # Calculate metrics from recent data (last 5 minutes)
        recent_cutoff = now - timedelta(minutes=5)
        recent_metrics = [m for m in self.metrics_history if m.timestamp >= recent_cutoff]
        
        # Calculate response times
        response_times = [m.value for m in recent_metrics 
                         if m.name in ["api_response_time", "database_operation_time"]]
        avg_response_time = statistics.mean(response_times) if response_times else 0.0
        
        # Calculate error rate
        api_requests = [m for m in recent_metrics if m.name == "api_request"]
        if api_requests:
            error_count = sum(1 for m in api_requests if not m.metadata.get("success", True))
            error_rate = error_count / len(api_requests)
        else:
            error_rate = 0.0
        
        # Simulate system resource metrics (in production, these would be real)
        cpu_usage = min(0.3 + len(self.active_sessions) * 0.01, 1.0)
        memory_usage = min(0.4 + len(recent_metrics) * 0.0001, 1.0)
        
        # Determine overall status
        if (avg_response_time > self.thresholds['response_time_critical'] or
            error_rate > self.thresholds['error_rate_critical'] or
            cpu_usage > self.thresholds['cpu_usage_critical'] or
            memory_usage > self.thresholds['memory_usage_critical']):
            overall_status = "critical"
        elif (avg_response_time > self.thresholds['response_time_warning'] or
              error_rate > self.thresholds['error_rate_warning'] or
              cpu_usage > self.thresholds['cpu_usage_warning'] or
              memory_usage > self.thresholds['memory_usage_warning']):
            overall_status = "warning"
        else:
            overall_status = "healthy"
        
        return SystemHealth(
            overall_status=overall_status,
            cpu_usage=cpu_usage,
            memory_usage=memory_usage,
            database_response_time=avg_response_time,
            active_sessions=len(self.active_sessions),
            error_rate=error_rate,
            uptime=now - self.start_time
        )
    
    def get_performance_report(self, hours: int = 24) -> PerformanceReport:
        """Generate performance report for the specified time period"""
        end_time = datetime.now()
        start_time = end_time - timedelta(hours=hours)
        
        # Filter metrics for the time period
        period_metrics = [m for m in self.metrics_history 
                         if start_time <= m.timestamp <= end_time]
        
        # Calculate assessment metrics
        assessment_completions = [m for m in period_metrics 
                                if m.name == "session_completed" and 
                                m.metadata.get("success", True)]
        total_assessments = len(assessment_completions)
        
        completion_times = [m.value for m in period_metrics 
                          if m.name == "assessment_completion_time"]
        avg_completion_time = statistics.mean(completion_times) if completion_times else 0.0
        
        # Calculate success rate
        all_sessions = [m for m in period_metrics if m.name == "session_completed"]
        if all_sessions:
            successful_sessions = [m for m in all_sessions if m.metadata.get("success", True)]
            success_rate = len(successful_sessions) / len(all_sessions)
        else:
            success_rate = 1.0
        
        # Calculate peak concurrent users
        session_starts = [m for m in period_metrics if m.name == "session_started"]
        session_ends = [m for m in period_metrics if m.name == "session_completed"]
        
        # Simplified peak calculation
        peak_concurrent_users = max(len(self.active_sessions), 
                                  len(session_starts) - len(session_ends))
        
        # Get current system health
        system_health = self.get_current_system_health()
        
        # Identify bottlenecks
        bottlenecks = self._identify_bottlenecks(period_metrics)
        
        # Generate recommendations
        recommendations = self._generate_recommendations(system_health, bottlenecks)
        
        return PerformanceReport(
            period_start=start_time,
            period_end=end_time,
            total_assessments=total_assessments,
            avg_completion_time=avg_completion_time,
            success_rate=success_rate,
            peak_concurrent_users=peak_concurrent_users,
            system_health=system_health,
            bottlenecks=bottlenecks,
            recommendations=recommendations
        )
    
    def _identify_bottlenecks(self, metrics: List[PerformanceMetric]) -> List[str]:
        """Identify system bottlenecks from metrics"""
        bottlenecks = []
        
        # Check database performance
        db_times = [m.value for m in metrics if m.name == "database_operation_time"]
        if db_times and statistics.mean(db_times) > 1.0:
            bottlenecks.append("Database operations are slow")
        
        # Check API response times
        api_times = [m.value for m in metrics if m.name == "api_response_time"]
        if api_times and statistics.mean(api_times) > 2.0:
            bottlenecks.append("API response times are high")
        
        # Check assessment completion times
        assessment_times = [m.value for m in metrics if m.name == "assessment_completion_time"]
        if assessment_times and statistics.mean(assessment_times) > 300:  # 5 minutes
            bottlenecks.append("Assessment completion times are excessive")
        
        # Check error rates
        api_requests = [m for m in metrics if m.name == "api_request"]
        if api_requests:
            error_count = sum(1 for m in api_requests if not m.metadata.get("success", True))
            error_rate = error_count / len(api_requests)
            if error_rate > 0.05:
                bottlenecks.append(f"High error rate: {error_rate:.1%}")
        
        return bottlenecks
    
    def _generate_recommendations(self, system_health: SystemHealth, 
                                bottlenecks: List[str]) -> List[str]:
        """Generate performance recommendations"""
        recommendations = []
        
        if system_health.overall_status == "critical":
            recommendations.append("URGENT: System is in critical state - immediate attention required")
        elif system_health.overall_status == "warning":
            recommendations.append("System performance is degraded - monitor closely")
        
        if system_health.cpu_usage > 0.8:
            recommendations.append("High CPU usage detected - consider scaling up")
        
        if system_health.memory_usage > 0.8:
            recommendations.append("High memory usage detected - check for memory leaks")
        
        if system_health.database_response_time > 2.0:
            recommendations.append("Database performance is slow - optimize queries or scale database")
        
        if system_health.error_rate > 0.05:
            recommendations.append("High error rate detected - investigate error logs")
        
        if "Database operations are slow" in bottlenecks:
            recommendations.append("Consider database indexing optimization")
        
        if "API response times are high" in bottlenecks:
            recommendations.append("Consider API caching or load balancing")
        
        if not recommendations:
            recommendations.append("System is performing well - no immediate action needed")
        
        return recommendations
    
    def get_real_time_metrics(self, minutes: int = 5) -> Dict[str, any]:
        """Get real-time metrics for the last N minutes"""
        cutoff_time = datetime.now() - timedelta(minutes=minutes)
        recent_metrics = [m for m in self.metrics_history if m.timestamp >= cutoff_time]
        
        # Group metrics by category
        metrics_by_category = defaultdict(list)
        for metric in recent_metrics:
            metrics_by_category[metric.category].append(metric)
        
        # Calculate aggregated metrics
        result = {
            "time_period_minutes": minutes,
            "total_metrics": len(recent_metrics),
            "active_sessions": len(self.active_sessions),
            "categories": {}
        }
        
        for category, category_metrics in metrics_by_category.items():
            # Group by metric name within category
            metrics_by_name = defaultdict(list)
            for metric in category_metrics:
                metrics_by_name[metric.name].append(metric.value)
            
            category_summary = {}
            for name, values in metrics_by_name.items():
                if values:
                    category_summary[name] = {
                        "count": len(values),
                        "avg": statistics.mean(values),
                        "min": min(values),
                        "max": max(values),
                        "latest": values[-1] if values else 0
                    }
            
            result["categories"][category] = category_summary
        
        return result
    
    def export_metrics(self, start_time: Optional[datetime] = None, 
                      end_time: Optional[datetime] = None) -> List[Dict]:
        """Export metrics for external analysis"""
        if start_time is None:
            start_time = datetime.now() - timedelta(hours=24)
        if end_time is None:
            end_time = datetime.now()
        
        filtered_metrics = [
            m for m in self.metrics_history 
            if start_time <= m.timestamp <= end_time
        ]
        
        return [
            {
                "name": m.name,
                "value": m.value,
                "timestamp": m.timestamp.isoformat(),
                "category": m.category,
                "metadata": m.metadata
            }
            for m in filtered_metrics
        ]
    
    def clear_old_metrics(self, days: int = 7):
        """Clear metrics older than specified days"""
        cutoff_time = datetime.now() - timedelta(days=days)
        
        with self.metrics_lock:
            # Convert to list to avoid modifying deque during iteration
            metrics_to_keep = [m for m in self.metrics_history if m.timestamp >= cutoff_time]
            self.metrics_history.clear()
            self.metrics_history.extend(metrics_to_keep)
        
        logger.info(f"Cleared metrics older than {days} days")
    
    def get_performance_dashboard_data(self) -> Dict[str, any]:
        """Get data for performance dashboard"""
        system_health = self.get_current_system_health()
        real_time_metrics = self.get_real_time_metrics(5)
        recent_report = self.get_performance_report(1)  # Last hour
        
        return {
            "system_health": {
                "status": system_health.overall_status,
                "cpu_usage": system_health.cpu_usage,
                "memory_usage": system_health.memory_usage,
                "database_response_time": system_health.database_response_time,
                "active_sessions": system_health.active_sessions,
                "error_rate": system_health.error_rate,
                "uptime_hours": system_health.uptime.total_seconds() / 3600
            },
            "real_time_metrics": real_time_metrics,
            "hourly_summary": {
                "total_assessments": recent_report.total_assessments,
                "avg_completion_time": recent_report.avg_completion_time,
                "success_rate": recent_report.success_rate,
                "peak_concurrent_users": recent_report.peak_concurrent_users
            },
            "alerts": {
                "bottlenecks": recent_report.bottlenecks,
                "recommendations": recent_report.recommendations
            },
            "timestamp": datetime.now().isoformat()
        }

# Global instance for application-wide use
performance_monitor = PerformanceMonitoringService()

# Decorator for automatic performance monitoring
def monitor_performance(category: str = "general", record_args: bool = False):
    """Decorator to automatically monitor function performance"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            start_time = time.time()
            success = True
            error = None
            
            try:
                result = func(*args, **kwargs)
                return result
            except Exception as e:
                success = False
                error = str(e)
                raise
            finally:
                duration = time.time() - start_time
                metadata = {
                    "function": func.__name__,
                    "success": success
                }
                
                if error:
                    metadata["error"] = error
                
                if record_args and args:
                    metadata["args_count"] = len(args)
                
                performance_monitor.record_metric(
                    f"{func.__name__}_execution_time",
                    duration,
                    category,
                    metadata
                )
        
        return wrapper
    return decorator

