using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Masark.Application.Interfaces;
using Masark.Application.Services;
using Masark.Infrastructure.Services;
using Masark.Infrastructure.Identity;

namespace Masark.Infrastructure.Services
{
    public interface IServiceValidator
    {
        Task<bool> ValidateServicesAsync(IServiceProvider serviceProvider);
        Task<ServiceValidationResult> GetValidationResultAsync(IServiceProvider serviceProvider);
    }

    public class ServiceValidator : IServiceValidator
    {
        private readonly ILogger<ServiceValidator> _logger;

        public ServiceValidator(ILogger<ServiceValidator> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ValidateServicesAsync(IServiceProvider serviceProvider)
        {
            var result = await GetValidationResultAsync(serviceProvider);
            return result.IsValid;
        }

        public async Task<ServiceValidationResult> GetValidationResultAsync(IServiceProvider serviceProvider)
        {
            var result = new ServiceValidationResult();
            
            try
            {
                _logger.LogInformation("Starting service validation...");

                await ValidateCriticalServicesAsync(serviceProvider, result);
                
                await ValidateDatabaseConnectivityAsync(serviceProvider, result);
                
                await ValidateCachingServicesAsync(serviceProvider, result);
                
                await ValidateAuthenticationServicesAsync(serviceProvider, result);

                result.IsValid = result.Errors.Count == 0;
                
                if (result.IsValid)
                {
                    _logger.LogInformation("Service validation completed successfully");
                }
                else
                {
                    _logger.LogError("Service validation failed with {ErrorCount} errors", result.Errors.Count);
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Validation error: {Error}", error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service validation failed with exception");
                result.Errors.Add($"Service validation exception: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private async Task ValidateCriticalServicesAsync(IServiceProvider serviceProvider, ServiceValidationResult result)
        {
            var criticalServices = new[]
            {
                typeof(IPersonalityScoringService),
                typeof(ICareerMatchingService),
                typeof(ICachingService),
                typeof(ILocalizationService),
                typeof(IPersonalityRepository),
                typeof(IJwtTokenService)
            };

            foreach (var serviceType in criticalServices)
            {
                try
                {
                    var service = serviceProvider.GetService(serviceType);
                    if (service == null)
                    {
                        result.Errors.Add($"Critical service {serviceType.Name} is not registered");
                    }
                    else
                    {
                        result.ValidatedServices.Add(serviceType.Name);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to resolve {serviceType.Name}: {ex.Message}");
                }
            }

            await Task.CompletedTask;
        }

        private async Task ValidateDatabaseConnectivityAsync(IServiceProvider serviceProvider, ServiceValidationResult result)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<Masark.Infrastructure.Identity.ApplicationDbContext>();
                
                if (dbContext == null)
                {
                    result.Errors.Add("ApplicationDbContext is not registered");
                    return;
                }

                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    result.Errors.Add("Cannot connect to database");
                }
                else
                {
                    result.ValidatedServices.Add("DatabaseConnectivity");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Database validation failed: {ex.Message}");
            }
        }

        private async Task ValidateCachingServicesAsync(IServiceProvider serviceProvider, ServiceValidationResult result)
        {
            try
            {
                var cachingService = serviceProvider.GetService<ICachingService>();
                if (cachingService == null)
                {
                    result.Errors.Add("ICachingService is not registered");
                    return;
                }

                var testKey = "validation_test_key";
                var testValue = "validation_test_value";
                
                var testQuestions = await cachingService.GetQuestionsAsync("en");
                var testCareers = await cachingService.GetCareersAsync("en");
                
                if (testQuestions == null && testCareers == null)
                {
                    _logger.LogWarning("Cache service has no data available - this is expected during initial startup");
                }
                
                result.ValidatedServices.Add("CachingService");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Caching service validation failed: {ex.Message}");
            }
        }

        private async Task ValidateAuthenticationServicesAsync(IServiceProvider serviceProvider, ServiceValidationResult result)
        {
            try
            {
                var jwtService = serviceProvider.GetService<IJwtTokenService>();
                if (jwtService == null)
                {
                    result.Errors.Add("IJwtTokenService is not registered");
                    return;
                }

                var testUser = new ApplicationUser
                {
                    Id = "test-user",
                    UserName = "testuser",
                    Email = "test@example.com",
                    TenantId = 1,
                    IsActive = true
                };
                
                var token = jwtService.GenerateToken(testUser, new List<string> { "USER" });
                if (string.IsNullOrEmpty(token))
                {
                    result.Errors.Add("JWT token generation failed");
                }
                else
                {
                    result.ValidatedServices.Add("JwtTokenService");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Authentication service validation failed: {ex.Message}");
            }
        }
    }

    public class ServiceValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> ValidatedServices { get; set; } = new();
        public DateTime ValidationTime { get; set; } = DateTime.UtcNow;
    }
}
