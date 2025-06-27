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
    }

    public class GenerateReportRequest
    {
        [Required]
        public string SessionToken { get; set; } = string.Empty;

        public string? Language { get; set; } = "en";

        public string? ReportType { get; set; } = "comprehensive";

        public bool IncludeCareerDetails { get; set; } = true;
    }
}
