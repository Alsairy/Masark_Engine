using Microsoft.Extensions.Diagnostics.HealthChecks;
using Masark.Infrastructure.Services;
using Masark.Infrastructure.Identity;

namespace Masark.Infrastructure.HealthChecks;

public class JwtTokenHealthCheck : IHealthCheck
{
    private readonly IJwtTokenService _jwtTokenService;

    public JwtTokenHealthCheck(IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var testUser = new ApplicationUser
            {
                Id = "health-check-user",
                UserName = "healthcheck",
                Email = "healthcheck@test.com",
                TenantId = 1,
                IsActive = true
            };

            var testRoles = new List<string> { "USER" };

            var token = _jwtTokenService.GenerateToken(testUser, testRoles);
            
            if (string.IsNullOrEmpty(token))
            {
                return HealthCheckResult.Unhealthy("JWT token generation failed - empty token returned");
            }

            var principal = _jwtTokenService.ValidateToken(token);
            
            if (principal == null)
            {
                return HealthCheckResult.Unhealthy("JWT token validation failed");
            }

            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var data = new Dictionary<string, object>
            {
                ["TokenLength"] = token.Length,
                ["TokenGenerated"] = true,
                ["TokenValidated"] = principal != null,
                ["RefreshTokenGenerated"] = !string.IsNullOrEmpty(refreshToken),
                ["ClaimsCount"] = principal?.Claims?.Count() ?? 0,
                ["LastCheck"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("JWT Token Service is functioning correctly", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("JWT Token Service health check failed", ex);
        }
    }
}
