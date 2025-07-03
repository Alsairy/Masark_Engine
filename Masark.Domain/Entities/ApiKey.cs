using Masark.Domain.Common;

namespace Masark.Domain.Entities;

public class ApiKey : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public int UserId { get; private set; }
    public bool IsActive { get; private set; }
    public List<string> Permissions { get; private set; } = new();
    public int RateLimit { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime? LastUsed { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    protected ApiKey()
    {
        Permissions = new List<string>();
    }

    public ApiKey(string name, string key, int userId, int tenantId, 
                  List<string>? permissions = null, int rateLimit = 1000, 
                  DateTime? expiresAt = null) : base(tenantId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        UserId = userId;
        IsActive = true;
        Permissions = permissions ?? new List<string> { "read" };
        RateLimit = rateLimit;
        UsageCount = 0;
        ExpiresAt = expiresAt;
    }

    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UpdateTimestamp();
    }

    public void UpdatePermissions(List<string> permissions)
    {
        Permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        UpdateTimestamp();
    }

    public void UpdateRateLimit(int rateLimit)
    {
        RateLimit = rateLimit;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void RegenerateKey(string newKey)
    {
        Key = newKey ?? throw new ArgumentNullException(nameof(newKey));
        UpdateTimestamp();
    }

    public void RecordUsage()
    {
        UsageCount++;
        LastUsed = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }
}
