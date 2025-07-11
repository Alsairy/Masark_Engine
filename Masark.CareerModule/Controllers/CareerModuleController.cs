using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Masark.CareerModule.Services;

namespace Masark.CareerModule.Controllers
{
    [ApiController]
    [Route("api/career-module")]
    [Authorize]
    public class CareerModuleController : ControllerBase
    {
        private readonly ICareerModuleService _careerModuleService;
        private readonly ILogger<CareerModuleController> _logger;

        public CareerModuleController(
            ICareerModuleService careerModuleService,
            ILogger<CareerModuleController> logger)
        {
            _careerModuleService = careerModuleService;
            _logger = logger;
        }

        [HttpGet("careers")]
        [Authorize(Policy = "ViewCareerData")]
        public async Task<ActionResult<object>> GetCareers([FromQuery] string language = "en")
        {
            try
            {
                var careers = await _careerModuleService.GetCareersAsync(language);
                return Ok(new { careers = careers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving careers for language {Language}", language);
                return StatusCode(500, new { error = "Failed to retrieve careers" });
            }
        }

        [HttpGet("matches/{personalityType}")]
        [Authorize(Policy = "ViewCareerData")]
        public async Task<ActionResult<object>> GetCareerMatches(
            string personalityType,
            [FromQuery] string language = "en")
        {
            try
            {
                var matches = await _careerModuleService.GetCareerMatchesAsync(personalityType, language);
                return Ok(new { matches = matches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving career matches for personality type {PersonalityType}", personalityType);
                return StatusCode(500, new { error = "Failed to retrieve career matches" });
            }
        }

        [HttpGet("clusters")]
        [Authorize(Policy = "ViewCareerData")]
        public async Task<ActionResult<object>> GetCareerClusters([FromQuery] string language = "en")
        {
            try
            {
                var clusters = await _careerModuleService.GetCareerClustersAsync(language);
                return Ok(new { clusters = clusters });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving career clusters for language {Language}", language);
                return StatusCode(500, new { error = "Failed to retrieve career clusters" });
            }
        }

        [HttpGet("recommendations/{userId}")]
        [Authorize(Policy = "ViewCareerData")]
        public async Task<ActionResult<object>> GetPersonalizedRecommendations(
            string userId,
            [FromQuery] string personalityType)
        {
            try
            {
                var currentUserId = User.FindFirst("sub")?.Value ?? User.Identity?.Name;
                
                if (currentUserId != userId && !User.IsInRole("ADMIN"))
                {
                    return Forbid("You can only access your own recommendations");
                }

                var recommendations = await _careerModuleService.GetPersonalizedRecommendationsAsync(userId, personalityType);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personalized recommendations for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to retrieve personalized recommendations" });
            }
        }

        [HttpPut("careers/{careerId}")]
        [Authorize(Policy = "ManageCareers")]
        public async Task<ActionResult<object>> UpdateCareer(
            int careerId,
            [FromBody] UpdateCareerRequest request)
        {
            try
            {
                var career = new Domain.Entities.Career
                {
                    Id = careerId,
                    Title = request.Title,
                    Description = request.Description,
                    RequiredSkills = request.RequiredSkills,
                    SalaryRange = request.SalaryRange,
                    EducationLevel = request.EducationLevel,
                    Language = request.Language
                };

                var success = await _careerModuleService.UpdateCareerDataAsync(career);
                
                if (!success)
                {
                    return BadRequest(new { error = "Failed to update career data" });
                }

                return Ok(new { success = true, message = "Career updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating career {CareerId}", careerId);
                return StatusCode(500, new { error = "Failed to update career" });
            }
        }

        [HttpGet("analytics")]
        [Authorize(Policy = "ViewCareerData")]
        public async Task<ActionResult<object>> GetCareerAnalytics()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
                var analytics = await _careerModuleService.GetCareerAnalyticsAsync(tenantId);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving career analytics");
                return StatusCode(500, new { error = "Failed to retrieve career analytics" });
            }
        }
    }

    public class UpdateCareerRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string SalaryRange { get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
    }
}
