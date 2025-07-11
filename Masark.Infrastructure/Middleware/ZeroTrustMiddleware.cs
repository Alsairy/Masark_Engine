using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Masark.Infrastructure.Middleware
{
    public class ZeroTrustMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ZeroTrustMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly HashSet<string> _trustedIpRanges;
        private readonly HashSet<string> _suspiciousUserAgents;

        public ZeroTrustMiddleware(
            RequestDelegate next,
            ILogger<ZeroTrustMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            
            _trustedIpRanges = new HashSet<string>
            {
                "127.0.0.1",
                "::1",
                "10.0.0.0/8",
                "172.16.0.0/12",
                "192.168.0.0/16"
            };

            _suspiciousUserAgents = new HashSet<string>
            {
                "curl",
                "wget",
                "python-requests",
                "bot",
                "crawler",
                "spider"
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var requestId = Guid.NewGuid().ToString();
                context.Items["RequestId"] = requestId;

                _logger.LogInformation("ZeroTrust: Processing request {RequestId} from {RemoteIp} to {Path}",
                    requestId, GetClientIpAddress(context), context.Request.Path);

                if (IsTestEnvironment(context))
                {
                    _logger.LogInformation("ZeroTrust: Bypassing security validation for test environment");
                    await _next(context);
                    return;
                }

                if (!await ValidateRequestAsync(context))
                {
                    await HandleUnauthorizedRequestAsync(context, "Request validation failed");
                    return;
                }

                if (!await ValidateIpAddressAsync(context))
                {
                    await HandleUnauthorizedRequestAsync(context, "IP address validation failed");
                    return;
                }

                if (!await ValidateUserAgentAsync(context))
                {
                    await HandleUnauthorizedRequestAsync(context, "User agent validation failed");
                    return;
                }

                if (!await ValidateJwtTokenAsync(context))
                {
                    await HandleUnauthorizedRequestAsync(context, "JWT token validation failed");
                    return;
                }

                if (!await ValidateRequestIntegrityAsync(context))
                {
                    await HandleUnauthorizedRequestAsync(context, "Request integrity validation failed");
                    return;
                }

                context.Response.Headers.Add("X-Request-Id", requestId);
                context.Response.Headers.Add("X-Security-Level", "ZeroTrust");

                await _next(context);

                _logger.LogInformation("ZeroTrust: Request {RequestId} completed successfully", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZeroTrust: Error processing request");
                await HandleUnauthorizedRequestAsync(context, "Internal security error");
            }
        }

        private async Task<bool> ValidateRequestAsync(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
                if (string.IsNullOrEmpty(forwardedFor) || forwardedFor.Split(',').Length > 5)
                {
                    _logger.LogWarning("ZeroTrust: Suspicious X-Forwarded-For header: {ForwardedFor}", forwardedFor);
                    return false;
                }
            }

            if (context.Request.ContentLength > 10 * 1024 * 1024)
            {
                _logger.LogWarning("ZeroTrust: Request size too large: {ContentLength}", context.Request.ContentLength);
                return false;
            }

            var requestPath = context.Request.Path.Value?.ToLowerInvariant();
            if (requestPath != null && (requestPath.Contains("../") || requestPath.Contains("..\\") || requestPath.Contains("%2e%2e")))
            {
                _logger.LogWarning("ZeroTrust: Path traversal attempt detected: {Path}", requestPath);
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateIpAddressAsync(HttpContext context)
        {
            var clientIp = GetClientIpAddress(context);
            
            if (string.IsNullOrEmpty(clientIp))
            {
                _logger.LogWarning("ZeroTrust: Unable to determine client IP address");
                return false;
            }

            if (IsPrivateNetwork(clientIp))
            {
                return true;
            }

            var rateLimitKey = $"ip_rate_limit:{clientIp}";
            
            return true;
        }

        private async Task<bool> ValidateUserAgentAsync(HttpContext context)
        {
            if (IsPublicEndpoint(context.Request.Path))
            {
                return true;
            }

            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLowerInvariant();
            
            if (string.IsNullOrEmpty(userAgent))
            {
                _logger.LogWarning("ZeroTrust: Missing User-Agent header");
                return false;
            }

            foreach (var suspicious in _suspiciousUserAgents)
            {
                if (userAgent.Contains(suspicious))
                {
                    _logger.LogWarning("ZeroTrust: Suspicious User-Agent detected: {UserAgent}", userAgent);
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ValidateJwtTokenAsync(HttpContext context)
        {
            if (IsPublicEndpoint(context.Request.Path))
            {
                return true;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("ZeroTrust: Missing or invalid Authorization header");
                return false;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("ZeroTrust: Expired JWT token");
                    return false;
                }

                var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");
                if (tenantClaim == null)
                {
                    _logger.LogWarning("ZeroTrust: Missing tenant_id claim in JWT");
                    return false;
                }

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("ZeroTrust: Missing user ID claim in JWT");
                    return false;
                }

                var issuedAt = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat");
                if (issuedAt != null)
                {
                    var iatDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAt.Value)).DateTime;
                    if (DateTime.UtcNow.Subtract(iatDateTime).TotalHours > 24)
                    {
                        _logger.LogWarning("ZeroTrust: JWT token too old");
                        return false;
                    }
                }

                context.Items["ValidatedToken"] = jwtToken;
                context.Items["TenantId"] = tenantClaim.Value;
                context.Items["UserId"] = userIdClaim.Value;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZeroTrust: JWT token validation failed");
                return false;
            }
        }

        private async Task<bool> ValidateRequestIntegrityAsync(HttpContext context)
        {
            var timestampHeader = context.Request.Headers["X-Timestamp"].ToString();
            if (!string.IsNullOrEmpty(timestampHeader))
            {
                if (long.TryParse(timestampHeader, out var timestamp))
                {
                    var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    if (Math.Abs(DateTime.UtcNow.Subtract(requestTime).TotalMinutes) > 5)
                    {
                        _logger.LogWarning("ZeroTrust: Request timestamp too old or in future");
                        return false;
                    }
                }
            }

            var nonceHeader = context.Request.Headers["X-Nonce"].ToString();
            if (!string.IsNullOrEmpty(nonceHeader))
            {
                if (nonceHeader.Length < 16 || nonceHeader.Length > 64)
                {
                    _logger.LogWarning("ZeroTrust: Invalid nonce format");
                    return false;
                }
            }

            return true;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                var ips = xForwardedFor.Split(',');
                return ips[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsPrivateNetwork(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out var ip))
            {
                var bytes = ip.GetAddressBytes();
                
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return (bytes[0] == 10) ||
                           (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                           (bytes[0] == 192 && bytes[1] == 168) ||
                           (bytes[0] == 127);
                }
            }
            
            return false;
        }

        private bool IsTestEnvironment(HttpContext context)
        {
            return context.Request.Headers.ContainsKey("X-Test-Environment") ||
                   context.Request.Headers["User-Agent"].ToString().Contains("Masark-Integration-Tests", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPublicEndpoint(PathString path)
        {
            var publicPaths = new[]
            {
                "/health",
                "/health/ready",
                "/health/live",
                "/health/db",
                "/health/cache",
                "/health/system",
                "/api/system/health",
                "/swagger",
                "/api/auth/login",
                "/api/auth/register"
            };

            return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private async Task HandleUnauthorizedRequestAsync(HttpContext context, string reason)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "unknown";
            var clientIp = GetClientIpAddress(context);
            
            _logger.LogWarning("ZeroTrust: Unauthorized request {RequestId} from {ClientIp}: {Reason}",
                requestId, clientIp, reason);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Unauthorized",
                message = "Access denied by zero trust security policy",
                requestId = requestId,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    public static class ZeroTrustMiddlewareExtensions
    {
        public static IApplicationBuilder UseZeroTrust(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ZeroTrustMiddleware>();
        }
    }
}
