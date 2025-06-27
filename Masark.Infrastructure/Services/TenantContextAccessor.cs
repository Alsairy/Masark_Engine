using Masark.Domain.Common;

namespace Masark.Infrastructure.Services
{
    public interface ITenantContextAccessor
    {
        TenantContext? TenantContext { get; set; }
    }

    public class TenantContextAccessor : ITenantContextAccessor
    {
        private static readonly AsyncLocal<TenantContext?> _tenantContext = new();

        public TenantContext? TenantContext
        {
            get => _tenantContext.Value;
            set => _tenantContext.Value = value;
        }
    }

    public class TenantContext
    {
        public int TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? TenantCode { get; set; }
    }
}
