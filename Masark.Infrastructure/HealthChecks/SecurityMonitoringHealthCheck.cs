using Microsoft.Extensions.Diagnostics.HealthChecks;
using Masark.Infrastructure.Services;

namespace Masark.Infrastructure.HealthChecks;

public class SecurityMonitoringHealthCheck : IHealthCheck
{
    private readonly ISecurityMonitoringService _securityMonitoringService;

    public SecurityMonitoringHealthCheck(ISecurityMonitoringService securityMonitoringService)
    {
        _securityMonitoringService = securityMonitoringService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testEvent = new SecurityEvent
            {
                EventType = "HealthCheck",
                Severity = SecurityEventSeverity.Low,
                UserId = "health-check-user",
                IpAddress = "127.0.0.1",
                UserAgent = "HealthCheckAgent",
                Resource = "HealthCheck",
                Action = "Test",
                Description = "Security monitoring health check test",
                TenantId = "health-check-tenant",
                AdditionalData = new Dictionary<string, object>
                {
                    ["Source"] = "SecurityMonitoringHealthCheck",
                    ["TestRun"] = DateTime.UtcNow
                }
            };

            await _securityMonitoringService.LogSecurityEventAsync(testEvent);

            var metrics = await _securityMonitoringService.GetSecurityMetricsAsync(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
            var alerts = await _securityMonitoringService.GetActiveAlertsAsync();
            
            var data = new Dictionary<string, object>
            {
                ["SecurityEventLogged"] = true,
                ["MetricsAvailable"] = metrics != null,
                ["ActiveAlertsCount"] = alerts?.Count ?? 0,
                ["MonitoringActive"] = true,
                ["LastCheck"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Security Monitoring Service is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Security Monitoring Service health check failed", ex);
        }
    }
}
