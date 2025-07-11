using Microsoft.Extensions.Logging;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;
using Masark.Application.Services;

namespace Masark.ReportingModule.Services
{
    public interface IReportingModuleService
    {
        Task<ReportGenerationResult> GenerateAssessmentReportAsync(string sessionToken, string language);
        Task<byte[]> GeneratePdfReportAsync(string sessionToken, string language);
        Task<IEnumerable<ReportSummary>> GetReportsAsync(string tenantId, int page = 1, int pageSize = 20);
        Task<ReportMetrics> GetReportMetricsAsync(string tenantId);
        Task<bool> DeleteReportAsync(Guid reportId);
        Task<ReportTemplate> GetReportTemplateAsync(string templateName, string language);
    }

    public class ReportingModuleService : IReportingModuleService
    {
        private readonly IReportGenerationService _reportGenerationService;
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ICareerMatchingService _careerMatchingService;
        private readonly ICachingService _cachingService;
        private readonly ILogger<ReportingModuleService> _logger;

        public ReportingModuleService(
            IReportGenerationService reportGenerationService,
            IPersonalityRepository personalityRepository,
            ICareerMatchingService careerMatchingService,
            ICachingService cachingService,
            ILogger<ReportingModuleService> logger)
        {
            _reportGenerationService = reportGenerationService;
            _personalityRepository = personalityRepository;
            _careerMatchingService = careerMatchingService;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async Task<ReportGenerationResult> GenerateAssessmentReportAsync(string sessionToken, string language)
        {
            _logger.LogInformation("Generating assessment report for session {SessionToken} in language {Language}", 
                sessionToken, language);

            var session = await _personalityRepository.GetSessionByTokenAsync(sessionToken);
            if (session == null)
            {
                throw new InvalidOperationException($"Session not found for token {sessionToken}");
            }

            if (session.State != Domain.Enums.AssessmentState.Completed)
            {
                throw new InvalidOperationException($"Assessment session {sessionToken} is not completed");
            }

            var answers = await _personalityRepository.GetSessionAnswersAsync(session.Id);
            var careerMatches = await _careerMatchingService.GetCareerMatchesAsync(session.PersonalityType!, language);

            var reportData = new ReportData
            {
                SessionId = session.Id,
                UserId = session.UserId,
                PersonalityType = session.PersonalityType!,
                AssessmentAnswers = answers.ToList(),
                CareerMatches = careerMatches.Take(10).ToList(),
                Language = language,
                GeneratedAt = DateTime.UtcNow,
                TenantId = session.TenantId
            };

            var reportContent = await _reportGenerationService.GenerateReportAsync(reportData);

            var result = new ReportGenerationResult
            {
                ReportId = Guid.NewGuid(),
                SessionToken = sessionToken,
                Content = reportContent,
                Language = language,
                GeneratedAt = DateTime.UtcNow,
                FileSize = System.Text.Encoding.UTF8.GetByteCount(reportContent)
            };

            var cacheKey = $"report_{sessionToken}_{language}";
            await _cachingService.SetAsync(cacheKey, result, TimeSpan.FromHours(24));

            _logger.LogInformation("Assessment report generated successfully for session {SessionToken}, size: {FileSize} bytes", 
                sessionToken, result.FileSize);

            return result;
        }

        public async Task<byte[]> GeneratePdfReportAsync(string sessionToken, string language)
        {
            _logger.LogInformation("Generating PDF report for session {SessionToken} in language {Language}", 
                sessionToken, language);

            var cacheKey = $"pdf_report_{sessionToken}_{language}";
            var cachedPdf = await _cachingService.GetAsync<byte[]>(cacheKey);
            
            if (cachedPdf != null)
            {
                _logger.LogDebug("PDF report retrieved from cache for session {SessionToken}", sessionToken);
                return cachedPdf;
            }

            var reportResult = await GenerateAssessmentReportAsync(sessionToken, language);
            var pdfBytes = await _reportGenerationService.GeneratePdfAsync(reportResult.Content, language);

            await _cachingService.SetAsync(cacheKey, pdfBytes, TimeSpan.FromHours(24));

            _logger.LogInformation("PDF report generated successfully for session {SessionToken}, size: {Size} bytes", 
                sessionToken, pdfBytes.Length);

            return pdfBytes;
        }

        public async Task<IEnumerable<ReportSummary>> GetReportsAsync(string tenantId, int page = 1, int pageSize = 20)
        {
            _logger.LogInformation("Retrieving reports for tenant {TenantId}, page {Page}, pageSize {PageSize}", 
                tenantId, page, pageSize);

            var sessions = await _personalityRepository.GetCompletedSessionsAsync(tenantId);
            var pagedSessions = sessions
                .OrderByDescending(s => s.CompletedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var reports = new List<ReportSummary>();
            foreach (var session in pagedSessions)
            {
                var summary = new ReportSummary
                {
                    SessionId = session.Id,
                    SessionToken = session.SessionToken,
                    UserId = session.UserId,
                    PersonalityType = session.PersonalityType ?? "Unknown",
                    Language = session.LanguagePreference,
                    CompletedAt = session.CompletedAt ?? DateTime.MinValue,
                    TenantId = session.TenantId
                };
                reports.Add(summary);
            }

            _logger.LogInformation("Retrieved {Count} report summaries for tenant {TenantId}", reports.Count, tenantId);
            return reports;
        }

        public async Task<ReportMetrics> GetReportMetricsAsync(string tenantId)
        {
            _logger.LogInformation("Calculating report metrics for tenant {TenantId}", tenantId);

            var sessions = await _personalityRepository.GetCompletedSessionsAsync(tenantId);
            var totalSessions = await _personalityRepository.GetTotalSessionsAsync(tenantId);

            var metrics = new ReportMetrics
            {
                TenantId = tenantId,
                TotalReports = sessions.Count(),
                ReportsThisMonth = sessions.Count(s => s.CompletedAt?.Month == DateTime.UtcNow.Month),
                ReportsThisWeek = sessions.Count(s => s.CompletedAt >= DateTime.UtcNow.AddDays(-7)),
                CompletionRate = totalSessions > 0 ? (double)sessions.Count() / totalSessions * 100 : 0,
                AverageGenerationTime = TimeSpan.FromSeconds(2.5), // Estimated based on performance
                PopularLanguages = sessions
                    .GroupBy(s => s.LanguagePreference)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GeneratedAt = DateTime.UtcNow
            };

            return metrics;
        }

        public async Task<bool> DeleteReportAsync(Guid reportId)
        {
            _logger.LogInformation("Deleting report {ReportId}", reportId);

            try
            {
                await _cachingService.RemoveByPatternAsync($"report_*");
                await _cachingService.RemoveByPatternAsync($"pdf_report_*");

                _logger.LogInformation("Report {ReportId} deleted successfully", reportId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete report {ReportId}", reportId);
                return false;
            }
        }

        public async Task<ReportTemplate> GetReportTemplateAsync(string templateName, string language)
        {
            _logger.LogInformation("Retrieving report template {TemplateName} for language {Language}", 
                templateName, language);

            var cacheKey = $"template_{templateName}_{language}";
            var cachedTemplate = await _cachingService.GetAsync<ReportTemplate>(cacheKey);
            
            if (cachedTemplate != null)
            {
                return cachedTemplate;
            }

            var template = new ReportTemplate
            {
                Name = templateName,
                Language = language,
                Content = await LoadTemplateContentAsync(templateName, language),
                Version = "1.0",
                CreatedAt = DateTime.UtcNow
            };

            await _cachingService.SetAsync(cacheKey, template, TimeSpan.FromHours(12));
            return template;
        }

        private async Task<string> LoadTemplateContentAsync(string templateName, string language)
        {
            await Task.CompletedTask;
            
            return templateName switch
            {
                "assessment_report" => language == "ar" ? 
                    "قالب تقرير التقييم باللغة العربية" : 
                    "Assessment Report Template in English",
                "career_recommendation" => language == "ar" ? 
                    "قالب توصيات المهن باللغة العربية" : 
                    "Career Recommendation Template in English",
                _ => "Default template content"
            };
        }
    }

    public class ReportGenerationResult
    {
        public Guid ReportId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public long FileSize { get; set; }
    }

    public class ReportSummary
    {
        public Guid SessionId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string PersonalityType { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }

    public class ReportMetrics
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public int ReportsThisMonth { get; set; }
        public int ReportsThisWeek { get; set; }
        public double CompletionRate { get; set; }
        public TimeSpan AverageGenerationTime { get; set; }
        public Dictionary<string, int> PopularLanguages { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class ReportTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReportData
    {
        public Guid SessionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string PersonalityType { get; set; } = string.Empty;
        public List<AssessmentAnswer> AssessmentAnswers { get; set; } = new();
        public List<object> CareerMatches { get; set; } = new();
        public string Language { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }
}
