using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Masark.Infrastructure.Identity;
using Masark.Domain.Entities;

namespace Masark.API.Controllers;

[ApiController]
[Route("api/rate-limits")]
[Authorize(Roles = "ADMIN,Administrator,Manager")]
public class RateLimitsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RateLimitsController> _logger;

    public RateLimitsController(ApplicationDbContext context, ILogger<RateLimitsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRateLimitConfigs()
    {
        try
        {
            var configs = await _context.RateLimitConfigs
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rate limit configurations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRateLimit([FromBody] CreateRateLimitRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Rate limit name is required" });
            }

            var tenantId = 1; // Get from tenant context
            var config = new RateLimitConfig(
                request.Name,
                request.RequestsPerMinute,
                request.RequestsPerHour,
                request.RequestsPerDay,
                request.BurstLimit,
                tenantId,
                request.AppliesTo,
                request.TargetId
            );

            _context.RateLimitConfigs.Add(config);
            await _context.SaveChangesAsync();

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rate limit configuration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{configId}")]
    public async Task<IActionResult> UpdateRateLimit(int configId, [FromBody] UpdateRateLimitRequest request)
    {
        try
        {
            var config = await _context.RateLimitConfigs.FindAsync(configId);
            if (config == null)
            {
                return NotFound(new { message = "Rate limit configuration not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                config.UpdateName(request.Name);
            
            if (request.RequestsPerMinute.HasValue || request.RequestsPerHour.HasValue || 
                request.RequestsPerDay.HasValue || request.BurstLimit.HasValue)
            {
                config.UpdateLimits(
                    request.RequestsPerMinute ?? config.RequestsPerMinute,
                    request.RequestsPerHour ?? config.RequestsPerHour,
                    request.RequestsPerDay ?? config.RequestsPerDay,
                    request.BurstLimit ?? config.BurstLimit
                );
            }
            
            if (!string.IsNullOrWhiteSpace(request.AppliesTo) || request.TargetId.HasValue)
            {
                config.UpdateTarget(
                    request.AppliesTo ?? config.AppliesTo,
                    request.TargetId ?? config.TargetId
                );
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Rate limit configuration updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rate limit configuration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{configId}")]
    public async Task<IActionResult> DeleteRateLimit(int configId)
    {
        try
        {
            var config = await _context.RateLimitConfigs.FindAsync(configId);
            if (config == null)
            {
                return NotFound(new { message = "Rate limit configuration not found" });
            }

            _context.RateLimitConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rate limit configuration deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rate limit configuration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPatch("{configId}/toggle")]
    public async Task<IActionResult> ToggleRateLimit(int configId, [FromBody] ToggleRateLimitRequest request)
    {
        try
        {
            var config = await _context.RateLimitConfigs.FindAsync(configId);
            if (config == null)
            {
                return NotFound(new { message = "Rate limit configuration not found" });
            }

            if (request.IsActive)
                config.Activate();
            else
                config.Deactivate();

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Rate limit configuration {(request.IsActive ? "activated" : "deactivated")} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling rate limit configuration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateRateLimitRequest
{
    public string Name { get; set; } = string.Empty;
    public int RequestsPerMinute { get; set; }
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
    public int BurstLimit { get; set; }
    public string AppliesTo { get; set; } = "all";
    public int? TargetId { get; set; }
}

public class UpdateRateLimitRequest
{
    public string? Name { get; set; }
    public int? RequestsPerMinute { get; set; }
    public int? RequestsPerHour { get; set; }
    public int? RequestsPerDay { get; set; }
    public int? BurstLimit { get; set; }
    public string? AppliesTo { get; set; }
    public int? TargetId { get; set; }
}

public class ToggleRateLimitRequest
{
    public bool IsActive { get; set; }
}
