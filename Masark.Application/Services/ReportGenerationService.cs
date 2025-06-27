using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Borders;
using Masark.Domain.Entities;
using Masark.Application.Interfaces;

namespace Masark.Application.Services
{
    public class ReportOptions
    {
        public string OutputDirectory { get; set; } = "Reports";
        public string TemplateDirectory { get; set; } = "Templates";
        public bool EnableWatermark { get; set; } = true;
        public string WatermarkText { get; set; } = "Masark Engine";
        public string DefaultLanguage { get; set; } = "en";
        public Dictionary<string, string> BrandingColors { get; set; } = new()
        {
            ["primary"] = "#1E3A8A",
            ["secondary"] = "#10B981",
            ["accent"] = "#F59E0B",
            ["text"] = "#1F2937",
            ["background"] = "#F9FAFB"
        };
    }

    public class ReportData
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime AssessmentDate { get; set; }
        public string PersonalityType { get; set; } = string.Empty;
        public string PersonalityDescription { get; set; } = string.Empty;
        public Dictionary<string, double> DimensionScores { get; set; } = new();
        public List<ReportCareerMatch> CareerMatches { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Language { get; set; } = "en";
    }

    public class ReportCareerMatch
    {
        public int CareerId { get; set; }
        public string CareerName { get; set; } = string.Empty;
        public string CareerNameAr { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string ClusterName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Programs { get; set; } = new();
        public List<string> Pathways { get; set; } = new();
    }

    public class ReportTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SupportedLanguages { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public interface IReportGenerationService
    {
        Task<byte[]> GeneratePersonalityReportAsync(ReportData reportData, string templateName = "standard");
        Task<byte[]> GenerateCareerReportAsync(ReportData reportData, string templateName = "career_focused");
        Task<byte[]> GenerateDetailedReportAsync(ReportData reportData, string templateName = "comprehensive");
        Task<byte[]> GenerateSummaryReportAsync(ReportData reportData, string templateName = "summary");

        Task<List<ReportTemplate>> GetAvailableTemplatesAsync();
        Task<ReportTemplate?> GetTemplateAsync(string templateName);
        Task<bool> ValidateTemplateAsync(string templateName, string language);

        Task<string> SaveReportAsync(byte[] reportData, string fileName, string sessionId);
        Task<byte[]?> GetSavedReportAsync(string fileName);
        Task<bool> DeleteReportAsync(string fileName);
        Task<List<string>> GetReportHistoryAsync(string sessionId);

        Task<byte[]> GenerateCustomReportAsync(ReportData reportData, Dictionary<string, object> customOptions);
        Task<Dictionary<string, object>> GetReportMetadataAsync(string sessionId);

        Task<List<byte[]>> GenerateBulkReportsAsync(List<ReportData> reportDataList, string templateName);
        Task<byte[]> GenerateComparisonReportAsync(List<ReportData> reportDataList);

        Task<string> ExportToJsonAsync(ReportData reportData);
        Task<string> ExportToCsvAsync(List<ReportData> reportDataList);
        Task<byte[]> ExportToExcelAsync(List<ReportData> reportDataList);
    }

    public class ReportGenerationService : IReportGenerationService
    {
        private readonly ILogger<ReportGenerationService> _logger;
        private readonly ReportOptions _options;
        private readonly ILocalizationService _localizationService;
        private readonly Dictionary<string, ReportTemplate> _templates;

        public ReportGenerationService(
            ILogger<ReportGenerationService> logger,
            IOptions<ReportOptions> options,
            ILocalizationService localizationService)
        {
            _logger = logger;
            _options = options.Value;
            _localizationService = localizationService;
            _templates = new Dictionary<string, ReportTemplate>();
            
            InitializeTemplates();
            EnsureDirectoriesExist();
        }

        private void InitializeTemplates()
        {
            _templates["standard"] = new ReportTemplate
            {
                Name = "standard",
                DisplayName = "Standard Personality Report",
                Description = "Comprehensive personality assessment report with career recommendations",
                SupportedLanguages = new List<string> { "en", "ar" },
                Configuration = new Dictionary<string, object>
                {
                    ["include_career_matches"] = true,
                    ["include_dimension_details"] = true,
                    ["include_recommendations"] = true,
                    ["max_career_matches"] = 10
                }
            };

            _templates["career_focused"] = new ReportTemplate
            {
                Name = "career_focused",
                DisplayName = "Career-Focused Report",
                Description = "Report emphasizing career matches and pathways",
                SupportedLanguages = new List<string> { "en", "ar" },
                Configuration = new Dictionary<string, object>
                {
                    ["include_career_matches"] = true,
                    ["include_programs"] = true,
                    ["include_pathways"] = true,
                    ["max_career_matches"] = 15,
                    ["detailed_career_info"] = true
                }
            };

            _templates["comprehensive"] = new ReportTemplate
            {
                Name = "comprehensive",
                DisplayName = "Comprehensive Report",
                Description = "Detailed report with all available information",
                SupportedLanguages = new List<string> { "en", "ar" },
                Configuration = new Dictionary<string, object>
                {
                    ["include_everything"] = true,
                    ["include_statistical_analysis"] = true,
                    ["include_comparison_data"] = true,
                    ["max_career_matches"] = 20
                }
            };

            _templates["summary"] = new ReportTemplate
            {
                Name = "summary",
                DisplayName = "Summary Report",
                Description = "Concise summary of personality type and top career matches",
                SupportedLanguages = new List<string> { "en", "ar" },
                Configuration = new Dictionary<string, object>
                {
                    ["include_career_matches"] = true,
                    ["max_career_matches"] = 5,
                    ["concise_format"] = true
                }
            };
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_options.OutputDirectory);
            Directory.CreateDirectory(_options.TemplateDirectory);
        }

        public async Task<byte[]> GeneratePersonalityReportAsync(ReportData reportData, string templateName = "standard")
        {
            try
            {
                _logger.LogInformation("Generating personality report for session {SessionId} using template {Template}", 
                    reportData.SessionId, templateName);

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                await AddReportHeaderAsync(document, reportData, titleFont);

                await AddPersonalityTypeSectionAsync(document, reportData, headerFont, bodyFont);

                await AddDimensionScoresSectionAsync(document, reportData, headerFont, bodyFont);

                if (reportData.CareerMatches.Any())
                {
                    await AddCareerMatchesSectionAsync(document, reportData, headerFont, bodyFont, templateName);
                }

                await AddRecommendationsSectionAsync(document, reportData, headerFont, bodyFont);

                await AddReportFooterAsync(document, reportData, bodyFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personality report for session {SessionId}", reportData.SessionId);
                throw;
            }
        }

        public async Task<byte[]> GenerateCareerReportAsync(ReportData reportData, string templateName = "career_focused")
        {
            try
            {
                _logger.LogInformation("Generating career report for session {SessionId}", reportData.SessionId);

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                await AddReportHeaderAsync(document, reportData, titleFont);

                await AddPersonalityOverviewAsync(document, reportData, headerFont, bodyFont);

                await AddDetailedCareerMatchesSectionAsync(document, reportData, headerFont, bodyFont);

                await AddProgramsAndPathwaysAsync(document, reportData, headerFont, bodyFont);

                await AddCareerDevelopmentRecommendationsAsync(document, reportData, headerFont, bodyFont);

                await AddReportFooterAsync(document, reportData, bodyFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating career report for session {SessionId}", reportData.SessionId);
                throw;
            }
        }

        public async Task<byte[]> GenerateDetailedReportAsync(ReportData reportData, string templateName = "comprehensive")
        {
            try
            {
                _logger.LogInformation("Generating detailed report for session {SessionId}", reportData.SessionId);

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                await AddReportHeaderAsync(document, reportData, titleFont);
                await AddExecutiveSummaryAsync(document, reportData, headerFont, bodyFont);
                await AddPersonalityTypeSectionAsync(document, reportData, headerFont, bodyFont);
                await AddDetailedDimensionAnalysisAsync(document, reportData, headerFont, bodyFont);
                await AddDetailedCareerMatchesSectionAsync(document, reportData, headerFont, bodyFont);
                await AddProgramsAndPathwaysAsync(document, reportData, headerFont, bodyFont);
                await AddStatisticalAnalysisAsync(document, reportData, headerFont, bodyFont);
                await AddRecommendationsSectionAsync(document, reportData, headerFont, bodyFont);
                await AddReportFooterAsync(document, reportData, bodyFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating detailed report for session {SessionId}", reportData.SessionId);
                throw;
            }
        }

        public async Task<byte[]> GenerateSummaryReportAsync(ReportData reportData, string templateName = "summary")
        {
            try
            {
                _logger.LogInformation("Generating summary report for session {SessionId}", reportData.SessionId);

                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                await AddReportHeaderAsync(document, reportData, titleFont);
                await AddPersonalityOverviewAsync(document, reportData, headerFont, bodyFont);
                await AddTopCareerMatchesAsync(document, reportData, headerFont, bodyFont, 5);
                await AddKeyRecommendationsAsync(document, reportData, headerFont, bodyFont);
                await AddReportFooterAsync(document, reportData, bodyFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary report for session {SessionId}", reportData.SessionId);
                throw;
            }
        }

        public async Task<List<ReportTemplate>> GetAvailableTemplatesAsync()
        {
            return await Task.FromResult(_templates.Values.ToList());
        }

        public async Task<ReportTemplate?> GetTemplateAsync(string templateName)
        {
            _templates.TryGetValue(templateName, out var template);
            return await Task.FromResult(template);
        }

        public async Task<bool> ValidateTemplateAsync(string templateName, string language)
        {
            if (!_templates.ContainsKey(templateName))
                return false;

            var template = _templates[templateName];
            return await Task.FromResult(template.SupportedLanguages.Contains(language));
        }

        public async Task<string> SaveReportAsync(byte[] reportData, string fileName, string sessionId)
        {
            try
            {
                var sessionDirectory = Path.Combine(_options.OutputDirectory, sessionId);
                Directory.CreateDirectory(sessionDirectory);

                var filePath = Path.Combine(sessionDirectory, fileName);
                await File.WriteAllBytesAsync(filePath, reportData);

                _logger.LogInformation("Saved report {FileName} for session {SessionId}", fileName, sessionId);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving report {FileName} for session {SessionId}", fileName, sessionId);
                throw;
            }
        }

        public async Task<byte[]?> GetSavedReportAsync(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    return await File.ReadAllBytesAsync(fileName);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving saved report {FileName}", fileName);
                return null;
            }
        }

        public async Task<bool> DeleteReportAsync(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    _logger.LogInformation("Deleted report {FileName}", fileName);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {FileName}", fileName);
                return false;
            }
        }

        public async Task<List<string>> GetReportHistoryAsync(string sessionId)
        {
            try
            {
                var sessionDirectory = Path.Combine(_options.OutputDirectory, sessionId);
                if (Directory.Exists(sessionDirectory))
                {
                    var files = Directory.GetFiles(sessionDirectory, "*.pdf")
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>()
                        .ToList();
                    return await Task.FromResult(files);
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report history for session {SessionId}", sessionId);
                return new List<string>();
            }
        }

        public async Task<byte[]> GenerateCustomReportAsync(ReportData reportData, Dictionary<string, object> customOptions)
        {
            var templateName = customOptions.GetValueOrDefault("template", "standard").ToString() ?? "standard";
            return await GeneratePersonalityReportAsync(reportData, templateName);
        }

        public async Task<Dictionary<string, object>> GetReportMetadataAsync(string sessionId)
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                ["session_id"] = sessionId,
                ["generated_at"] = DateTime.UtcNow,
                ["available_templates"] = _templates.Keys.ToList(),
                ["supported_languages"] = new List<string> { "en", "ar" },
                ["output_formats"] = new List<string> { "pdf", "json", "csv", "excel" }
            });
        }

        public async Task<List<byte[]>> GenerateBulkReportsAsync(List<ReportData> reportDataList, string templateName)
        {
            var reports = new List<byte[]>();
            
            foreach (var reportData in reportDataList)
            {
                try
                {
                    var report = await GeneratePersonalityReportAsync(reportData, templateName);
                    reports.Add(report);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating bulk report for session {SessionId}", reportData.SessionId);
                }
            }

            return reports;
        }

        public async Task<byte[]> GenerateComparisonReportAsync(List<ReportData> reportDataList)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            document.Add(new Paragraph("Personality Assessment Comparison Report")
                .SetFont(titleFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            await AddComparisonTableAsync(document, reportDataList, headerFont, bodyFont);

            document.Close();
            return memoryStream.ToArray();
        }

        public async Task<string> ExportToJsonAsync(ReportData reportData)
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                return await Task.FromResult(JsonSerializer.Serialize(reportData, jsonOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to JSON for session {SessionId}", reportData.SessionId);
                throw;
            }
        }

        public async Task<string> ExportToCsvAsync(List<ReportData> reportDataList)
        {
            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("SessionId,UserId,UserName,AssessmentDate,PersonalityType,TopCareerMatch,MatchScore");

                foreach (var data in reportDataList)
                {
                    var topCareer = data.CareerMatches.FirstOrDefault();
                    csv.AppendLine($"{data.SessionId},{data.UserId},{data.UserName},{data.AssessmentDate:yyyy-MM-dd}," +
                                 $"{data.PersonalityType},{topCareer?.CareerName ?? "N/A"},{topCareer?.MatchScore ?? 0}");
                }

                return await Task.FromResult(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(List<ReportData> reportDataList)
        {
            var csvData = await ExportToCsvAsync(reportDataList);
            return Encoding.UTF8.GetBytes(csvData);
        }

        private async Task AddReportHeaderAsync(Document document, ReportData reportData, PdfFont titleFont)
        {
            var title = await _localizationService.GetTextAsync("reports.personality_report", reportData.Language);
            
            document.Add(new Paragraph(title)
                .SetFont(titleFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10));

            document.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}")
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetMarginBottom(20));
        }

        private async Task AddPersonalityTypeSectionAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            var sectionTitle = await _localizationService.GetTextAsync("reports.personality_type", reportData.Language);
            
            document.Add(new Paragraph(sectionTitle)
                .SetFont(headerFont)
                .SetFontSize(16)
                .SetMarginBottom(10));

            document.Add(new Paragraph($"Your personality type: {reportData.PersonalityType}")
                .SetFont(bodyFont)
                .SetFontSize(12)
                .SetMarginBottom(5));

            document.Add(new Paragraph(reportData.PersonalityDescription)
                .SetFont(bodyFont)
                .SetFontSize(11)
                .SetMarginBottom(15));
        }

        private async Task AddDimensionScoresSectionAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Dimension Scores")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            foreach (var dimension in reportData.DimensionScores)
            {
                document.Add(new Paragraph($"{dimension.Key}: {dimension.Value:F1}%")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(3));
            }

            document.Add(new Paragraph("").SetMarginBottom(15));
        }

        private async Task AddCareerMatchesSectionAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont, string templateName)
        {
            var template = await GetTemplateAsync(templateName);
            var maxMatches = template?.Configuration.GetValueOrDefault("max_career_matches", 10) as int? ?? 10;

            var sectionTitle = await _localizationService.GetTextAsync("reports.career_matches", reportData.Language);
            
            document.Add(new Paragraph(sectionTitle)
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var topMatches = reportData.CareerMatches.Take(maxMatches);
            foreach (var match in topMatches)
            {
                var careerName = reportData.Language == "ar" && !string.IsNullOrEmpty(match.CareerNameAr) 
                    ? match.CareerNameAr 
                    : match.CareerName;

                document.Add(new Paragraph($"{careerName} - {match.MatchScore:F1}% match")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(3));
            }

            document.Add(new Paragraph("").SetMarginBottom(15));
        }

        private async Task AddRecommendationsSectionAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            var sectionTitle = await _localizationService.GetTextAsync("reports.recommendations", reportData.Language);
            
            document.Add(new Paragraph(sectionTitle)
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var recommendations = GenerateRecommendations(reportData);
            foreach (var recommendation in recommendations)
            {
                document.Add(new Paragraph($"• {recommendation}")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(5));
            }
        }

        private async Task AddReportFooterAsync(Document document, ReportData reportData, PdfFont bodyFont)
        {
            document.Add(new Paragraph($"Report generated by Masark Engine on {DateTime.Now:yyyy-MM-dd}")
                .SetFont(bodyFont)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30));
        }

        private async Task AddPersonalityOverviewAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Personality Overview")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            document.Add(new Paragraph($"Type: {reportData.PersonalityType}")
                .SetFont(bodyFont)
                .SetFontSize(12)
                .SetMarginBottom(10));
        }

        private async Task AddDetailedCareerMatchesSectionAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Detailed Career Matches")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            foreach (var match in reportData.CareerMatches.Take(10))
            {
                document.Add(new Paragraph($"{match.CareerName} ({match.MatchScore:F1}%)")
                    .SetFont(headerFont)
                    .SetFontSize(12)
                    .SetMarginBottom(5));

                if (!string.IsNullOrEmpty(match.Description))
                {
                    document.Add(new Paragraph(match.Description)
                        .SetFont(bodyFont)
                        .SetFontSize(10)
                        .SetMarginLeft(20)
                        .SetMarginBottom(10));
                }
            }
        }

        private async Task AddProgramsAndPathwaysAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Educational Programs and Career Pathways")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            foreach (var match in reportData.CareerMatches.Take(5))
            {
                if (match.Programs.Any() || match.Pathways.Any())
                {
                    document.Add(new Paragraph($"{match.CareerName}:")
                        .SetFont(headerFont)
                        .SetFontSize(11)
                        .SetMarginBottom(5));

                    if (match.Programs.Any())
                    {
                        document.Add(new Paragraph($"Programs: {string.Join(", ", match.Programs)}")
                            .SetFont(bodyFont)
                            .SetFontSize(10)
                            .SetMarginLeft(20)
                            .SetMarginBottom(3));
                    }

                    if (match.Pathways.Any())
                    {
                        document.Add(new Paragraph($"Pathways: {string.Join(", ", match.Pathways)}")
                            .SetFont(bodyFont)
                            .SetFontSize(10)
                            .SetMarginLeft(20)
                            .SetMarginBottom(10));
                    }
                }
            }
        }

        private async Task AddExecutiveSummaryAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Executive Summary")
                .SetFont(headerFont)
                .SetFontSize(16)
                .SetMarginBottom(10));

            var summary = $"This comprehensive personality assessment report for {reportData.UserName} " +
                         $"reveals a {reportData.PersonalityType} personality type with {reportData.CareerMatches.Count} " +
                         $"career matches identified. The assessment was completed on {reportData.AssessmentDate:yyyy-MM-dd}.";

            document.Add(new Paragraph(summary)
                .SetFont(bodyFont)
                .SetFontSize(11)
                .SetMarginBottom(15));
        }

        private async Task AddDetailedDimensionAnalysisAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Detailed Dimension Analysis")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            foreach (var dimension in reportData.DimensionScores)
            {
                document.Add(new Paragraph($"{dimension.Key}: {dimension.Value:F1}%")
                    .SetFont(headerFont)
                    .SetFontSize(12)
                    .SetMarginBottom(5));

                var analysis = GetDimensionAnalysis(dimension.Key, dimension.Value);
                document.Add(new Paragraph(analysis)
                    .SetFont(bodyFont)
                    .SetFontSize(10)
                    .SetMarginLeft(20)
                    .SetMarginBottom(10));
            }
        }

        private async Task AddStatisticalAnalysisAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Statistical Analysis")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var avgScore = reportData.DimensionScores.Values.Average();
            document.Add(new Paragraph($"Average dimension score: {avgScore:F1}%")
                .SetFont(bodyFont)
                .SetFontSize(11)
                .SetMarginBottom(5));

            var topCareerMatch = reportData.CareerMatches.FirstOrDefault();
            if (topCareerMatch != null)
            {
                document.Add(new Paragraph($"Highest career match: {topCareerMatch.MatchScore:F1}%")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginBottom(15));
            }
        }

        private async Task AddTopCareerMatchesAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont, int count)
        {
            document.Add(new Paragraph($"Top {count} Career Matches")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            foreach (var match in reportData.CareerMatches.Take(count))
            {
                document.Add(new Paragraph($"{match.CareerName} - {match.MatchScore:F1}%")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(3));
            }

            document.Add(new Paragraph("").SetMarginBottom(15));
        }

        private async Task AddKeyRecommendationsAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Key Recommendations")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var keyRecommendations = GenerateRecommendations(reportData).Take(3);
            foreach (var recommendation in keyRecommendations)
            {
                document.Add(new Paragraph($"• {recommendation}")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(5));
            }
        }

        private async Task AddCareerDevelopmentRecommendationsAsync(Document document, ReportData reportData, PdfFont headerFont, PdfFont bodyFont)
        {
            document.Add(new Paragraph("Career Development Recommendations")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var recommendations = new List<string>
            {
                "Focus on developing skills relevant to your top career matches",
                "Consider pursuing educational programs aligned with your interests",
                "Network with professionals in your target career fields",
                "Gain practical experience through internships or volunteer work"
            };

            foreach (var recommendation in recommendations)
            {
                document.Add(new Paragraph($"• {recommendation}")
                    .SetFont(bodyFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20)
                    .SetMarginBottom(5));
            }
        }

        private async Task AddComparisonTableAsync(Document document, List<ReportData> reportDataList, PdfFont headerFont, PdfFont bodyFont)
        {
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(new Cell().Add(new Paragraph("Name").SetFont(headerFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Personality Type").SetFont(headerFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Top Career").SetFont(headerFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Match Score").SetFont(headerFont)));

            foreach (var data in reportDataList)
            {
                var topCareer = data.CareerMatches.FirstOrDefault();
                table.AddCell(new Cell().Add(new Paragraph(data.UserName).SetFont(bodyFont)));
                table.AddCell(new Cell().Add(new Paragraph(data.PersonalityType).SetFont(bodyFont)));
                table.AddCell(new Cell().Add(new Paragraph(topCareer?.CareerName ?? "N/A").SetFont(bodyFont)));
                table.AddCell(new Cell().Add(new Paragraph($"{topCareer?.MatchScore:F1}%").SetFont(bodyFont)));
            }

            document.Add(table);
        }

        private List<string> GenerateRecommendations(ReportData reportData)
        {
            var recommendations = new List<string>();

            recommendations.Add($"As a {reportData.PersonalityType}, focus on careers that align with your natural strengths.");
            
            if (reportData.CareerMatches.Any())
            {
                var topCareer = reportData.CareerMatches.First();
                recommendations.Add($"Consider pursuing {topCareer.CareerName} as it shows a {topCareer.MatchScore:F1}% match with your personality.");
            }

            recommendations.Add("Develop both technical and soft skills relevant to your target career field.");
            recommendations.Add("Seek mentorship from professionals in your areas of interest.");
            recommendations.Add("Consider additional assessments to further refine your career path.");

            return recommendations;
        }

        private string GetDimensionAnalysis(string dimension, double score)
        {
            return dimension switch
            {
                "E_I" => score > 50 ? "You tend to be more extraverted, gaining energy from social interactions." 
                                   : "You tend to be more introverted, preferring quiet reflection and smaller groups.",
                "S_N" => score > 50 ? "You prefer concrete information and practical applications." 
                                   : "You prefer abstract concepts and future possibilities.",
                "T_F" => score > 50 ? "You tend to make decisions based on logical analysis." 
                                   : "You tend to make decisions based on personal values and impact on others.",
                "J_P" => score > 50 ? "You prefer structure and planned approaches." 
                                   : "You prefer flexibility and spontaneous approaches.",
                _ => "This dimension reflects your natural preferences and tendencies."
            };
        }
    }
}
