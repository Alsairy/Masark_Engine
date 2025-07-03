using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ILogger<ReportsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
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

                if (string.IsNullOrWhiteSpace(request.SessionToken))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "session_token is required"
                    });
                }

                var language = request.Language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                var reportType = request.ReportType?.ToLower() ?? "comprehensive";
                if (!new[] { "comprehensive", "summary" }.Contains(reportType))
                {
                    reportType = "comprehensive";
                }

                var filename = $"report_{request.SessionToken}_{reportType}_{language}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                var filePath = $"/reports/{filename}";

                return Ok(new
                {
                    success = true,
                    report = new
                    {
                        filename = filename,
                        file_path = filePath,
                        file_size_bytes = 0,
                        report_type = reportType,
                        language = language,
                        session_token = request.SessionToken,
                        personality_type = "INTJ", // Placeholder
                        student_name = "Student Name", // Placeholder
                        generated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to generate report",
                    message = ex.Message
                });
            }
        }

        [HttpGet("download/{filename}")]
        public async Task<IActionResult> DownloadReport(string filename)
        {
            try
            {
                if (!filename.EndsWith(".pdf") || filename.Contains("..") || filename.Contains("/"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid filename"
                    });
                }

                return NotFound(new
                {
                    success = false,
                    error = "Report file not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading report {Filename}", filename);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to download report",
                    message = ex.Message
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListReports([FromQuery] int limit = 50)
        {
            try
            {
                limit = Math.Min(limit, 200);

                return Ok(new
                {
                    success = true,
                    reports = new object[0],
                    total_reports = 0,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing reports");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to list reports",
                    message = ex.Message
                });
            }
        }

        [HttpDelete("delete/{filename}")]
        public async Task<IActionResult> DeleteReport(string filename)
        {
            try
            {
                if (!filename.EndsWith(".pdf") || filename.Contains("..") || filename.Contains("/"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid filename"
                    });
                }

                return NotFound(new
                {
                    success = false,
                    error = "Report file not found or could not be deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {Filename}", filename);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete report",
                    message = ex.Message
                });
            }
        }

        [HttpGet("session/{sessionToken}")]
        public async Task<IActionResult> GetSessionReports(string sessionToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionToken))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid session token"
                    });
                }

                return Ok(new
                {
                    success = true,
                    session_token = sessionToken,
                    personality_type = "INTJ", // Placeholder
                    student_name = "Student Name", // Placeholder
                    reports = new object[0],
                    total_reports = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session reports for {SessionToken}", sessionToken);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get session reports",
                    message = ex.Message
                });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetReportStats()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    statistics = new
                    {
                        total_reports = 0,
                        total_size_bytes = 0,
                        total_size_mb = 0.0,
                        reports_last_7_days = 0,
                        reports_by_date = new { },
                        average_report_size_kb = 0.0
                    },
                    generated_at = DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report stats");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get report statistics",
                    message = ex.Message
                });
            }
        }

        [HttpGet("assessments/{assessmentId}/elements")]
        public async Task<IActionResult> GetReportElements(int assessmentId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                var sampleElements = new object[]
                {
                    new
                    {
                        id = 1,
                        parent_element_id = (int?)null,
                        element_type = "Section",
                        title = language == "ar" ? "نتائج تقييم الشخصية" : "Personality Assessment Results",
                        content = language == "ar" ? "هذا القسم يحتوي على نتائج تقييم شخصيتك" : "This section contains your personality assessment results",
                        order_index = 1,
                        is_interactive = false,
                        child_elements = new object[]
                        {
                            new
                            {
                                id = 2,
                                parent_element_id = 1,
                                element_type = "TextBlock",
                                title = language == "ar" ? "نوع شخصيتك" : "Your Personality Type",
                                content = language == "ar" ? "نوع شخصيتك هو INTJ - المهندس المعماري" : "Your personality type is INTJ - The Architect",
                                order_index = 1,
                                is_interactive = false,
                                graph_data = (string?)null
                            },
                            new
                            {
                                id = 3,
                                parent_element_id = 1,
                                element_type = "GraphSection",
                                title = language == "ar" ? "نقاط القوة في الأبعاد" : "Dimension Strengths",
                                content = "",
                                order_index = 2,
                                is_interactive = false,
                                graph_data = (string?)"{\"type\":\"bar\",\"data\":{\"E\":0.7,\"S\":0.3,\"T\":0.8,\"J\":0.6}}"
                            }
                        },
                        activity_data = (string?)null
                    },
                    new
                    {
                        id = 4,
                        parent_element_id = (int?)null,
                        element_type = "Activity",
                        title = language == "ar" ? "تقييم النتائج" : "Rate Your Results",
                        content = language == "ar" ? "ما مدى دقة هذه النتائج في وصف شخصيتك؟" : "How accurately do these results describe your personality?",
                        order_index = 2,
                        is_interactive = true,
                        child_elements = new object[0],
                        activity_data = (string?)"{\"type\":\"rating\",\"scale\":5,\"required\":true}"
                    }
                };

                return Ok(new
                {
                    success = true,
                    assessment_id = assessmentId,
                    language = language,
                    report_elements = sampleElements,
                    total_elements = sampleElements.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report elements for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get report elements",
                    message = ex.Message
                });
            }
        }

        [HttpPost("elements/{elementId}/answers")]
        public async Task<IActionResult> SubmitReportAnswer(int elementId, [FromBody] ReportAnswerRequest request)
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

                if (request.AssessmentSessionId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "assessment_session_id is required"
                    });
                }

                return Ok(new
                {
                    success = true,
                    report_user_answer = new
                    {
                        id = new Random().Next(1000, 9999),
                        report_element_question_id = request.QuestionId,
                        assessment_session_id = request.AssessmentSessionId,
                        answer_text = request.AnswerText,
                        answer_rating = request.AnswerRating,
                        answer_boolean = request.AnswerBoolean,
                        answer_choice = request.AnswerChoice,
                        answered_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting report answer for element {ElementId}", elementId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to submit report answer",
                    message = ex.Message
                });
            }
        }

        [HttpPost("elements/{elementId}/ratings")]
        public async Task<IActionResult> RateReportElement(int elementId, [FromBody] ReportElementRatingRequest request)
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

                if (request.AssessmentSessionId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "assessment_session_id is required"
                    });
                }

                if (request.Rating < 1 || request.Rating > 5)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "rating must be between 1 and 5"
                    });
                }

                return Ok(new
                {
                    success = true,
                    report_element_rating = new
                    {
                        id = new Random().Next(1000, 9999),
                        report_element_id = elementId,
                        assessment_session_id = request.AssessmentSessionId,
                        rating = request.Rating,
                        comment = request.Comment ?? "",
                        rated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating report element {ElementId}", elementId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to rate report element",
                    message = ex.Message
                });
            }
        }

        [HttpGet("assessments/{assessmentId}/feedback")]
        public async Task<IActionResult> GetReportFeedback(int assessmentId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    assessment_id = assessmentId,
                    user_answers = new object[0],
                    element_ratings = new object[0],
                    total_answers = 0,
                    total_ratings = 0,
                    average_rating = 0.0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report feedback for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get report feedback",
                    message = ex.Message
                });
            }
        }
    }

    [ApiController]
    [Route("api/achieve_works_reports")]
    public class AchieveWorksReportsController : ControllerBase
    {
        private readonly ILogger<AchieveWorksReportsController> _logger;

        public AchieveWorksReportsController(ILogger<AchieveWorksReportsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetAchieveWorksReport(int reportId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar", "es", "zh" }.Contains(language))
                {
                    language = "en";
                }

                return Ok(new
                {
                    success = true,
                    report_id = reportId,
                    language = language,
                    report_type = "AchieveWorks",
                    personality_type = "INTJ",
                    student_name = "Student Name",
                    generated_at = DateTime.UtcNow.ToString("O"),
                    report_elements = new object[0],
                    career_matches = new object[0]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AchieveWorks report {ReportId}", reportId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get AchieveWorks report",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{reportId}/careers")]
        public async Task<IActionResult> GetReportCareers(int reportId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar", "es", "zh" }.Contains(language))
                {
                    language = "en";
                }

                return Ok(new
                {
                    success = true,
                    report_id = reportId,
                    language = language,
                    career_matches = new object[0],
                    total_matches = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting careers for report {ReportId}", reportId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get report careers",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{reportId}/career_program_matches")]
        public async Task<IActionResult> GetCareerProgramMatches(int reportId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar", "es", "zh" }.Contains(language))
                {
                    language = "en";
                }

                return Ok(new
                {
                    success = true,
                    report_id = reportId,
                    language = language,
                    program_matches = new object[0],
                    total_matches = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting program matches for report {ReportId}", reportId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career program matches",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{reportId}/report_element_ratings")]
        public async Task<IActionResult> GetReportElementRatings(int reportId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    report_id = reportId,
                    element_ratings = new object[0],
                    total_ratings = 0,
                    average_rating = 0.0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting element ratings for report {ReportId}", reportId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get report element ratings",
                    message = ex.Message
                });
            }
        }
    }

    [ApiController]
    [Route("api/report_user_answers")]
    public class ReportUserAnswersController : ControllerBase
    {
        private readonly ILogger<ReportUserAnswersController> _logger;

        public ReportUserAnswersController(ILogger<ReportUserAnswersController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReportUserAnswer([FromBody] CreateReportUserAnswerRequest request)
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

                return Ok(new
                {
                    success = true,
                    report_user_answer = new
                    {
                        id = new Random().Next(1000, 9999),
                        report_element_question_id = request.ReportElementQuestionId,
                        assessment_session_id = request.AssessmentSessionId,
                        answer_text = request.AnswerText,
                        answer_rating = request.AnswerRating,
                        answer_boolean = request.AnswerBoolean,
                        answer_choice = request.AnswerChoice,
                        answered_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report user answer");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create report user answer",
                    message = ex.Message
                });
            }
        }

        [HttpPut("{answerId}")]
        public async Task<IActionResult> UpdateReportUserAnswer(int answerId, [FromBody] UpdateReportUserAnswerRequest request)
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

                return Ok(new
                {
                    success = true,
                    report_user_answer = new
                    {
                        id = answerId,
                        answer_text = request.AnswerText,
                        answer_rating = request.AnswerRating,
                        answer_boolean = request.AnswerBoolean,
                        answer_choice = request.AnswerChoice,
                        updated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report user answer {AnswerId}", answerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update report user answer",
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{answerId}")]
        public async Task<IActionResult> DeleteReportUserAnswer(int answerId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    message = "Report user answer deleted successfully",
                    deleted_id = answerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report user answer {AnswerId}", answerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete report user answer",
                    message = ex.Message
                });
            }
        }
    }

    [ApiController]
    [Route("api/report_element_ratings")]
    public class ReportElementRatingsController : ControllerBase
    {
        private readonly ILogger<ReportElementRatingsController> _logger;

        public ReportElementRatingsController(ILogger<ReportElementRatingsController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReportElementRating([FromBody] CreateReportElementRatingRequest request)
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

                return Ok(new
                {
                    success = true,
                    report_element_rating = new
                    {
                        id = new Random().Next(1000, 9999),
                        report_element_id = request.ReportElementId,
                        assessment_session_id = request.AssessmentSessionId,
                        rating = request.Rating,
                        comment = request.Comment ?? "",
                        rated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report element rating");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create report element rating",
                    message = ex.Message
                });
            }
        }

        [HttpPut("{ratingId}")]
        public async Task<IActionResult> UpdateReportElementRating(int ratingId, [FromBody] UpdateReportElementRatingRequest request)
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

                return Ok(new
                {
                    success = true,
                    report_element_rating = new
                    {
                        id = ratingId,
                        rating = request.Rating,
                        comment = request.Comment ?? "",
                        updated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report element rating {RatingId}", ratingId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update report element rating",
                    message = ex.Message
                });
            }
        }
    }

    [ApiController]
    [Route("api/career_user_ratings")]
    public class CareerUserRatingsController : ControllerBase
    {
        private readonly ILogger<CareerUserRatingsController> _logger;

        public CareerUserRatingsController(ILogger<CareerUserRatingsController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCareerUserRating([FromBody] CreateCareerUserRatingRequest request)
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

                return Ok(new
                {
                    success = true,
                    career_user_rating = new
                    {
                        id = new Random().Next(1000, 9999),
                        career_id = request.CareerId,
                        user_id = request.UserId,
                        rating_id = request.RatingId,
                        rating_value = request.RatingValue,
                        created_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating career user rating");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create career user rating",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{ratingId}")]
        public async Task<IActionResult> GetCareerUserRating(int ratingId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    career_user_rating = new
                    {
                        id = ratingId,
                        career_id = 1,
                        user_id = 1,
                        rating_id = 1,
                        rating_value = 5,
                        created_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career user rating {RatingId}", ratingId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career user rating",
                    message = ex.Message
                });
            }
        }

        [HttpPut("{ratingId}")]
        public async Task<IActionResult> UpdateCareerUserRating(int ratingId, [FromBody] UpdateCareerUserRatingRequest request)
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

                return Ok(new
                {
                    success = true,
                    career_user_rating = new
                    {
                        id = ratingId,
                        rating_id = request.RatingId,
                        rating_value = request.RatingValue,
                        updated_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating career user rating {RatingId}", ratingId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update career user rating",
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{ratingId}")]
        public async Task<IActionResult> DeleteCareerUserRating(int ratingId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    message = "Career user rating deleted successfully",
                    deleted_id = ratingId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting career user rating {RatingId}", ratingId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete career user rating",
                    message = ex.Message
                });
            }
        }
    }

    public class GenerateReportRequest
    {
        [Required]
        public string SessionToken { get; set; } = string.Empty;

        public string? Language { get; set; } = "en";

        public string? ReportType { get; set; } = "comprehensive";

        public bool IncludeCareerDetails { get; set; } = true;
    }

    public class ReportAnswerRequest
    {
        [Required]
        public int AssessmentSessionId { get; set; }

        public int? QuestionId { get; set; }

        public string? AnswerText { get; set; }

        [Range(1, 5)]
        public int? AnswerRating { get; set; }

        public bool? AnswerBoolean { get; set; }

        public string? AnswerChoice { get; set; }
    }

    public class ReportElementRatingRequest
    {
        [Required]
        public int AssessmentSessionId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class CreateReportUserAnswerRequest
    {
        [Required]
        public int ReportElementQuestionId { get; set; }

        [Required]
        public int AssessmentSessionId { get; set; }

        public string? AnswerText { get; set; }

        [Range(1, 5)]
        public int? AnswerRating { get; set; }

        public bool? AnswerBoolean { get; set; }

        public string? AnswerChoice { get; set; }
    }

    public class UpdateReportUserAnswerRequest
    {
        public string? AnswerText { get; set; }

        [Range(1, 5)]
        public int? AnswerRating { get; set; }

        public bool? AnswerBoolean { get; set; }

        public string? AnswerChoice { get; set; }
    }

    public class CreateReportElementRatingRequest
    {
        [Required]
        public int ReportElementId { get; set; }

        [Required]
        public int AssessmentSessionId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class UpdateReportElementRatingRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class CreateCareerUserRatingRequest
    {
        [Required]
        public int CareerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int RatingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }
    }

    public class UpdateCareerUserRatingRequest
    {
        [Required]
        public int RatingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }
    }
}
