using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Masark.Infrastructure.Identity;
using Masark.Domain.Entities;

namespace Masark.API.Controllers;

[ApiController]
[Route("api/api-usage")]
[Authorize(Roles = "Administrator,Manager")]
public class ApiUsageController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiUsageController> _logger;

    public ApiUsageController(ApplicationDbContext context, ILogger<ApiUsageController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetApiUsageStats(
        [FromQuery] string timeRange = "7d",
        [FromQuery] int? userId = null,
        [FromQuery] int? apiKeyId = null)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = timeRange switch
            {
                "1d" => endDate.AddDays(-1),
                "7d" => endDate.AddDays(-7),
                "30d" => endDate.AddDays(-30),
                "90d" => endDate.AddDays(-90),
                _ => endDate.AddDays(-7)
            };

            var query = _context.ApiUsageLogs.AsQueryable();
            
            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);
            
            if (apiKeyId.HasValue)
                query = query.Where(l => l.ApiKeyId == apiKeyId.Value);

            var logs = await query
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .ToListAsync();

            var totalRequests = logs.Count;
            var successfulRequests = logs.Count(l => l.StatusCode >= 200 && l.StatusCode < 400);
            var failedRequests = totalRequests - successfulRequests;
            var averageResponseTime = logs.Any() ? logs.Average(l => l.ResponseTimeMs) : 0;

            var last24Hours = logs.Count(l => l.Timestamp >= endDate.AddDays(-1));
            var last7Days = logs.Count(l => l.Timestamp >= endDate.AddDays(-7));
            var last30Days = logs.Count(l => l.Timestamp >= endDate.AddDays(-30));

            var topEndpoints = logs
                .GroupBy(l => l.Endpoint)
                .Select(g => new
                {
                    endpoint = g.Key,
                    count = g.Count(),
                    averageResponseTime = g.Average(l => l.ResponseTimeMs)
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            var usageByHour = logs
                .GroupBy(l => l.Timestamp.ToString("yyyy-MM-dd HH:00"))
                .Select(g => new
                {
                    hour = g.Key,
                    requests = g.Count()
                })
                .OrderBy(x => x.hour)
                .ToList();

            var errorsByType = logs
                .Where(l => l.StatusCode >= 400)
                .GroupBy(l => GetErrorType(l.StatusCode))
                .Select(g => new
                {
                    errorType = g.Key,
                    count = g.Count()
                })
                .ToList();

            var stats = new
            {
                totalRequests,
                successfulRequests,
                failedRequests,
                averageResponseTime,
                requestsLast24Hours = last24Hours,
                requestsLast7Days = last7Days,
                requestsLast30Days = last30Days,
                topEndpoints,
                usageByHour,
                errorsByType
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API usage stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetApiUsageHistory(
        [FromQuery] string timeRange = "7d",
        [FromQuery] int? userId = null,
        [FromQuery] int? apiKeyId = null)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = timeRange switch
            {
                "1d" => endDate.AddDays(-1),
                "7d" => endDate.AddDays(-7),
                "30d" => endDate.AddDays(-30),
                "90d" => endDate.AddDays(-90),
                _ => endDate.AddDays(-7)
            };

            var query = _context.ApiUsageLogs.AsQueryable();
            
            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);
            
            if (apiKeyId.HasValue)
                query = query.Where(l => l.ApiKeyId == apiKeyId.Value);

            var history = await query
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .Take(1000)
                .Select(l => new
                {
                    l.Id,
                    l.Endpoint,
                    l.Method,
                    l.StatusCode,
                    l.ResponseTimeMs,
                    l.Timestamp,
                    l.UserId,
                    l.ApiKeyId,
                    l.UserAgent,
                    l.IpAddress
                })
                .ToListAsync();

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API usage history");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("by-endpoint")]
    public async Task<IActionResult> GetApiUsageByEndpoint([FromQuery] string timeRange = "7d")
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = timeRange switch
            {
                "1d" => endDate.AddDays(-1),
                "7d" => endDate.AddDays(-7),
                "30d" => endDate.AddDays(-30),
                "90d" => endDate.AddDays(-90),
                _ => endDate.AddDays(-7)
            };

            var endpointStats = await _context.ApiUsageLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => new { l.Endpoint, l.Method })
                .Select(g => new
                {
                    endpoint = g.Key.Endpoint,
                    method = g.Key.Method,
                    totalRequests = g.Count(),
                    successfulRequests = g.Count(l => l.StatusCode >= 200 && l.StatusCode < 400),
                    averageResponseTime = g.Average(l => l.ResponseTimeMs),
                    minResponseTime = g.Min(l => l.ResponseTimeMs),
                    maxResponseTime = g.Max(l => l.ResponseTimeMs)
                })
                .OrderByDescending(x => x.totalRequests)
                .ToListAsync();

            return Ok(endpointStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API usage by endpoint");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("errors")]
    public async Task<IActionResult> GetApiErrorLogs(
        [FromQuery] int limit = 100,
        [FromQuery] string? severity = null)
    {
        try
        {
            var query = _context.ApiUsageLogs
                .Where(l => l.StatusCode >= 400);

            if (!string.IsNullOrEmpty(severity))
            {
                query = severity.ToLower() switch
                {
                    "warning" => query.Where(l => l.StatusCode >= 400 && l.StatusCode < 500),
                    "error" => query.Where(l => l.StatusCode >= 500),
                    _ => query
                };
            }

            var errorLogs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .Select(l => new
                {
                    l.Id,
                    l.Endpoint,
                    l.Method,
                    l.StatusCode,
                    l.ResponseTimeMs,
                    l.Timestamp,
                    l.UserId,
                    l.ApiKeyId,
                    l.UserAgent,
                    l.IpAddress,
                    l.ErrorMessage,
                    Severity = l.StatusCode >= 500 ? "Error" : "Warning"
                })
                .ToListAsync();

            return Ok(errorLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API error logs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private static string GetErrorType(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            429 => "Rate Limited",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => $"HTTP {statusCode}"
        };
    }
}
