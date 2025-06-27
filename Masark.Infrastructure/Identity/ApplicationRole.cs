using Microsoft.AspNetCore.Identity;
using Masark.Domain.Common;

namespace Masark.Infrastructure.Identity
{
    public class ApplicationRole : IdentityRole, ITenantEntity
    {
        public int TenantId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
