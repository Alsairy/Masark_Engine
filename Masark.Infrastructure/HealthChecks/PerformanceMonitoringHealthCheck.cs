using Microsoft.Extensions.Diagnostics.HealthChecks;
using Masark.Application.Services;

namespace Masark.Infrastructure.HealthChecks;

public class PerformanceMonitoringHealthCheck : IHealthCheck
{
    private readonly IPerformanceMonitoringService _performanceMonitoringService;

    public PerformanceMonitoringHealthCheck(IPerformanceMonitoringService performanceMonitoringService)
    {
        _performanceMonitoringService = performanceMonitoringService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _performanceMonitoringService.RecordMetricAsync("health_check_test", 1.0, "count");
            
            await _performanceMonitoringService.RecordRequestMetricAsync("/health", TimeSpan.FromMilliseconds(50), 200);

            var systemHealth = await _performanceMonitoringService.GetSystemHealthAsync();
            var systemMetrics = await _performanceMonitoringService.GetSystemMetricsAsync();
            var isHealthy = await _performanceMonitoringService.IsSystemHealthyAsync();

            var data = new Dictionary<string, object>
            {
                ["MetricRecorded"] = true,
                ["RequestMetricRecorded"] = true,
                ["SystemHealthAvailable"] = systemHealth != null,
                ["SystemMetricsAvailable"] = systemMetrics != null,
                ["SystemHealthy"] = isHealthy,
                ["MetricsCount"] = systemMetrics?.Count ?? 0,
                ["LastCheck"] = DateTime.UtcNow
            };

            if (!isHealthy)
            {
                return HealthCheckResult.Degraded("System health check indicates degraded performance", null, data);
            }

            return HealthCheckResult.Healthy("Performance Monitoring Service is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Performance Monitoring Service health check failed", ex);
        }
    }
}
