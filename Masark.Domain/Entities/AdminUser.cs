using Masark.Domain.Common;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class AdminUser : Entity, IAggregateRoot
    {
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Role { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public bool IsSuperAdmin { get; private set; }
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public DateTime? LastLoginAt { get; private set; }

        public virtual ICollection<AuditLog> AuditLogs { get; private set; }

        protected AdminUser() 
        {
            AuditLogs = new List<AuditLog>();
        }

        public AdminUser(string username, string email, string passwordHash, int tenantId) : base(tenantId)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            Role = "admin";
            IsActive = true;
            IsSuperAdmin = false;
            AuditLogs = new List<AuditLog>();
        }

        public void UpdateProfile(string firstName, string lastName, string email)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email ?? throw new ArgumentNullException(nameof(email));
            UpdateTimestamp();
        }

        public void UpdatePassword(string passwordHash)
        {
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            UpdateTimestamp();
        }

        public void SetRole(string role, bool isSuperAdmin = false)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            IsSuperAdmin = isSuperAdmin;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public void RecordLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public string GetFullName()
        {
            if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
                return Username;
            
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
