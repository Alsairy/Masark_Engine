using Masark.Domain.Common;

namespace Masark.Domain.Entities;

public class ApiUsageLog : Entity
{
    public string Endpoint { get; private set; } = string.Empty;
    public string Method { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public double ResponseTimeMs { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int? UserId { get; private set; }
    public int? ApiKeyId { get; private set; }
    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    public string? ErrorMessage { get; private set; }

    protected ApiUsageLog() { }

    public ApiUsageLog(string endpoint, string method, int statusCode, 
                       double responseTimeMs, int tenantId, int? userId = null, 
                       int? apiKeyId = null, string? userAgent = null, 
                       string? ipAddress = null, string? errorMessage = null) : base(tenantId)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        StatusCode = statusCode;
        ResponseTimeMs = responseTimeMs;
        Timestamp = DateTime.UtcNow;
        UserId = userId;
        ApiKeyId = apiKeyId;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        ErrorMessage = errorMessage;
    }
}
