using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Masark.Infrastructure.Services;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CdnController : ControllerBase
    {
        private readonly ICdnService _cdnService;
        private readonly ILogger<CdnController> _logger;

        public CdnController(ICdnService cdnService, ILogger<CdnController> logger)
        {
            _cdnService = cdnService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<Dictionary<string, object>>> GetCdnStats()
        {
            try
            {
                var stats = await _cdnService.GetCdnStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CDN statistics");
                return StatusCode(500, new { error = "Failed to retrieve CDN statistics" });
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult<object>> UploadAsset(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var content = memoryStream.ToArray();

                var cdnUrl = await _cdnService.UploadAssetAsync(file.FileName, content, file.ContentType);
                
                return Ok(new 
                { 
                    success = true,
                    fileName = file.FileName,
                    cdnUrl = cdnUrl,
                    size = file.Length,
                    contentType = file.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading asset to CDN: {FileName}", file.FileName);
                return StatusCode(500, new { error = "Failed to upload asset to CDN" });
            }
        }

        [HttpPost("invalidate")]
        public async Task<ActionResult<object>> InvalidateCache([FromBody] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return BadRequest(new { error = "Asset path is required" });
            }

            try
            {
                var success = await _cdnService.InvalidateCacheAsync(assetPath);
                
                return Ok(new 
                { 
                    success = success,
                    assetPath = assetPath,
                    message = success ? "Cache invalidated successfully" : "Cache invalidation failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating CDN cache for: {AssetPath}", assetPath);
                return StatusCode(500, new { error = "Failed to invalidate CDN cache" });
            }
        }

        [HttpGet("url")]
        public ActionResult<object> GetCdnUrl([FromQuery] string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return BadRequest(new { error = "Asset path is required" });
            }

            try
            {
                var cdnUrl = _cdnService.GetCdnUrl(assetPath);
                
                return Ok(new 
                { 
                    originalPath = assetPath,
                    cdnUrl = cdnUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CDN URL for: {AssetPath}", assetPath);
                return StatusCode(500, new { error = "Failed to generate CDN URL" });
            }
        }

        [HttpGet("image-url")]
        public ActionResult<object> GetOptimizedImageUrl(
            [FromQuery] string imagePath,
            [FromQuery] int? width = null,
            [FromQuery] int? height = null,
            [FromQuery] string? format = null)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return BadRequest(new { error = "Image path is required" });
            }

            try
            {
                var optimizedUrl = _cdnService.GenerateOptimizedImageUrl(imagePath, width, height, format);
                
                return Ok(new 
                { 
                    originalPath = imagePath,
                    optimizedUrl = optimizedUrl,
                    parameters = new 
                    {
                        width = width,
                        height = height,
                        format = format
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating optimized image URL for: {ImagePath}", imagePath);
                return StatusCode(500, new { error = "Failed to generate optimized image URL" });
            }
        }
    }
}
