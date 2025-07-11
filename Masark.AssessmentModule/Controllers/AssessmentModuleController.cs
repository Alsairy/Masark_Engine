using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Masark.AssessmentModule.Services;
using Masark.Domain.Enums;

namespace Masark.AssessmentModule.Controllers
{
    [ApiController]
    [Route("api/assessment-module")]
    [Authorize]
    public class AssessmentModuleController : ControllerBase
    {
        private readonly IAssessmentModuleService _assessmentModuleService;
        private readonly ILogger<AssessmentModuleController> _logger;

        public AssessmentModuleController(
            IAssessmentModuleService assessmentModuleService,
            ILogger<AssessmentModuleController> logger)
        {
            _assessmentModuleService = assessmentModuleService;
            _logger = logger;
        }

        [HttpPost("sessions")]
        [Authorize(Policy = "TakeAssessment")]
        public async Task<ActionResult<object>> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
                var userId = User.FindFirst("sub")?.Value ?? User.Identity?.Name ?? "anonymous";

                var session = await _assessmentModuleService.CreateSessionAsync(
                    userId, 
                    request.LanguagePreference, 
                    tenantId);

                return Ok(new
                {
                    sessionToken = session.SessionToken,
                    sessionId = session.Id,
                    state = session.State.ToString(),
                    createdAt = session.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assessment session");
                return StatusCode(500, new { error = "Failed to create assessment session" });
            }
        }

        [HttpPost("sessions/{sessionToken}/answers")]
        [Authorize(Policy = "TakeAssessment")]
        public async Task<ActionResult<object>> SubmitAnswer(
            string sessionToken,
            [FromBody] SubmitAnswerRequest request)
        {
            try
            {
                var success = await _assessmentModuleService.SubmitAnswerAsync(
                    sessionToken,
                    request.QuestionId,
                    request.SelectedOption,
                    request.PreferenceStrength);

                if (!success)
                {
                    return BadRequest(new { error = "Failed to submit answer" });
                }

                return Ok(new { success = true, message = "Answer submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer for session {SessionToken}", sessionToken);
                return StatusCode(500, new { error = "Failed to submit answer" });
            }
        }

        [HttpPost("sessions/{sessionToken}/complete")]
        [Authorize(Policy = "TakeAssessment")]
        public async Task<ActionResult<object>> CompleteAssessment(string sessionToken)
        {
            try
            {
                var result = await _assessmentModuleService.CompleteAssessmentAsync(sessionToken);

                return Ok(new
                {
                    sessionId = result.SessionId,
                    personalityType = result.PersonalityType,
                    dimensionScores = result.DimensionScores,
                    confidenceLevel = result.ConfidenceLevel,
                    completedAt = result.CompletedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing assessment for session {SessionToken}", sessionToken);
                return StatusCode(500, new { error = "Failed to complete assessment" });
            }
        }

        [HttpGet("questions")]
        [Authorize(Policy = "TakeAssessment")]
        public async Task<ActionResult<object>> GetQuestions([FromQuery] string language = "en")
        {
            try
            {
                var questions = await _assessmentModuleService.GetQuestionsAsync(language);
                return Ok(new { questions = questions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving questions for language {Language}", language);
                return StatusCode(500, new { error = "Failed to retrieve questions" });
            }
        }

        [HttpGet("statistics")]
        [Authorize(Policy = "ViewAssessmentResults")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
                var statistics = await _assessmentModuleService.GetStatisticsAsync(tenantId);

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assessment statistics");
                return StatusCode(500, new { error = "Failed to retrieve statistics" });
            }
        }
    }

    public class CreateSessionRequest
    {
        public string LanguagePreference { get; set; } = "en";
    }

    public class SubmitAnswerRequest
    {
        public int QuestionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
        public PreferenceStrength PreferenceStrength { get; set; }
    }
}
