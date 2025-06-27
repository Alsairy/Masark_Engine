using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Masark.Application.Commands.Assessment;
using Masark.Application.Queries.Assessment;
using Masark.Domain.Enums;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssessmentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AssessmentController> _logger;

        public AssessmentController(IMediator mediator, ILogger<AssessmentController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "Masark Assessment Engine",
                timestamp = DateTime.UtcNow.ToString("O")
            });
        }

        [HttpPost("start-session")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> StartAssessmentSession([FromBody] StartAssessmentSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var sessionToken = Guid.NewGuid().ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                var userAgent = Request.Headers["User-Agent"].ToString();

                var command = new StartAssessmentSessionCommand
                {
                    SessionToken = sessionToken,
                    TenantId = request.TenantId ?? 1,
                    LanguagePreference = request.LanguagePreference ?? "en",
                    UserAgent = userAgent,
                    IpAddress = ipAddress,
                    DeploymentMode = Enum.TryParse<DeploymentMode>(request.DeploymentMode?.ToUpper(), out var mode) ? mode : DeploymentMode.STANDARD
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        session_token = sessionToken,
                        session_id = result.SessionId,
                        started_at = result.StartedAt.ToString("O"),
                        deployment_mode = command.DeploymentMode.ToString(),
                        language_preference = command.LanguagePreference
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting assessment session");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to start assessment session",
                    message = ex.Message
                });
            }
        }

        [HttpGet("questions")]
        public async Task<IActionResult> GetAssessmentQuestions([FromQuery] string? sessionToken, [FromQuery] string? language = "en")
        {
            try
            {
                var query = new GetQuestionsQuery
                {
                    TenantId = 1,
                    Language = language ?? "en"
                };

                var result = await _mediator.Send(query);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        questions = result.Questions.Select(q => new
                        {
                            id = q.Id,
                            text = q.GetText(language ?? "en"),
                            dimension = q.Dimension.ToString(),
                            order = q.OrderNumber,
                            options = new[]
                            {
                                new { value = "A", text = q.GetOptionAText(language ?? "en") },
                                new { value = "B", text = q.GetOptionBText(language ?? "en") }
                            },
                            is_reverse_scored = false
                        }),
                        total_questions = result.Questions.Count,
                        language = language
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment questions");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get assessment questions",
                    message = ex.Message
                });
            }
        }

        [HttpPost("submit-answer")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var command = new SubmitAnswerCommand
                {
                    SessionId = 1, // TODO: Get session ID from token
                    QuestionId = request.QuestionId,
                    SelectedOption = request.AnswerValue.ToString(),
                    TenantId = request.TenantId ?? 1
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Answer submitted successfully",
                        question_id = request.QuestionId,
                        answer_value = request.AnswerValue,
                        submitted_at = DateTime.UtcNow.ToString("O"),
                        total_answered = result.TotalAnswered,
                        total_questions = result.TotalQuestions,
                        is_complete = result.IsComplete
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to submit answer",
                    message = ex.Message
                });
            }
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteAssessment([FromBody] CompleteAssessmentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var command = new CompleteAssessmentCommand
                {
                    SessionId = request.SessionId,
                    TenantId = request.TenantId ?? 1
                };

                var result = await _mediator.Send(command);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Assessment completed successfully",
                        personality_type = result.PersonalityType,
                        dimension_scores = result.DimensionScores,
                        completed_at = result.CompletedAt.ToString("O")
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing assessment");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to complete assessment",
                    message = ex.Message
                });
            }
        }

        [HttpGet("results/{sessionId}")]
        public async Task<IActionResult> GetAssessmentResults(int sessionId, [FromQuery] bool includeStatistics = false, [FromQuery] int? tenantId = null)
        {
            try
            {
                var query = new GetAssessmentResultsQuery
                {
                    SessionId = sessionId,
                    TenantId = tenantId ?? 1,
                    IncludeStatistics = includeStatistics
                };

                var result = await _mediator.Send(query);

                if (result.Success)
                {
                    var response = new
                    {
                        success = true,
                        session_id = sessionId,
                        personality_type = result.PersonalityType,
                        dimension_scores = result.DimensionScores,
                        session_info = new
                        {
                            started_at = result.Session?.StartedAt.ToString("O"),
                            completed_at = result.Session?.CompletedAt?.ToString("O"),
                            language_preference = result.Session?.LanguagePreference,
                            deployment_mode = result.Session?.DeploymentMode.ToString()
                        }
                    };

                    if (includeStatistics && result.Statistics != null)
                    {
                        return Ok(new
                        {
                            success = response.success,
                            session_id = response.session_id,
                            personality_type = response.personality_type,
                            dimension_scores = response.dimension_scores,
                            session_info = response.session_info,
                            statistics = result.Statistics
                        });
                    }

                    return Ok(response);
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment results for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get assessment results",
                    message = ex.Message
                });
            }
        }
    }

    public class StartAssessmentSessionRequest
    {
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentId { get; set; }
        public string? DeploymentMode { get; set; } = "STANDARD";
        public string? LanguagePreference { get; set; } = "en";
        public int? TenantId { get; set; }
    }

    public class SubmitAnswerRequest
    {
        public string SessionToken { get; set; } = string.Empty;
        public int QuestionId { get; set; }
        public int AnswerValue { get; set; }
        public int? TenantId { get; set; }
    }

    public class CompleteAssessmentRequest
    {
        public int SessionId { get; set; }
        public int? TenantId { get; set; }
    }
}
