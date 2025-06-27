using Microsoft.AspNetCore.Identity;
using Masark.Domain.Common;

namespace Masark.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser, ITenantEntity
    {
        public int TenantId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        public string GetFullName() => $"{FirstName} {LastName}".Trim();
    }
}
