using Microsoft.Extensions.Diagnostics.HealthChecks;
using Masark.Application.Services;

namespace Masark.Infrastructure.HealthChecks;

public class CachingServiceHealthCheck : IHealthCheck
{
    private readonly ICachingService _cachingService;

    public CachingServiceHealthCheck(ICachingService cachingService)
    {
        _cachingService = cachingService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testQuestions = await _cachingService.GetQuestionsAsync("en");
            var testCareers = await _cachingService.GetCareersAsync("en");
            var testPersonalityTypes = await _cachingService.GetPersonalityTypesAsync("en");

            var data = new Dictionary<string, object>
            {
                ["QuestionsAvailable"] = testQuestions != null,
                ["CareersAvailable"] = testCareers != null,
                ["PersonalityTypesAvailable"] = testPersonalityTypes != null,
                ["QuestionsCount"] = testQuestions?.Count ?? 0,
                ["CareersCount"] = testCareers?.Count ?? 0,
                ["PersonalityTypesCount"] = testPersonalityTypes?.Count ?? 0,
                ["LastCheck"] = DateTime.UtcNow
            };

            if (testQuestions == null && testCareers == null && testPersonalityTypes == null)
            {
                return HealthCheckResult.Degraded("Caching service has no cached data available", null, data);
            }

            return HealthCheckResult.Healthy("Caching Service is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Caching Service health check failed", ex);
        }
    }
}
