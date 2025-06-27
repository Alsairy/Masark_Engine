using Masark.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Masark.Infrastructure.Middleware
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantContextAccessor)
        {
            var tenantId = ResolveTenantId(context);
            
            tenantContextAccessor.TenantContext = new TenantContext
            {
                TenantId = tenantId,
                TenantName = GetTenantName(tenantId),
                TenantCode = GetTenantCode(tenantId)
            };

            await _next(context);
        }

        private int ResolveTenantId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
            {
                if (int.TryParse(tenantHeader.FirstOrDefault(), out var headerTenantId) && headerTenantId > 0)
                {
                    return headerTenantId;
                }
            }

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
                if (!string.IsNullOrEmpty(tenantClaim) && int.TryParse(tenantClaim, out var claimTenantId) && claimTenantId > 0)
                {
                    return claimTenantId;
                }
            }

            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host) && host.Contains('.'))
            {
                var subdomain = host.Split('.')[0];
                var tenantId = GetTenantIdFromSubdomain(subdomain);
                if (tenantId > 0)
                {
                    return tenantId;
                }
            }

            if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
            {
                if (int.TryParse(tenantQuery.FirstOrDefault(), out var queryTenantId) && queryTenantId > 0)
                {
                    return queryTenantId;
                }
            }

            return 1;
        }

        private int GetTenantIdFromSubdomain(string subdomain)
        {
            return subdomain.ToLower() switch
            {
                "demo" => 1,
                "test" => 2,
                "staging" => 3,
                _ => 0
            };
        }

        private string? GetTenantName(int tenantId)
        {
            return tenantId switch
            {
                1 => "Demo Tenant",
                2 => "Test Tenant", 
                3 => "Staging Tenant",
                _ => "Default Tenant"
            };
        }

        private string? GetTenantCode(int tenantId)
        {
            return tenantId switch
            {
                1 => "DEMO",
                2 => "TEST",
                3 => "STAGING", 
                _ => "DEFAULT"
            };
        }
    }
}
