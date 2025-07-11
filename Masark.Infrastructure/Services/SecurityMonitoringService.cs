using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Masark.Infrastructure.Services
{
    public interface ISecurityMonitoringService
    {
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task LogAuthenticationAttemptAsync(string userId, string ipAddress, bool success, string reason = null);
        Task LogAuthorizationFailureAsync(string userId, string resource, string action, string reason);
        Task LogSuspiciousActivityAsync(string description, string ipAddress, string userAgent, Dictionary<string, string> additionalData = null);
        Task LogDataAccessAsync(string userId, string dataType, string operation, bool success);
        Task LogConfigurationChangeAsync(string userId, string configKey, string oldValue, string newValue);
        Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime from, DateTime to);
        Task<List<SecurityAlert>> GetActiveAlertsAsync();
        Task TrackSecurityMetricAsync(string metricName, double value, Dictionary<string, string> properties = null);
    }

    public class SecurityMonitoringService : ISecurityMonitoringService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<SecurityMonitoringService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, SecurityMetricCounter> _securityCounters;
        private readonly ConcurrentQueue<SecurityAlert> _activeAlerts;

        public SecurityMonitoringService(
            TelemetryClient telemetryClient,
            ILogger<SecurityMonitoringService> logger,
            IConfiguration configuration)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
            _configuration = configuration;
            _securityCounters = new ConcurrentDictionary<string, SecurityMetricCounter>();
            _activeAlerts = new ConcurrentQueue<SecurityAlert>();
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            try
            {
                var eventTelemetry = new EventTelemetry("SecurityEvent");
                eventTelemetry.Properties["EventType"] = securityEvent.EventType;
                eventTelemetry.Properties["Severity"] = securityEvent.Severity.ToString();
                eventTelemetry.Properties["UserId"] = securityEvent.UserId ?? "anonymous";
                eventTelemetry.Properties["IpAddress"] = securityEvent.IpAddress ?? "unknown";
                eventTelemetry.Properties["UserAgent"] = securityEvent.UserAgent ?? "unknown";
                eventTelemetry.Properties["Resource"] = securityEvent.Resource ?? "unknown";
                eventTelemetry.Properties["Action"] = securityEvent.Action ?? "unknown";
                eventTelemetry.Properties["Description"] = securityEvent.Description;
                eventTelemetry.Properties["TenantId"] = securityEvent.TenantId ?? "unknown";

                if (securityEvent.AdditionalData != null)
                {
                    foreach (var kvp in securityEvent.AdditionalData)
                    {
                        eventTelemetry.Properties[$"Additional_{kvp.Key}"] = kvp.Value?.ToString() ?? "null";
                    }
                }

                _telemetryClient.TrackEvent(eventTelemetry);

                _logger.LogWarning("Security Event: {EventType} - {Description} | User: {UserId} | IP: {IpAddress} | Severity: {Severity}",
                    securityEvent.EventType, securityEvent.Description, securityEvent.UserId, securityEvent.IpAddress, securityEvent.Severity);

                if (securityEvent.Severity >= SecurityEventSeverity.High)
                {
                    await CreateSecurityAlertAsync(securityEvent);
                }

                await UpdateSecurityMetricsAsync(securityEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", securityEvent.EventType);
            }
        }

        public async Task LogAuthenticationAttemptAsync(string userId, string ipAddress, bool success, string reason = null)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = success ? "AuthenticationSuccess" : "AuthenticationFailure",
                Severity = success ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium,
                UserId = userId,
                IpAddress = ipAddress,
                Description = success ? "User authenticated successfully" : $"Authentication failed: {reason}",
                Action = "Authenticate",
                Resource = "AuthenticationSystem",
                AdditionalData = new Dictionary<string, object>
                {
                    ["Success"] = success,
                    ["Reason"] = reason ?? "N/A"
                }
            };

            await LogSecurityEventAsync(securityEvent);

            var metricName = success ? "authentication_success_count" : "authentication_failure_count";
            await TrackSecurityMetricAsync(metricName, 1, new Dictionary<string, string>
            {
                ["IpAddress"] = ipAddress,
                ["UserId"] = userId ?? "anonymous"
            });
        }

        public async Task LogAuthorizationFailureAsync(string userId, string resource, string action, string reason)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "AuthorizationFailure",
                Severity = SecurityEventSeverity.Medium,
                UserId = userId,
                Resource = resource,
                Action = action,
                Description = $"Authorization failed for {action} on {resource}: {reason}",
                AdditionalData = new Dictionary<string, object>
                {
                    ["Reason"] = reason
                }
            };

            await LogSecurityEventAsync(securityEvent);
            await TrackSecurityMetricAsync("authorization_failure_count", 1, new Dictionary<string, string>
            {
                ["Resource"] = resource,
                ["Action"] = action,
                ["UserId"] = userId ?? "anonymous"
            });
        }

        public async Task LogSuspiciousActivityAsync(string description, string ipAddress, string userAgent, Dictionary<string, string> additionalData = null)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "SuspiciousActivity",
                Severity = SecurityEventSeverity.High,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Description = description,
                AdditionalData = additionalData?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
            };

            await LogSecurityEventAsync(securityEvent);
            await TrackSecurityMetricAsync("suspicious_activity_count", 1, new Dictionary<string, string>
            {
                ["IpAddress"] = ipAddress,
                ["UserAgent"] = userAgent ?? "unknown"
            });
        }

        public async Task LogDataAccessAsync(string userId, string dataType, string operation, bool success)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = success ? "DataAccessSuccess" : "DataAccessFailure",
                Severity = success ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium,
                UserId = userId,
                Action = operation,
                Resource = dataType,
                Description = $"Data access {(success ? "succeeded" : "failed")}: {operation} on {dataType}",
                AdditionalData = new Dictionary<string, object>
                {
                    ["Success"] = success,
                    ["DataType"] = dataType,
                    ["Operation"] = operation
                }
            };

            await LogSecurityEventAsync(securityEvent);

            var metricName = success ? "data_access_success_count" : "data_access_failure_count";
            await TrackSecurityMetricAsync(metricName, 1, new Dictionary<string, string>
            {
                ["DataType"] = dataType,
                ["Operation"] = operation,
                ["UserId"] = userId ?? "anonymous"
            });
        }

        public async Task LogConfigurationChangeAsync(string userId, string configKey, string oldValue, string newValue)
        {
            var securityEvent = new SecurityEvent
            {
                EventType = "ConfigurationChange",
                Severity = SecurityEventSeverity.Medium,
                UserId = userId,
                Action = "ConfigurationUpdate",
                Resource = "SystemConfiguration",
                Description = $"Configuration changed: {configKey}",
                AdditionalData = new Dictionary<string, object>
                {
                    ["ConfigKey"] = configKey,
                    ["OldValue"] = oldValue ?? "null",
                    ["NewValue"] = newValue ?? "null"
                }
            };

            await LogSecurityEventAsync(securityEvent);
            await TrackSecurityMetricAsync("configuration_change_count", 1, new Dictionary<string, string>
            {
                ["ConfigKey"] = configKey,
                ["UserId"] = userId ?? "anonymous"
            });
        }

        public async Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime from, DateTime to)
        {
            var metrics = new SecurityMetrics
            {
                From = from,
                To = to,
                AuthenticationAttempts = GetCounterValue("authentication_success_count") + GetCounterValue("authentication_failure_count"),
                AuthenticationFailures = GetCounterValue("authentication_failure_count"),
                AuthorizationFailures = GetCounterValue("authorization_failure_count"),
                SuspiciousActivities = GetCounterValue("suspicious_activity_count"),
                DataAccessAttempts = GetCounterValue("data_access_success_count") + GetCounterValue("data_access_failure_count"),
                DataAccessFailures = GetCounterValue("data_access_failure_count"),
                ConfigurationChanges = GetCounterValue("configuration_change_count"),
                ActiveAlerts = _activeAlerts.Count
            };

            return metrics;
        }

        public async Task<List<SecurityAlert>> GetActiveAlertsAsync()
        {
            return _activeAlerts.ToList();
        }

        public async Task TrackSecurityMetricAsync(string metricName, double value, Dictionary<string, string> properties = null)
        {
            try
            {
                var metric = _telemetryClient.GetMetric(metricName);
                
                if (properties != null && properties.Any())
                {
                    var dimensionNames = properties.Keys.ToArray();
                    var dimensionValues = properties.Values.ToArray();
                    
                    if (dimensionNames.Length == 1)
                        metric.TrackValue(value, dimensionValues[0]);
                    else if (dimensionNames.Length == 2)
                        metric.TrackValue(value, dimensionValues[0], dimensionValues[1]);
                    else
                        metric.TrackValue(value);
                }
                else
                {
                    metric.TrackValue(value);
                }

                _securityCounters.AddOrUpdate(metricName, 
                    new SecurityMetricCounter { Count = (long)value, LastUpdated = DateTime.UtcNow },
                    (key, existing) => new SecurityMetricCounter 
                    { 
                        Count = existing.Count + (long)value, 
                        LastUpdated = DateTime.UtcNow 
                    });

                _logger.LogDebug("Security metric tracked: {MetricName} = {Value}", metricName, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track security metric: {MetricName}", metricName);
            }
        }

        private async Task CreateSecurityAlertAsync(SecurityEvent securityEvent)
        {
            var alert = new SecurityAlert
            {
                Id = Guid.NewGuid().ToString(),
                EventType = securityEvent.EventType,
                Severity = securityEvent.Severity,
                Description = securityEvent.Description,
                UserId = securityEvent.UserId,
                IpAddress = securityEvent.IpAddress,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AdditionalData = securityEvent.AdditionalData
            };

            _activeAlerts.Enqueue(alert);

            _logger.LogCritical("Security Alert Created: {AlertId} - {EventType} - {Description}",
                alert.Id, alert.EventType, alert.Description);

            var alertTelemetry = new EventTelemetry("SecurityAlert");
            alertTelemetry.Properties["AlertId"] = alert.Id;
            alertTelemetry.Properties["EventType"] = alert.EventType;
            alertTelemetry.Properties["Severity"] = alert.Severity.ToString();
            alertTelemetry.Properties["Description"] = alert.Description;
            
            _telemetryClient.TrackEvent(alertTelemetry);
        }

        private async Task UpdateSecurityMetricsAsync(SecurityEvent securityEvent)
        {
            var metricName = $"security_event_{securityEvent.EventType.ToLowerInvariant()}_count";
            await TrackSecurityMetricAsync(metricName, 1, new Dictionary<string, string>
            {
                ["Severity"] = securityEvent.Severity.ToString(),
                ["EventType"] = securityEvent.EventType
            });
        }

        private long GetCounterValue(string counterName)
        {
            return _securityCounters.TryGetValue(counterName, out var counter) ? counter.Count : 0;
        }
    }

    public class SecurityEvent
    {
        public string EventType { get; set; }
        public SecurityEventSeverity Severity { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Resource { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string TenantId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    public enum SecurityEventSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public class SecurityMetrics
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public long AuthenticationAttempts { get; set; }
        public long AuthenticationFailures { get; set; }
        public long AuthorizationFailures { get; set; }
        public long SuspiciousActivities { get; set; }
        public long DataAccessAttempts { get; set; }
        public long DataAccessFailures { get; set; }
        public long ConfigurationChanges { get; set; }
        public int ActiveAlerts { get; set; }
    }

    public class SecurityAlert
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public SecurityEventSeverity Severity { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    public class SecurityMetricCounter
    {
        public long Count { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
