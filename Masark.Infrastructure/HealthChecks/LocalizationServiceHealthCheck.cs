using Microsoft.Extensions.Diagnostics.HealthChecks;
using Masark.Application.Services;

namespace Masark.Infrastructure.HealthChecks;

public class LocalizationServiceHealthCheck : IHealthCheck
{
    private readonly ILocalizationService _localizationService;

    public LocalizationServiceHealthCheck(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var supportedLanguages = await _localizationService.GetSupportedLanguagesAsync();
            
            if (supportedLanguages == null || !supportedLanguages.Any())
            {
                return HealthCheckResult.Unhealthy("No supported languages found");
            }

            var testTranslations = new Dictionary<string, string>();
            var failedLanguages = new List<string>();

            foreach (var languageConfig in supportedLanguages.Take(3))
            {
                try
                {
                    var translation = await _localizationService.GetTextAsync("welcome", languageConfig.Code);
                    testTranslations[languageConfig.Code] = translation ?? "N/A";
                }
                catch
                {
                    failedLanguages.Add(languageConfig.Code);
                }
            }

            var data = new Dictionary<string, object>
            {
                ["SupportedLanguagesCount"] = supportedLanguages.Count(),
                ["SupportedLanguages"] = supportedLanguages.Take(10).Select(l => l.Code).ToArray(),
                ["TestTranslations"] = testTranslations,
                ["FailedLanguages"] = failedLanguages,
                ["LastCheck"] = DateTime.UtcNow
            };

            if (failedLanguages.Any())
            {
                return HealthCheckResult.Degraded($"Localization service has issues with {failedLanguages.Count} languages", null, data);
            }

            return HealthCheckResult.Healthy("Localization Service is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Localization Service health check failed", ex);
        }
    }
}
