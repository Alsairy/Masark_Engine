using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Masark.Infrastructure.HealthChecks;

public class ApplicationInsightsHealthCheck : IHealthCheck
{
    private readonly TelemetryClient _telemetryClient;
    private readonly TelemetryConfiguration _telemetryConfiguration;

    public ApplicationInsightsHealthCheck(TelemetryClient telemetryClient, TelemetryConfiguration telemetryConfiguration)
    {
        _telemetryClient = telemetryClient;
        _telemetryConfiguration = telemetryConfiguration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isEnabled = !_telemetryConfiguration.DisableTelemetry;
            var connectionString = _telemetryConfiguration.ConnectionString;
            
            if (!isEnabled)
            {
                return HealthCheckResult.Degraded("Application Insights telemetry is disabled");
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy("Application Insights connection string is not configured");
            }

            _telemetryClient.TrackEvent("HealthCheck.ApplicationInsights", new Dictionary<string, string>
            {
                ["Status"] = "Healthy",
                ["Timestamp"] = DateTime.UtcNow.ToString("O")
            });

            await _telemetryClient.FlushAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["ConnectionString"] = connectionString?.Substring(0, Math.Min(50, connectionString.Length)) + "...",
                ["TelemetryEnabled"] = isEnabled,
                ["InstrumentationKey"] = _telemetryConfiguration.InstrumentationKey?.Substring(0, 8) + "...",
                ["LastCheck"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Application Insights is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Application Insights health check failed", ex);
        }
    }
}
