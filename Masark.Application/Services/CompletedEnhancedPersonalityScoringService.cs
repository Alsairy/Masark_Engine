using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Masark.Application.Services
{
    /// <summary>
    /// Extension of the EnhancedPersonalityScoringService that integrates with the
    /// persistence layer.  This wrapper class decorates the existing enhanced
    /// scoring logic by retrieving assessment sessions and their answers via
    /// IPersonalityRepository, computing the enhanced result and updating the
    /// session entity with the outcome (personality type, strengths and
    /// clarification categories).  It also exposes an API to generate a
    /// quality assessment report that includes statistical metrics.
    /// </summary>
    public class CompletedEnhancedPersonalityScoringService : IEnhancedPersonalityScoringService
    {
        private readonly IPersonalityRepository _repository;
        private readonly EnhancedPersonalityScoringService _inner;
        private readonly ILogger<CompletedEnhancedPersonalityScoringService> _logger;

        public CompletedEnhancedPersonalityScoringService(
            IPersonalityRepository repository,
            ILogger<CompletedEnhancedPersonalityScoringService> logger)
        {
            _repository = repository;
            _logger = logger;
            _inner = new EnhancedPersonalityScoringService(logger);
        }

        /// <summary>
        /// Calculate the enhanced personality type for a session, update the session
        /// with the results and persist it.  The method fetches the session,
        /// answers and questions via the repository, delegates the heavy lifting
        /// to the existing EnhancedPersonalityScoringService and then updates
        /// the session using domain methods.
        /// </summary>
        public async Task CalculateEnhancedPersonalityTypeAsync(int sessionId)
        {
            try
            {
                var session = await _repository.GetSessionByIdAsync(sessionId);
                if (session == null)
                    throw new ArgumentException($"Session {sessionId} not found");
                if (session.IsCompleted)
                {
                    _logger.LogInformation("Session {SessionId} is already completed", sessionId);
                    return;
                }
                var answers = await _repository.GetAnswersBySessionIdAsync(sessionId);
                var questions = await _repository.GetActiveQuestionsAsync();
                var result = await _inner.CalculatePersonalityTypeAsync(session, answers, questions);
                // Determine type ID
                var personality = await _repository.GetPersonalityTypeByCodeAsync(result.TypeCode);
                if (personality == null)
                    throw new InvalidOperationException($"Personality type {result.TypeCode} not found");
                // Convert dimension analyses to strength ratios for dominant side
                var strengths = new Dictionary<string, double>();
                foreach (var kvp in result.DimensionAnalyses)
                {
                    var analysis = kvp.Value;
                    // analysis.Percentage is the ratio of dominant side (0–1)
                    switch (kvp.Key)
                    {
                        case "EI": strengths["E"] = analysis.PreferenceLetter == 'E'
                            ? analysis.Percentage : 1 - analysis.Percentage; break;
                        case "SN": strengths["S"] = analysis.PreferenceLetter == 'S'
                            ? analysis.Percentage : 1 - analysis.Percentage; break;
                        case "TF": strengths["T"] = analysis.PreferenceLetter == 'T'
                            ? analysis.Percentage : 1 - analysis.Percentage; break;
                        case "JP": strengths["J"] = analysis.PreferenceLetter == 'J'
                            ? analysis.Percentage : 1 - analysis.Percentage; break;
                    }
                }
                // Ensure all four keys exist
                foreach (var key in new[] { "E", "S", "T", "J" })
                {
                    if (!strengths.ContainsKey(key)) strengths[key] = 0.5;
                }
                // Determine clarity categories
                PreferenceStrength GetClarity(double ratio)
                {
                    var d = Math.Abs(ratio - 0.5) * 2.0;
                    if (d >= 0.75) return PreferenceStrength.VERY_CLEAR;
                    if (d >= 0.5) return PreferenceStrength.CLEAR;
                    if (d >= 0.25) return PreferenceStrength.MODERATE;
                    return PreferenceStrength.SLIGHT;
                }
                var eiClarity = GetClarity(strengths["E"]);
                var snClarity = GetClarity(strengths["S"]);
                var tfClarity = GetClarity(strengths["T"]);
                var jpClarity = GetClarity(strengths["J"]);
                // Update session
                session.CompleteAssessment(
                    personality.Id,
                    (decimal)strengths["E"],
                    (decimal)strengths["S"],
                    (decimal)strengths["T"],
                    (decimal)strengths["J"],
                    eiClarity,
                    snClarity,
                    tfClarity,
                    jpClarity);
                await _repository.UpdateSessionWithResultsAsync(session);
                _logger.LogInformation("Enhanced personality type {Type} calculated for session {SessionId}", result.TypeCode, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating enhanced personality type for session {SessionId}", sessionId);
                throw;
            }
        }
        /// <summary>
        /// Delegate to the underlying EnhancedPersonalityScoringService.  This method
        /// does not persist results, but simply calculates the result using the
        /// provided session object, answers and questions.  Useful for
        /// retrieving statistical metrics without committing to the database.
        /// </summary>
        public Task<EnhancedPersonalityResult> CalculatePersonalityTypeAsync(
            AssessmentSession session,
            List<AssessmentAnswer> answers,
            Dictionary<int, Question> questions)
        {
            return _inner.CalculatePersonalityTypeAsync(session, answers, questions);
        }

        /// <summary>
        /// Generates a quality assessment report for a given session.  The report
        /// includes statistical metrics such as internal consistency, response
        /// consistency, response bias and an overall quality level.  Results are
        /// returned as a dictionary for ease of use by API controllers.
        /// </summary>
        public async Task<Dictionary<string, object>> GetQualityAssessmentReportAsync(int sessionId)
        {
            try
            {
                var session = await _repository.GetSessionByIdAsync(sessionId);
                if (session == null)
                    throw new ArgumentException($"Session {sessionId} not found");
                var answers = await _repository.GetAnswersBySessionIdAsync(sessionId);
                var questions = await _repository.GetActiveQuestionsAsync();
                var result = await _inner.CalculatePersonalityTypeAsync(session, answers, questions);
                var metrics = result.StatisticalMetrics;
                // Derive quality level using same thresholds as Enhanced service
                string quality = GetQualityLevel(metrics.InternalConsistency);
                return new Dictionary<string, object>
                {
                    ["personality_type"] = result.TypeCode,
                    ["dimension_strengths"] = result.PreferenceStrengths,
                    ["dimension_clarity"] = result.PreferenceClarity,
                    ["statistical_metrics"] = metrics,
                    ["borderline_dimensions"] = result.BorderlineDimensions,
                    ["quality_level"] = quality,
                    ["confidence_interval"] = result.StatisticalMetrics.ConfidenceInterval
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quality assessment report for session {SessionId}", sessionId);
                throw;
            }
        }

        // Helper to map internal consistency to quality level using thresholds from original Enhanced service
        private string GetQualityLevel(double score)
        {
            // Same thresholds as EnhancedPersonalityScoringService.QualityThresholds
            if (score >= 0.85) return "Excellent";
            if (score >= 0.70) return "Good";
            if (score >= 0.55) return "Acceptable";
            return "Questionable";
        }
    }
}
