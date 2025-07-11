using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Masark.Infrastructure.HealthChecks;

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly long _minimumFreeBytesThreshold;

    public DiskSpaceHealthCheck(long minimumFreeBytesThreshold = 1024 * 1024 * 1024) // 1GB default
    {
        _minimumFreeBytesThreshold = minimumFreeBytesThreshold;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            var driveInfos = new List<object>();
            var hasLowSpace = false;
            var hasCriticalSpace = false;

            foreach (var drive in drives)
            {
                var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                var usedSpaceGB = totalSpaceGB - freeSpaceGB;
                var usagePercentage = (usedSpaceGB / totalSpaceGB) * 100;

                driveInfos.Add(new
                {
                    Name = drive.Name,
                    FreeSpaceGB = Math.Round(freeSpaceGB, 2),
                    TotalSpaceGB = Math.Round(totalSpaceGB, 2),
                    UsedSpaceGB = Math.Round(usedSpaceGB, 2),
                    UsagePercentage = Math.Round(usagePercentage, 2)
                });

                if (drive.AvailableFreeSpace < _minimumFreeBytesThreshold)
                {
                    if (usagePercentage > 95)
                    {
                        hasCriticalSpace = true;
                    }
                    else if (usagePercentage > 85)
                    {
                        hasLowSpace = true;
                    }
                }
            }

            var data = new Dictionary<string, object>
            {
                ["Drives"] = driveInfos,
                ["MinimumFreeSpaceGB"] = _minimumFreeBytesThreshold / (1024.0 * 1024.0 * 1024.0),
                ["LastCheck"] = DateTime.UtcNow
            };

            if (hasCriticalSpace)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Critical disk space - less than 5% free space available", null, data));
            }

            if (hasLowSpace)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Low disk space - less than 15% free space available", null, data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Disk space is sufficient", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space health check failed", ex));
        }
    }
}
