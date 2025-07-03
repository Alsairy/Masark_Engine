using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Masark.Infrastructure.Identity;
using Masark.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Masark.API.Controllers;

[ApiController]
[Route("api/api-keys")]
[Authorize(Roles = "ADMIN,Administrator,Manager")]
public class ApiKeysController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(ApplicationDbContext context, ILogger<ApiKeysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetApiKeys([FromQuery] int? userId = null)
    {
        try
        {
            var query = _context.ApiKeys.AsQueryable();
            
            if (userId.HasValue)
            {
                query = query.Where(k => k.UserId == userId.Value);
            }

            var apiKeys = await query
                .Select(k => new
                {
                    k.Id,
                    k.Name,
                    Key = k.IsActive ? MaskApiKey(k.Key) : "***INACTIVE***",
                    k.UserId,
                    UserName = "User", // Will be populated from Users table separately
                    k.IsActive,
                    k.Permissions,
                    k.RateLimit,
                    k.UsageCount,
                    k.LastUsed,
                    k.ExpiresAt,
                    k.CreatedAt,
                    k.UpdatedAt
                })
                .ToListAsync();

            return Ok(apiKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "API key name is required" });
            }

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            var tenantId = 1; // Get from tenant context
            var apiKey = new ApiKey(
                request.Name,
                GenerateApiKey(),
                request.UserId,
                tenantId,
                request.Permissions ?? new List<string> { "read" },
                request.RateLimit ?? 1000,
                request.ExpiresAt
            );

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                apiKey.Id,
                apiKey.Name,
                apiKey.Key,
                apiKey.UserId,
                UserName = user.UserName,
                apiKey.IsActive,
                apiKey.Permissions,
                apiKey.RateLimit,
                apiKey.UsageCount,
                apiKey.LastUsed,
                apiKey.ExpiresAt,
                apiKey.CreatedAt,
                apiKey.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{keyId}")]
    public async Task<IActionResult> UpdateApiKey(int keyId, [FromBody] UpdateApiKeyRequest request)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(keyId);
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                apiKey.UpdateName(request.Name);
            
            if (request.Permissions != null)
                apiKey.UpdatePermissions(request.Permissions);
            
            if (request.RateLimit.HasValue)
                apiKey.UpdateRateLimit(request.RateLimit.Value);
            
            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    apiKey.Activate();
                else
                    apiKey.Deactivate();
            }
            
            if (request.ExpiresAt.HasValue)
                apiKey.SetExpiration(request.ExpiresAt.Value);

            await _context.SaveChangesAsync();

            return Ok(new { message = "API key updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API key");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{keyId}")]
    public async Task<IActionResult> DeleteApiKey(int keyId)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(keyId);
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();

            return Ok(new { message = "API key deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API key");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{keyId}/regenerate")]
    public async Task<IActionResult> RegenerateApiKey(int keyId)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(keyId);
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            apiKey.RegenerateKey(GenerateApiKey());

            await _context.SaveChangesAsync();

            return Ok(new { key = apiKey.Key, message = "API key regenerated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating API key");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPatch("{keyId}/toggle")]
    public async Task<IActionResult> ToggleApiKey(int keyId, [FromBody] ToggleApiKeyRequest request)
    {
        try
        {
            var apiKey = await _context.ApiKeys.FindAsync(keyId);
            if (apiKey == null)
            {
                return NotFound(new { message = "API key not found" });
            }

            if (request.IsActive)
                apiKey.Activate();
            else
                apiKey.Deactivate();

            await _context.SaveChangesAsync();

            return Ok(new { message = $"API key {(request.IsActive ? "activated" : "deactivated")} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling API key");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private static string GenerateApiKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string MaskApiKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8)
            return "***";
        
        return key[..4] + "..." + key[^4..];
    }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<string>? Permissions { get; set; }
    public int? RateLimit { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public List<string>? Permissions { get; set; }
    public int? RateLimit { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ToggleApiKeyRequest
{
    public bool IsActive { get; set; }
}
