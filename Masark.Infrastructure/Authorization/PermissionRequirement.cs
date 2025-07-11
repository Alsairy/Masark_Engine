using Microsoft.AspNetCore.Authorization;

namespace Masark.Infrastructure.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var permissionClaim = context.User.FindFirst("permission");
            if (permissionClaim != null && permissionClaim.Value == requirement.Permission)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class TenantAccessRequirement : IAuthorizationRequirement
    {
        public string RequiredTenantId { get; }

        public TenantAccessRequirement(string requiredTenantId)
        {
            RequiredTenantId = requiredTenantId;
        }
    }

    public class TenantAccessAuthorizationHandler : AuthorizationHandler<TenantAccessRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TenantAccessRequirement requirement)
        {
            var tenantClaim = context.User.FindFirst("tenant_id");
            if (tenantClaim != null && tenantClaim.Value == requirement.RequiredTenantId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class MfaRequirement : IAuthorizationRequirement
    {
    }

    public class MfaAuthorizationHandler : AuthorizationHandler<MfaRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MfaRequirement requirement)
        {
            var mfaClaim = context.User.FindFirst("mfa_verified");
            if (mfaClaim != null && mfaClaim.Value == "true")
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
