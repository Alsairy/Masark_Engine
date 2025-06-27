using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class AuditLog : Entity
    {
        public int? AdminUserId { get; private set; }
        public string Action { get; private set; }
        public string EntityType { get; private set; }
        public int? EntityId { get; private set; }
        public string OldValues { get; private set; }
        public string NewValues { get; private set; }
        public string IpAddress { get; private set; }
        public string UserAgent { get; private set; }
        public DateTime Timestamp { get; private set; }

        public virtual AdminUser AdminUser { get; private set; }

        protected AuditLog() { }

        public AuditLog(string action, string entityType, int? entityId, 
                       string oldValues, string newValues, string ipAddress, 
                       string userAgent, int? adminUserId, int tenantId) : base(tenantId)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            EntityType = entityType;
            EntityId = entityId;
            OldValues = oldValues;
            NewValues = newValues;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            AdminUserId = adminUserId;
            Timestamp = DateTime.UtcNow;
        }

        public static AuditLog Create(string action, string entityType, int? entityId, 
                                    object oldValues, object newValues, string ipAddress, 
                                    string userAgent, int? adminUserId, int tenantId)
        {
            var oldValuesJson = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : null;
            var newValuesJson = newValues != null ? System.Text.Json.JsonSerializer.Serialize(newValues) : null;

            return new AuditLog(action, entityType, entityId, oldValuesJson, newValuesJson, 
                              ipAddress, userAgent, adminUserId, tenantId);
        }
    }
}
