using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;

namespace Masark.Domain.Entities
{
    public class SystemConfiguration : Entity, IAggregateRoot
    {
        public string Key { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DeploymentMode? DeploymentMode { get; private set; }

        protected SystemConfiguration() { }

        public SystemConfiguration(string key, string value, int tenantId) : base(tenantId)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
        }

        public void UpdateValue(string value)
        {
            Value = value;
            UpdateTimestamp();
        }

        public void UpdateDescription(string description)
        {
            Description = description;
            UpdateTimestamp();
        }

        public void SetDeploymentMode(DeploymentMode? deploymentMode)
        {
            DeploymentMode = deploymentMode;
            UpdateTimestamp();
        }

        public T GetValueAs<T>() where T : IConvertible
        {
            if (string.IsNullOrWhiteSpace(Value))
                return default(T);

            try
            {
                return (T)Convert.ChangeType(Value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot convert configuration value '{Value}' to type {typeof(T).Name}", ex);
            }
        }
    }
}
