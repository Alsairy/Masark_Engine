using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Masark.Infrastructure.HealthChecks;

public class CpuUsageHealthCheck : IHealthCheck
{
    private readonly double _warningThreshold;
    private readonly double _criticalThreshold;
    private static readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");

    public CpuUsageHealthCheck(double warningThreshold = 80.0, double criticalThreshold = 95.0)
    {
        _warningThreshold = warningThreshold;
        _criticalThreshold = criticalThreshold;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;

            await Task.Delay(1000, cancellationToken);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            var cpuUsagePercentage = cpuUsageTotal * 100;

            double systemCpuUsage = 0;
            try
            {
                systemCpuUsage = _cpuCounter.NextValue();
                if (systemCpuUsage == 0)
                {
                    await Task.Delay(100, cancellationToken);
                    systemCpuUsage = _cpuCounter.NextValue();
                }
            }
            catch
            {
                systemCpuUsage = cpuUsagePercentage;
            }

            var data = new Dictionary<string, object>
            {
                ["ProcessCpuUsage"] = Math.Round(cpuUsagePercentage, 2),
                ["SystemCpuUsage"] = Math.Round(systemCpuUsage, 2),
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["WarningThreshold"] = _warningThreshold,
                ["CriticalThreshold"] = _criticalThreshold,
                ["LastCheck"] = DateTime.UtcNow
            };

            var effectiveCpuUsage = Math.Max(cpuUsagePercentage, systemCpuUsage);

            if (effectiveCpuUsage >= _criticalThreshold)
            {
                return HealthCheckResult.Unhealthy($"Critical CPU usage: {effectiveCpuUsage:F2}%", null, data);
            }

            if (effectiveCpuUsage >= _warningThreshold)
            {
                return HealthCheckResult.Degraded($"High CPU usage: {effectiveCpuUsage:F2}%", null, data);
            }

            return HealthCheckResult.Healthy($"CPU usage is normal: {effectiveCpuUsage:F2}%", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CPU usage health check failed", ex);
        }
    }
}
