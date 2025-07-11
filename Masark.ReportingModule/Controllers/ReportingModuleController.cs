using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Masark.ReportingModule.Services;

namespace Masark.ReportingModule.Controllers
{
    [ApiController]
    [Route("api/reporting-module")]
    [Authorize]
    public class ReportingModuleController : ControllerBase
    {
        private readonly IReportingModuleService _reportingModuleService;
        private readonly ILogger<ReportingModuleController> _logger;

        public ReportingModuleController(
            IReportingModuleService reportingModuleService,
            ILogger<ReportingModuleController> logger)
        {
            _reportingModuleService = reportingModuleService;
            _logger = logger;
        }

        [HttpPost("reports/{sessionToken}")]
        [Authorize(Policy = "ViewReports")]
        public async Task<ActionResult<object>> GenerateReport(
            string sessionToken,
            [FromQuery] string language = "en")
        {
            try
            {
                var report = await _reportingModuleService.GenerateAssessmentReportAsync(sessionToken, language);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report for session {SessionToken}", sessionToken);
                return StatusCode(500, new { error = "Failed to generate report" });
            }
        }

        [HttpGet("reports/{sessionToken}/pdf")]
        [Authorize(Policy = "ViewReports")]
        public async Task<ActionResult> GeneratePdfReport(
            string sessionToken,
            [FromQuery] string language = "en")
        {
            try
            {
                var pdfBytes = await _reportingModuleService.GeneratePdfReportAsync(sessionToken, language);
                
                return File(pdfBytes, "application/pdf", $"assessment-report-{sessionToken}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report for session {SessionToken}", sessionToken);
                return StatusCode(500, new { error = "Failed to generate PDF report" });
            }
        }

        [HttpGet("reports")]
        [Authorize(Policy = "ViewReports")]
        public async Task<ActionResult<object>> GetReports(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
                var reports = await _reportingModuleService.GetReportsAsync(tenantId, page, pageSize);

                return Ok(new 
                { 
                    reports = reports,
                    page = page,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reports");
                return StatusCode(500, new { error = "Failed to retrieve reports" });
            }
        }

        [HttpGet("metrics")]
        [Authorize(Policy = "ViewReports")]
        public async Task<ActionResult<object>> GetReportMetrics()
        {
            try
            {
                var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
                var metrics = await _reportingModuleService.GetReportMetricsAsync(tenantId);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report metrics");
                return StatusCode(500, new { error = "Failed to retrieve report metrics" });
            }
        }

        [HttpDelete("reports/{reportId}")]
        [Authorize(Policy = "ManageReports")]
        public async Task<ActionResult<object>> DeleteReport(Guid reportId)
        {
            try
            {
                var success = await _reportingModuleService.DeleteReportAsync(reportId);
                
                if (!success)
                {
                    return BadRequest(new { error = "Failed to delete report" });
                }

                return Ok(new { success = true, message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
                return StatusCode(500, new { error = "Failed to delete report" });
            }
        }

        [HttpGet("templates/{templateName}")]
        [Authorize(Policy = "ViewReports")]
        public async Task<ActionResult<object>> GetReportTemplate(
            string templateName,
            [FromQuery] string language = "en")
        {
            try
            {
                var template = await _reportingModuleService.GetReportTemplateAsync(templateName, language);
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report template {TemplateName}", templateName);
                return StatusCode(500, new { error = "Failed to retrieve report template" });
            }
        }
    }
}
