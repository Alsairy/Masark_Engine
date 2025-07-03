using Masark.Domain.Common;

namespace Masark.Domain.Entities;

public class RateLimitConfig : Entity
{
    public string Name { get; private set; } = string.Empty;
    public int RequestsPerMinute { get; private set; }
    public int RequestsPerHour { get; private set; }
    public int RequestsPerDay { get; private set; }
    public int BurstLimit { get; private set; }
    public bool IsActive { get; private set; }
    public string AppliesTo { get; private set; } = "all";
    public int? TargetId { get; private set; }

    protected RateLimitConfig() { }

    public RateLimitConfig(string name, int requestsPerMinute, int requestsPerHour, 
                          int requestsPerDay, int burstLimit, int tenantId,
                          string appliesTo = "all", int? targetId = null) : base(tenantId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RequestsPerMinute = requestsPerMinute;
        RequestsPerHour = requestsPerHour;
        RequestsPerDay = requestsPerDay;
        BurstLimit = burstLimit;
        IsActive = true;
        AppliesTo = appliesTo ?? "all";
        TargetId = targetId;
    }

    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UpdateTimestamp();
    }

    public void UpdateLimits(int requestsPerMinute, int requestsPerHour, 
                           int requestsPerDay, int burstLimit)
    {
        RequestsPerMinute = requestsPerMinute;
        RequestsPerHour = requestsPerHour;
        RequestsPerDay = requestsPerDay;
        BurstLimit = burstLimit;
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

    public void UpdateTarget(string appliesTo, int? targetId = null)
    {
        AppliesTo = appliesTo ?? throw new ArgumentNullException(nameof(appliesTo));
        TargetId = targetId;
        UpdateTimestamp();
    }
}
