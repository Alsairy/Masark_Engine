using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class User : Entity, IAggregateRoot
    {
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string FullName { get; private set; }
        public string Role { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? LastLogin { get; private set; }

        protected User() { }

        public User(string username, string email, string passwordHash, int tenantId) : base(tenantId)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            Role = "USER";
            IsActive = true;
        }

        public void UpdateProfile(string fullName, string email)
        {
            FullName = fullName;
            Email = email ?? throw new ArgumentNullException(nameof(email));
            UpdateTimestamp();
        }

        public void UpdatePassword(string passwordHash)
        {
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            UpdateTimestamp();
        }

        public void SetRole(string role)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
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
            LastLogin = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
