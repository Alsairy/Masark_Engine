using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Runtime.InteropServices;
using System.IO;

namespace Masark.Application.Services
{
    public class PerformanceMetrics
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    public class SystemHealth
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryTotalBytes { get; set; }
        public double MemoryUsagePercent { get; set; }
        public long DiskUsedBytes { get; set; }
        public long DiskTotalBytes { get; set; }
        public double DiskUsagePercent { get; set; }
        public int ActiveConnections { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "healthy";
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class PerformanceReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, double> AverageMetrics { get; set; } = new();
        public Dictionary<string, double> MaxMetrics { get; set; } = new();
        public Dictionary<string, double> MinMetrics { get; set; } = new();
        public Dictionary<string, int> RequestCounts { get; set; } = new();
        public Dictionary<string, double> ResponseTimes { get; set; } = new();
        public List<string> TopBottlenecks { get; set; } = new();
        public SystemHealth SystemHealthSummary { get; set; } = new();
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    public class SessionMetrics
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
        public int QuestionsAnswered { get; set; }
        public int TotalQuestions { get; set; }
        public double CompletionPercentage => TotalQuestions > 0 ? (double)QuestionsAnswered / TotalQuestions * 100 : 0;
        public List<TimeSpan> QuestionResponseTimes { get; set; } = new();
        public TimeSpan AverageResponseTime => QuestionResponseTimes.Count > 0 
            ? TimeSpan.FromMilliseconds(QuestionResponseTimes.Average(t => t.TotalMilliseconds)) 
            : TimeSpan.Zero;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public interface IPerformanceMonitoringService
    {
        Task RecordMetricAsync(string name, double value, string unit = "", Dictionary<string, object>? tags = null);
        Task RecordRequestMetricAsync(string endpoint, TimeSpan responseTime, int statusCode);
        Task RecordDatabaseMetricAsync(string operation, TimeSpan executionTime, bool success);
        Task RecordCacheMetricAsync(string operation, bool hit, TimeSpan? responseTime = null);
        Task RecordSecurityMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null);

        Task<SystemHealth> GetSystemHealthAsync();
        Task<Dictionary<string, object>> GetSystemMetricsAsync();
        Task<bool> IsSystemHealthyAsync();

        Task StartSessionTrackingAsync(string sessionId, Dictionary<string, object>? metadata = null);
        Task UpdateSessionProgressAsync(string sessionId, int questionsAnswered, int totalQuestions);
        Task RecordQuestionResponseTimeAsync(string sessionId, TimeSpan responseTime);
        Task EndSessionTrackingAsync(string sessionId);
        Task<SessionMetrics?> GetSessionMetricsAsync(string sessionId);

        Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime startTime, DateTime endTime);
        Task<List<string>> IdentifyBottlenecksAsync(TimeSpan timeWindow);
        Task<Dictionary<string, double>> GetAverageMetricsAsync(string metricName, TimeSpan timeWindow);

        Task<List<string>> GetActiveAlertsAsync();
        Task CheckPerformanceThresholdsAsync();
        Task<Dictionary<string, object>> GetPerformanceStatsAsync();

        Task CleanupOldMetricsAsync(TimeSpan retentionPeriod);
        Task<Dictionary<string, object>> GetMonitoringStatusAsync();
    }

    public class PerformanceMonitoringService : IPerformanceMonitoringService, IHostedService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly ConcurrentDictionary<string, List<PerformanceMetrics>> _metrics;
        private readonly ConcurrentDictionary<string, SessionMetrics> _sessionMetrics;
        private readonly ConcurrentDictionary<string, SecurityMetricData> _securityMetrics;
        private readonly Timer _systemHealthTimer;
        private readonly Timer _cleanupTimer;
        private readonly Timer _reportingTimer;
        private readonly Process _currentProcess;
        private readonly DateTime _startTime;
        private SystemHealth _lastSystemHealth;

        private readonly Dictionary<string, double> _performanceThresholds = new()
        {
            ["cpu_usage_percent"] = 80.0,
            ["memory_usage_percent"] = 85.0,
            ["disk_usage_percent"] = 90.0,
            ["response_time_ms"] = 5000.0,
            ["error_rate_percent"] = 5.0
        };

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _metrics = new ConcurrentDictionary<string, List<PerformanceMetrics>>();
            _sessionMetrics = new ConcurrentDictionary<string, SessionMetrics>();
            _securityMetrics = new ConcurrentDictionary<string, SecurityMetricData>();
            _currentProcess = Process.GetCurrentProcess();
            _startTime = DateTime.UtcNow;
            _lastSystemHealth = new SystemHealth();

            _systemHealthTimer = new Timer(CollectSystemHealth, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromHours(1), TimeSpan.FromHours(6));
            _reportingTimer = new Timer(ReportMetricsToApplicationInsights, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public async Task RecordMetricAsync(string name, double value, string unit = "", Dictionary<string, object>? tags = null)
        {
            try
            {
                var metric = new PerformanceMetrics
                {
                    MetricName = name,
                    Value = value,
                    Unit = unit,
                    Timestamp = DateTime.UtcNow,
                    Tags = tags ?? new Dictionary<string, object>()
                };

                _metrics.AddOrUpdate(name, 
                    new List<PerformanceMetrics> { metric },
                    (key, existingList) =>
                    {
                        lock (existingList)
                        {
                            existingList.Add(metric);
                            if (existingList.Count > 1000)
                            {
                                existingList.RemoveRange(0, existingList.Count - 1000);
                            }
                        }
                        return existingList;
                    });

                var telemetryMetric = _telemetryClient.GetMetric(name);
                if (tags != null && tags.Any())
                {
                    var stringTags = tags.Where(kvp => kvp.Value != null)
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
                    
                    if (stringTags.Count == 1)
                    {
                        var firstTag = stringTags.First();
                        telemetryMetric.TrackValue(value, firstTag.Value);
                    }
                    else if (stringTags.Count >= 2)
                    {
                        var tagArray = stringTags.Take(2).ToArray();
                        telemetryMetric.TrackValue(value, tagArray[0].Value, tagArray[1].Value);
                    }
                    else
                    {
                        telemetryMetric.TrackValue(value);
                    }
                }
                else
                {
                    telemetryMetric.TrackValue(value);
                }

                _logger.LogDebug("Recorded metric {MetricName}: {Value} {Unit}", name, value, unit);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording metric {MetricName}", name);
            }
        }

        public async Task RecordRequestMetricAsync(string endpoint, TimeSpan responseTime, int statusCode)
        {
            var tags = new Dictionary<string, object>
            {
                ["endpoint"] = endpoint,
                ["status_code"] = statusCode,
                ["is_success"] = statusCode >= 200 && statusCode < 400
            };

            await RecordMetricAsync("request_response_time", responseTime.TotalMilliseconds, "ms", tags);
            await RecordMetricAsync("request_count", 1, "count", tags);

            if (statusCode >= 400)
            {
                await RecordMetricAsync("error_count", 1, "count", tags);
            }

            var requestTelemetry = new RequestTelemetry(
                endpoint, 
                DateTimeOffset.UtcNow.Subtract(responseTime), 
                responseTime, 
                statusCode.ToString(), 
                statusCode >= 200 && statusCode < 400);
            
            requestTelemetry.Properties["endpoint"] = endpoint;
            requestTelemetry.Properties["status_code"] = statusCode.ToString();
            _telemetryClient.TrackRequest(requestTelemetry);
        }

        public async Task RecordDatabaseMetricAsync(string operation, TimeSpan executionTime, bool success)
        {
            var tags = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["success"] = success
            };

            await RecordMetricAsync("database_execution_time", executionTime.TotalMilliseconds, "ms", tags);
            await RecordMetricAsync("database_operation_count", 1, "count", tags);

            if (!success)
            {
                await RecordMetricAsync("database_error_count", 1, "count", tags);
            }
        }

        public async Task RecordCacheMetricAsync(string operation, bool hit, TimeSpan? responseTime = null)
        {
            var tags = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["hit"] = hit
            };

            await RecordMetricAsync("cache_operation_count", 1, "count", tags);
            await RecordMetricAsync(hit ? "cache_hit_count" : "cache_miss_count", 1, "count", tags);

            if (responseTime.HasValue)
            {
                await RecordMetricAsync("cache_response_time", responseTime.Value.TotalMilliseconds, "ms", tags);
            }
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            try
            {
                var health = new SystemHealth
                {
                    Timestamp = DateTime.UtcNow,
                    Uptime = DateTime.UtcNow - _startTime
                };

                health.CpuUsagePercent = await GetCpuUsageAsync();

                var memoryInfo = GC.GetGCMemoryInfo();
                health.MemoryUsedBytes = GC.GetTotalMemory(false);
                health.MemoryTotalBytes = memoryInfo.TotalAvailableMemoryBytes > 0 
                    ? memoryInfo.TotalAvailableMemoryBytes 
                    : health.MemoryUsedBytes * 4; // Rough estimate
                health.MemoryUsagePercent = health.MemoryTotalBytes > 0 
                    ? (double)health.MemoryUsedBytes / health.MemoryTotalBytes * 100 
                    : 0;

                var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
                if (driveInfo.IsReady)
                {
                    health.DiskTotalBytes = driveInfo.TotalSize;
                    health.DiskUsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
                    health.DiskUsagePercent = (double)health.DiskUsedBytes / health.DiskTotalBytes * 100;
                }

                health.ActiveConnections = _sessionMetrics.Count;

                health.Status = DetermineHealthStatus(health);
                health.Warnings = GetHealthWarnings(health);
                health.Errors = GetHealthErrors(health);

                _lastSystemHealth = health;
                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return _lastSystemHealth;
            }
        }

        public async Task<Dictionary<string, object>> GetSystemMetricsAsync()
        {
            var health = await GetSystemHealthAsync();
            
            return new Dictionary<string, object>
            {
                ["cpu_usage_percent"] = health.CpuUsagePercent,
                ["memory_usage_percent"] = health.MemoryUsagePercent,
                ["memory_used_bytes"] = health.MemoryUsedBytes,
                ["memory_total_bytes"] = health.MemoryTotalBytes,
                ["disk_usage_percent"] = health.DiskUsagePercent,
                ["disk_used_bytes"] = health.DiskUsedBytes,
                ["disk_total_bytes"] = health.DiskTotalBytes,
                ["active_connections"] = health.ActiveConnections,
                ["uptime_seconds"] = health.Uptime.TotalSeconds,
                ["status"] = health.Status,
                ["warnings_count"] = health.Warnings.Count,
                ["errors_count"] = health.Errors.Count,
                ["timestamp"] = health.Timestamp
            };
        }

        public async Task<bool> IsSystemHealthyAsync()
        {
            var health = await GetSystemHealthAsync();
            return health.Status == "healthy";
        }

        public async Task StartSessionTrackingAsync(string sessionId, Dictionary<string, object>? metadata = null)
        {
            var sessionMetrics = new SessionMetrics
            {
                SessionId = sessionId,
                StartTime = DateTime.UtcNow,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            _sessionMetrics.AddOrUpdate(sessionId, sessionMetrics, (key, existing) => sessionMetrics);
            
            await RecordMetricAsync("session_started", 1, "count", new Dictionary<string, object> { ["session_id"] = sessionId });
            _logger.LogDebug("Started tracking session {SessionId}", sessionId);
        }

        public async Task UpdateSessionProgressAsync(string sessionId, int questionsAnswered, int totalQuestions)
        {
            if (_sessionMetrics.TryGetValue(sessionId, out var session))
            {
                session.QuestionsAnswered = questionsAnswered;
                session.TotalQuestions = totalQuestions;
                
                await RecordMetricAsync("session_progress", session.CompletionPercentage, "percent", 
                    new Dictionary<string, object> { ["session_id"] = sessionId });
            }
        }

        public async Task RecordQuestionResponseTimeAsync(string sessionId, TimeSpan responseTime)
        {
            if (_sessionMetrics.TryGetValue(sessionId, out var session))
            {
                session.QuestionResponseTimes.Add(responseTime);
                
                await RecordMetricAsync("question_response_time", responseTime.TotalMilliseconds, "ms", 
                    new Dictionary<string, object> { ["session_id"] = sessionId });
            }
        }

        public async Task EndSessionTrackingAsync(string sessionId)
        {
            if (_sessionMetrics.TryGetValue(sessionId, out var session))
            {
                session.EndTime = DateTime.UtcNow;
                
                await RecordMetricAsync("session_completed", 1, "count", new Dictionary<string, object> 
                { 
                    ["session_id"] = sessionId,
                    ["duration_seconds"] = session.Duration.TotalSeconds,
                    ["completion_percentage"] = session.CompletionPercentage
                });
                
                _logger.LogInformation("Session {SessionId} completed in {Duration} with {Completion}% completion", 
                    sessionId, session.Duration, session.CompletionPercentage);
            }
        }

        public async Task<SessionMetrics?> GetSessionMetricsAsync(string sessionId)
        {
            _sessionMetrics.TryGetValue(sessionId, out var session);
            return await Task.FromResult(session);
        }

        public async Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime startTime, DateTime endTime)
        {
            var report = new PerformanceReport
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = endTime - startTime,
                SystemHealthSummary = await GetSystemHealthAsync()
            };

            foreach (var metricGroup in _metrics)
            {
                var relevantMetrics = metricGroup.Value
                    .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                    .ToList();

                if (relevantMetrics.Any())
                {
                    report.AverageMetrics[metricGroup.Key] = relevantMetrics.Average(m => m.Value);
                    report.MaxMetrics[metricGroup.Key] = relevantMetrics.Max(m => m.Value);
                    report.MinMetrics[metricGroup.Key] = relevantMetrics.Min(m => m.Value);
                }
            }

            if (_metrics.ContainsKey("request_count"))
            {
                var requestMetrics = _metrics["request_count"]
                    .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                    .ToList();

                foreach (var metric in requestMetrics)
                {
                    if (metric.Tags.ContainsKey("endpoint"))
                    {
                        var endpoint = metric.Tags["endpoint"].ToString() ?? "unknown";
                        report.RequestCounts[endpoint] = report.RequestCounts.GetValueOrDefault(endpoint, 0) + 1;
                    }
                }
            }

            if (_metrics.ContainsKey("request_response_time"))
            {
                var responseTimeMetrics = _metrics["request_response_time"]
                    .Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime)
                    .ToList();

                var endpointResponseTimes = responseTimeMetrics
                    .Where(m => m.Tags.ContainsKey("endpoint"))
                    .GroupBy(m => m.Tags["endpoint"].ToString())
                    .ToDictionary(g => g.Key ?? "unknown", g => g.Average(m => m.Value));

                report.ResponseTimes = endpointResponseTimes;
            }

            report.TopBottlenecks = await IdentifyBottlenecksAsync(report.Duration);

            return report;
        }

        public async Task<List<string>> IdentifyBottlenecksAsync(TimeSpan timeWindow)
        {
            var bottlenecks = new List<string>();
            var cutoffTime = DateTime.UtcNow - timeWindow;

            try
            {
                if (_metrics.ContainsKey("request_response_time"))
                {
                    var recentResponseTimes = _metrics["request_response_time"]
                        .Where(m => m.Timestamp >= cutoffTime)
                        .ToList();

                    if (recentResponseTimes.Any())
                    {
                        var avgResponseTime = recentResponseTimes.Average(m => m.Value);
                        if (avgResponseTime > _performanceThresholds["response_time_ms"])
                        {
                            bottlenecks.Add($"High average response time: {avgResponseTime:F2}ms");
                        }
                    }
                }

                if (_metrics.ContainsKey("database_execution_time"))
                {
                    var recentDbTimes = _metrics["database_execution_time"]
                        .Where(m => m.Timestamp >= cutoffTime)
                        .ToList();

                    if (recentDbTimes.Any())
                    {
                        var avgDbTime = recentDbTimes.Average(m => m.Value);
                        if (avgDbTime > 1000) // 1 second threshold
                        {
                            bottlenecks.Add($"Slow database operations: {avgDbTime:F2}ms average");
                        }
                    }
                }

                var health = await GetSystemHealthAsync();
                if (health.CpuUsagePercent > _performanceThresholds["cpu_usage_percent"])
                {
                    bottlenecks.Add($"High CPU usage: {health.CpuUsagePercent:F1}%");
                }

                if (health.MemoryUsagePercent > _performanceThresholds["memory_usage_percent"])
                {
                    bottlenecks.Add($"High memory usage: {health.MemoryUsagePercent:F1}%");
                }

                return bottlenecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying bottlenecks");
                return bottlenecks;
            }
        }

        public async Task<Dictionary<string, double>> GetAverageMetricsAsync(string metricName, TimeSpan timeWindow)
        {
            var result = new Dictionary<string, double>();
            var cutoffTime = DateTime.UtcNow - timeWindow;

            if (_metrics.ContainsKey(metricName))
            {
                var recentMetrics = _metrics[metricName]
                    .Where(m => m.Timestamp >= cutoffTime)
                    .ToList();

                if (recentMetrics.Any())
                {
                    result["average"] = recentMetrics.Average(m => m.Value);
                    result["minimum"] = recentMetrics.Min(m => m.Value);
                    result["maximum"] = recentMetrics.Max(m => m.Value);
                    result["count"] = recentMetrics.Count;
                }
            }

            return await Task.FromResult(result);
        }

        public async Task<List<string>> GetActiveAlertsAsync()
        {
            var alerts = new List<string>();
            var health = await GetSystemHealthAsync();

            alerts.AddRange(health.Warnings);
            alerts.AddRange(health.Errors);

            return alerts;
        }

        public async Task CheckPerformanceThresholdsAsync()
        {
            try
            {
                var health = await GetSystemHealthAsync();
                var alerts = new List<string>();

                foreach (var threshold in _performanceThresholds)
                {
                    var currentValue = threshold.Key switch
                    {
                        "cpu_usage_percent" => health.CpuUsagePercent,
                        "memory_usage_percent" => health.MemoryUsagePercent,
                        "disk_usage_percent" => health.DiskUsagePercent,
                        _ => 0.0
                    };

                    if (currentValue > threshold.Value)
                    {
                        var alert = $"Performance threshold exceeded: {threshold.Key} = {currentValue:F2} (threshold: {threshold.Value})";
                        alerts.Add(alert);
                        _logger.LogWarning(alert);
                    }
                }

                if (alerts.Any())
                {
                    await RecordMetricAsync("performance_alerts", alerts.Count, "count");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking performance thresholds");
            }
        }

        public async Task<Dictionary<string, object>> GetPerformanceStatsAsync()
        {
            var stats = new Dictionary<string, object>();
            var health = await GetSystemHealthAsync();

            stats["system_health"] = health;
            stats["total_metrics_collected"] = _metrics.Values.Sum(list => list.Count);
            stats["active_sessions"] = _sessionMetrics.Count;
            stats["uptime_seconds"] = (DateTime.UtcNow - _startTime).TotalSeconds;
            stats["memory_usage_mb"] = GC.GetTotalMemory(false) / (1024 * 1024);
            stats["gc_collections"] = new Dictionary<string, int>
            {
                ["gen0"] = GC.CollectionCount(0),
                ["gen1"] = GC.CollectionCount(1),
                ["gen2"] = GC.CollectionCount(2)
            };

            return stats;
        }

        public async Task CleanupOldMetricsAsync(TimeSpan retentionPeriod)
        {
            var cutoffTime = DateTime.UtcNow - retentionPeriod;
            var totalRemoved = 0;

            foreach (var metricGroup in _metrics.ToList())
            {
                lock (metricGroup.Value)
                {
                    var initialCount = metricGroup.Value.Count;
                    metricGroup.Value.RemoveAll(m => m.Timestamp < cutoffTime);
                    totalRemoved += initialCount - metricGroup.Value.Count;
                }
            }

            var oldSessions = _sessionMetrics
                .Where(kvp => kvp.Value.EndTime.HasValue && kvp.Value.EndTime < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in oldSessions)
            {
                _sessionMetrics.TryRemove(sessionId, out _);
            }

            _logger.LogInformation("Cleaned up {MetricsCount} old metrics and {SessionsCount} old sessions", 
                totalRemoved, oldSessions.Count);

            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> GetMonitoringStatusAsync()
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                ["service_status"] = "running",
                ["start_time"] = _startTime,
                ["uptime_seconds"] = (DateTime.UtcNow - _startTime).TotalSeconds,
                ["metrics_count"] = _metrics.Count,
                ["total_data_points"] = _metrics.Values.Sum(list => list.Count),
                ["active_sessions"] = _sessionMetrics.Count,
                ["last_health_check"] = _lastSystemHealth.Timestamp,
                ["performance_thresholds"] = _performanceThresholds
            });
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = _currentProcess.TotalProcessorTime;
                
                await Task.Delay(100); // Small delay to measure CPU usage
                
                var endTime = DateTime.UtcNow;
                var endCpuUsage = _currentProcess.TotalProcessorTime;
                
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                
                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0.0;
            }
        }

        private string DetermineHealthStatus(SystemHealth health)
        {
            if (health.CpuUsagePercent > 90 || health.MemoryUsagePercent > 95 || health.DiskUsagePercent > 95)
                return "critical";
            
            if (health.CpuUsagePercent > 80 || health.MemoryUsagePercent > 85 || health.DiskUsagePercent > 90)
                return "warning";
            
            return "healthy";
        }

        private List<string> GetHealthWarnings(SystemHealth health)
        {
            var warnings = new List<string>();
            
            if (health.CpuUsagePercent > _performanceThresholds["cpu_usage_percent"])
                warnings.Add($"High CPU usage: {health.CpuUsagePercent:F1}%");
            
            if (health.MemoryUsagePercent > _performanceThresholds["memory_usage_percent"])
                warnings.Add($"High memory usage: {health.MemoryUsagePercent:F1}%");
            
            if (health.DiskUsagePercent > _performanceThresholds["disk_usage_percent"])
                warnings.Add($"High disk usage: {health.DiskUsagePercent:F1}%");
            
            return warnings;
        }

        private List<string> GetHealthErrors(SystemHealth health)
        {
            var errors = new List<string>();
            
            if (health.CpuUsagePercent > 95)
                errors.Add("Critical CPU usage");
            
            if (health.MemoryUsagePercent > 98)
                errors.Add("Critical memory usage");
            
            if (health.DiskUsagePercent > 98)
                errors.Add("Critical disk usage");
            
            return errors;
        }

        private void CollectSystemHealth(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await GetSystemHealthAsync();
                    await CheckPerformanceThresholdsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during system health collection");
                }
            });
        }

        private void PerformCleanup(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CleanupOldMetricsAsync(TimeSpan.FromDays(7));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup");
                }
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performance Monitoring Service started");
            return Task.CompletedTask;
        }

        public async Task RecordSecurityMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            try
            {
                var securityMetric = new SecurityMetricData
                {
                    MetricName = metricName,
                    Value = value,
                    Timestamp = DateTime.UtcNow,
                    Properties = properties
                };

                _securityMetrics.AddOrUpdate(metricName, securityMetric, (key, existing) => securityMetric);

                if (IsCriticalSecurityMetric(metricName))
                {
                    _telemetryClient.TrackMetric($"security_{metricName}", value);
                    
                    if (properties != null)
                    {
                        var eventTelemetry = new EventTelemetry($"CriticalSecurityMetric_{metricName}");
                        foreach (var prop in properties)
                        {
                            eventTelemetry.Properties[prop.Key] = prop.Value;
                        }
                        _telemetryClient.TrackEvent(eventTelemetry);
                    }
                }

                _logger.LogDebug("Security metric recorded: {MetricName} = {Value}", metricName, value);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording security metric {MetricName}", metricName);
            }
        }

        private bool IsCriticalSecurityMetric(string metricName)
        {
            var criticalMetrics = new[]
            {
                "authentication_failure_count",
                "authorization_failure_count",
                "suspicious_activity_count",
                "xss_attack_count",
                "sql_injection_count",
                "rate_limit_exceeded_count",
                "invalid_token_count",
                "brute_force_attempt_count"
            };

            return criticalMetrics.Any(cm => metricName.Contains(cm, StringComparison.OrdinalIgnoreCase));
        }

        private void ReportMetricsToApplicationInsights(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var metricGroup in _metrics)
                    {
                        var recentMetrics = metricGroup.Value
                            .Where(m => m.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                            .ToList();

                        if (recentMetrics.Any())
                        {
                            var avgValue = recentMetrics.Average(m => m.Value);
                            _telemetryClient.TrackMetric($"performance_{metricGroup.Key}_avg", avgValue);
                            _telemetryClient.TrackMetric($"performance_{metricGroup.Key}_count", recentMetrics.Count);
                        }
                    }

                    foreach (var securityMetric in _securityMetrics.Values)
                    {
                        if (securityMetric.Timestamp >= DateTime.UtcNow.AddMinutes(-5))
                        {
                            _telemetryClient.TrackMetric($"security_{securityMetric.MetricName}", securityMetric.Value);
                            
                            if (securityMetric.Properties != null)
                            {
                                var eventTelemetry = new EventTelemetry($"SecurityMetric_{securityMetric.MetricName}");
                                foreach (var prop in securityMetric.Properties)
                                {
                                    eventTelemetry.Properties[prop.Key] = prop.Value;
                                }
                                _telemetryClient.TrackEvent(eventTelemetry);
                            }
                        }
                    }

                    var systemHealth = await GetSystemHealthAsync();
                    _telemetryClient.TrackMetric("system_cpu_usage", systemHealth.CpuUsagePercent);
                    _telemetryClient.TrackMetric("system_memory_usage", systemHealth.MemoryUsagePercent);
                    _telemetryClient.TrackMetric("system_disk_usage", systemHealth.DiskUsagePercent);
                    _telemetryClient.TrackMetric("system_active_sessions", systemHealth.ActiveConnections);

                    if (systemHealth.Status != "healthy")
                    {
                        var healthEvent = new EventTelemetry("SystemHealthAlert");
                        healthEvent.Properties["status"] = systemHealth.Status;
                        healthEvent.Properties["warnings_count"] = systemHealth.Warnings.Count.ToString();
                        healthEvent.Properties["errors_count"] = systemHealth.Errors.Count.ToString();
                        _telemetryClient.TrackEvent(healthEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reporting metrics to Application Insights");
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Performance Monitoring Service stopping");
            _systemHealthTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _reportingTimer?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _systemHealthTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _reportingTimer?.Dispose();
            _currentProcess?.Dispose();
        }
    }

    public class SecurityMetricData
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MonitorPerformanceAttribute : Attribute
    {
        public string MetricName { get; set; }
        public bool TrackExceptions { get; set; } = true;

        public MonitorPerformanceAttribute(string metricName)
        {
            MetricName = metricName;
        }
    }
}
