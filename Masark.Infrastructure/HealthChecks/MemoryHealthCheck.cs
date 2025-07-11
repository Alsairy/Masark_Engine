using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Masark.Infrastructure.HealthChecks;

public class MemoryHealthCheck : IHealthCheck
{
    private readonly long _thresholdBytes;

    public MemoryHealthCheck(long thresholdBytes = 1024 * 1024 * 1024) // 1GB default
    {
        _thresholdBytes = thresholdBytes;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            var virtualMemory = process.VirtualMemorySize64;

            var gcMemory = GC.GetTotalMemory(false);
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);

            var data = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = workingSet / (1024 * 1024),
                ["PrivateMemoryMB"] = privateMemory / (1024 * 1024),
                ["VirtualMemoryMB"] = virtualMemory / (1024 * 1024),
                ["GCMemoryMB"] = gcMemory / (1024 * 1024),
                ["Gen0Collections"] = gen0Collections,
                ["Gen1Collections"] = gen1Collections,
                ["Gen2Collections"] = gen2Collections,
                ["ThresholdMB"] = _thresholdBytes / (1024 * 1024),
                ["LastCheck"] = DateTime.UtcNow
            };

            if (workingSet > _thresholdBytes)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"High memory usage: {workingSet / (1024 * 1024)} MB", null, data));
            }

            if (gen2Collections > 100)
            {
                return Task.FromResult(HealthCheckResult.Degraded($"High Gen2 GC collections: {gen2Collections}", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Memory usage is within acceptable limits", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory health check failed", ex));
        }
    }
}
