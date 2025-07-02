using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Masark.Application.Commands.Assessment;
using Masark.Application.Queries.Assessment;
using Masark.Application.Services;
using Masark.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssessmentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AssessmentController> _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IAssessmentStateMachineService _stateMachineService;

        public AssessmentController(IMediator mediator, ILogger<AssessmentController> logger, ILocalizationService localizationService, IAssessmentStateMachineService stateMachineService)
        {
            _mediator = mediator;
            _logger = logger;
            _localizationService = localizationService;
            _stateMachineService = stateMachineService;
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
                
                var languagePreference = request.LanguagePreference ?? GetLanguageFromRequest();

                var command = new StartAssessmentSessionCommand
                {
                    SessionToken = sessionToken,
                    TenantId = request.TenantId,
                    LanguagePreference = languagePreference,
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
        public async Task<IActionResult> GetAssessmentQuestions([FromQuery] string? sessionToken, [FromQuery] string? language = null)
        {
            try
            {
                var requestLanguage = language ?? GetLanguageFromRequest();
                
                var query = new GetQuestionsQuery
                {
                    TenantId = 1,
                    Language = requestLanguage
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
                            text = q.GetText(requestLanguage),
                            dimension = q.Dimension.ToString(),
                            order = q.OrderNumber,
                            options = new[]
                            {
                                new { value = "A", text = q.GetOptionAText(requestLanguage) },
                                new { value = "B", text = q.GetOptionBText(requestLanguage) }
                            },
                            is_reverse_scored = false
                        }),
                        total_questions = result.Questions.Count,
                        language = requestLanguage
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
                    SelectedOption = request.AnswerValue == 1 ? "A" : "B",
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

        private string GetLanguageFromRequest()
        {
            try
            {
                var headers = new Dictionary<string, string>();
                var queryParams = new Dictionary<string, string>();

                foreach (var header in Request.Headers)
                {
                    headers[header.Key] = header.Value.ToString();
                }

                foreach (var param in Request.Query)
                {
                    queryParams[param.Key] = param.Value.ToString();
                }

                return _localizationService.GetLanguageFromHeaders(headers, queryParams);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining language from request, falling back to default");
                return "en";
            }
        }

        [HttpGet("{sessionId}/state")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> GetAssessmentState(int sessionId, [FromQuery] string language = "en")
        {
            try
            {
                var stateInfo = await _stateMachineService.GetCurrentStateInfoAsync(sessionId);
                
                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    current_state = stateInfo.CurrentState.ToString(),
                    previous_state = stateInfo.PreviousState?.ToString(),
                    state_entered_at = stateInfo.StateEnteredAt,
                    progress_percentage = stateInfo.ProgressPercentage,
                    can_progress = stateInfo.CanProgress,
                    blocking_reason = stateInfo.BlockingReason,
                    allowed_transitions = stateInfo.AllowedTransitions,
                    state_data = stateInfo.StateData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment state for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "STATE_RETRIEVAL_ERROR"
                });
            }
        }

        [HttpPost("{sessionId}/transition")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> TransitionAssessmentState(
            int sessionId,
            [FromBody] StateTransitionRequest request,
            [FromQuery] string language = "en")
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                if (!Enum.TryParse<AssessmentState>(request.TargetState, out var targetState))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid target state",
                        error_code = "INVALID_TARGET_STATE"
                    });
                }

                var result = await _stateMachineService.TransitionToStateAsync(sessionId, targetState, request.TransitionData);
                
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                        error_code = "STATE_TRANSITION_FAILED"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    new_state = result.NewState.ToString(),
                    state_data = result.StateData,
                    allowed_actions = result.AllowedActions,
                    requires_tie_breaker = result.RequiresTieBreaker,
                    tie_breaker_questions = result.TieBreakerQuestions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning assessment state for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "STATE_TRANSITION_ERROR"
                });
            }
        }

        [HttpPost("{sessionId}/career-clusters")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> SubmitCareerClusterRatings(
            int sessionId,
            [FromBody] CareerClusterRatingsRequest request,
            [FromQuery] string language = "en")
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var result = await _stateMachineService.ProcessClusterRatingSubmissionAsync(sessionId, request.ClusterRatings);
                
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                        error_code = "CLUSTER_RATING_FAILED"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    new_state = result.NewState.ToString(),
                    message = "Career cluster ratings submitted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting career cluster ratings for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "CLUSTER_RATING_ERROR"
                });
            }
        }

        [HttpPost("{sessionId}/calculate")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> CalculateAssessment(
            int sessionId,
            [FromQuery] string language = "en")
        {
            try
            {
                var result = await _stateMachineService.TransitionToStateAsync(sessionId, AssessmentState.CalculateAssessment);
                
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                        error_code = "CALCULATION_FAILED"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    state = result.NewState.ToString(),
                    calculation_status = "in_progress",
                    message = "Assessment calculation started"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating assessment for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "CALCULATION_ERROR"
                });
            }
        }

        [HttpPost("{sessionId}/tie-breaker")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> SubmitTieBreakerAnswers(
            int sessionId,
            [FromBody] TieBreakerAnswersRequest request,
            [FromQuery] string language = "en")
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var result = await _stateMachineService.ProcessTieBreakerResolutionAsync(sessionId, request.TieBreakerAnswers);
                
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                        error_code = "TIE_BREAKER_FAILED"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    new_state = result.NewState.ToString(),
                    message = "Tie-breaker questions resolved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting tie-breaker answers for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "TIE_BREAKER_ERROR"
                });
            }
        }

        [HttpPost("{sessionId}/rate-assessment")]
        [EnableRateLimiting("AssessmentRateLimit")]
        public async Task<IActionResult> RateAssessment(
            int sessionId,
            [FromBody] AssessmentRatingRequest request,
            [FromQuery] string language = "en")
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var result = await _stateMachineService.ProcessAssessmentRatingAsync(sessionId, request.Rating, request.Feedback);
                
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.ErrorMessage,
                        error_code = "ASSESSMENT_RATING_FAILED"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_id = sessionId,
                    new_state = result.NewState.ToString(),
                    message = "Assessment rating submitted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating assessment for session {SessionId}", sessionId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error_code = "ASSESSMENT_RATING_ERROR"
                });
            }
        }
    }

    public class StartAssessmentSessionRequest
    {
        [Required(ErrorMessage = "Student name is required")]
        [MinLength(2, ErrorMessage = "Student name must be at least 2 characters")]
        public string StudentName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Student email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string StudentEmail { get; set; } = string.Empty;
        
        public string? StudentId { get; set; }
        public string? DeploymentMode { get; set; } = "STANDARD";
        public string? LanguagePreference { get; set; } = "en";
        
        [Range(1, int.MaxValue, ErrorMessage = "Tenant ID must be a positive number")]
        public int TenantId { get; set; } = 1;
    }

    public class SubmitAnswerRequest
    {
        [Required(ErrorMessage = "Session token is required")]
        public string SessionToken { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue, ErrorMessage = "Question ID must be a positive number")]
        public int QuestionId { get; set; }
        
        [Range(1, 2, ErrorMessage = "Answer value must be 1 or 2")]
        public int AnswerValue { get; set; }
        
        public int? TenantId { get; set; }
    }

    public class CompleteAssessmentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Session ID must be a positive number")]
        public int SessionId { get; set; }
        
        public int? TenantId { get; set; }
    }

    public class StateTransitionRequest
    {
        [Required]
        public string TargetState { get; set; } = string.Empty;
        public Dictionary<string, object>? TransitionData { get; set; }
    }


    public class TieBreakerAnswersRequest
    {
        [Required]
        public Dictionary<int, string> TieBreakerAnswers { get; set; } = new();
    }

    public class CareerClusterRatingsRequest
    {
        [Required]
        public Dictionary<int, int> ClusterRatings { get; set; } = new();
    }

    public class AssessmentRatingRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        public string? Feedback { get; set; }
    }
}
