using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Masark.Application.Services
{
    public class StatisticalMetrics
    {
        public double InternalConsistency { get; set; }  // Cronbach's alpha equivalent
        public double ResponseConsistency { get; set; }  // Consistency across similar questions
        public double ExtremeResponseBias { get; set; }  // Tendency to choose extreme options
        public double AcquiescenceBias { get; set; }  // Tendency to agree/choose first option
        public double ResponseTimeVariance { get; set; }  // Variance in response times (if available)
        public (double Lower, double Upper) ConfidenceInterval { get; set; }  // 95% confidence interval for type certainty
    }

    public class DimensionAnalysis
    {
        public string Dimension { get; set; }
        public int RawScore { get; set; }
        public int TotalQuestions { get; set; }
        public double Percentage { get; set; }
        public char PreferenceLetter { get; set; }
        public PreferenceStrength StrengthCategory { get; set; }
        public double ConfidenceLevel { get; set; }
        public double StandardError { get; set; }
        public double ZScore { get; set; }  // How many standard deviations from neutral (50%)
    }

    public class EnhancedPersonalityResult
    {
        public string PersonalityType { get; set; }
        public string TypeCode { get; set; }
        public double TypeConfidence { get; set; }  // Overall confidence in type assignment (0-1)
        
        public Dictionary<string, DimensionAnalysis> DimensionAnalyses { get; set; }
        
        public StatisticalMetrics StatisticalMetrics { get; set; }
        
        public List<string> BorderlineDimensions { get; set; }
        public double TypeStabilityPrediction { get; set; }  // Likelihood type will remain stable over time
        public double AssessmentQualityScore { get; set; }  // Overall quality of the assessment (0-1)
        
        public bool RetestingRecommended { get; set; }
        public List<string> AreasForExploration { get; set; }
        public List<string> ConfidenceNotes { get; set; }
        
        public Dictionary<string, double> PreferenceStrengths { get; set; }
        public Dictionary<string, PreferenceStrength> PreferenceClarity { get; set; }

        public EnhancedPersonalityResult()
        {
            DimensionAnalyses = new Dictionary<string, DimensionAnalysis>();
            BorderlineDimensions = new List<string>();
            AreasForExploration = new List<string>();
            ConfidenceNotes = new List<string>();
            PreferenceStrengths = new Dictionary<string, double>();
            PreferenceClarity = new Dictionary<string, PreferenceStrength>();
        }
    }

    public interface IEnhancedPersonalityScoringService
    {
        Task<EnhancedPersonalityResult> CalculateEnhancedPersonalityTypeAsync(int sessionId);
        Task<EnhancedPersonalityResult> CalculatePersonalityTypeAsync(
            AssessmentSession session, 
            List<AssessmentAnswer> answers, 
            Dictionary<int, Question> questions);
        Task<Dictionary<string, object>> GetQualityAssessmentReportAsync(int sessionId);
    }

    public class EnhancedPersonalityScoringService : IEnhancedPersonalityScoringService
    {
        private readonly ILogger<EnhancedPersonalityScoringService> _logger;
        
        private static readonly Dictionary<string, (char Letter, double Confidence)> TieBreakingRules = 
            new Dictionary<string, (char, double)>
        {
            { "EI", ('I', 0.51) },  // Slight preference for I with 51% confidence
            { "SN", ('N', 0.51) },  // Slight preference for N with 51% confidence
            { "TF", ('F', 0.51) },  // Slight preference for F with 51% confidence
            { "JP", ('P', 0.51) }   // Slight preference for P with 51% confidence
        };
        
        private static readonly Dictionary<PreferenceStrength, double> ProfessionalThresholds = 
            new Dictionary<PreferenceStrength, double>
        {
            { PreferenceStrength.SLIGHT, 0.56 },      // 56-65% = slight preference
            { PreferenceStrength.MODERATE, 0.66 },    // 66-75% = moderate preference
            { PreferenceStrength.CLEAR, 0.76 },       // 76-85% = clear preference
            { PreferenceStrength.VERY_CLEAR, 0.86 }   // 86%+ = very clear preference
        };
        
        private static readonly Dictionary<string, double> QualityThresholds = new Dictionary<string, double>
        {
            { "excellent", 0.85 },
            { "good", 0.70 },
            { "acceptable", 0.55 },
            { "questionable", 0.40 }
        };

        public EnhancedPersonalityScoringService(ILogger<EnhancedPersonalityScoringService> logger)
        {
            _logger = logger;
        }

        public async Task<EnhancedPersonalityResult> CalculateEnhancedPersonalityTypeAsync(int sessionId)
        {
            try
            {
                _logger.LogInformation("Starting enhanced personality type calculation for session {SessionId}", sessionId);

                throw new NotImplementedException("Data access layer integration needed for enhanced personality scoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced personality type calculation for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<EnhancedPersonalityResult> CalculatePersonalityTypeAsync(
            AssessmentSession session, 
            List<AssessmentAnswer> answers, 
            Dictionary<int, Question> questions)
        {
            try
            {
                _logger.LogInformation("Calculating personality type for session {SessionId}", session.Id);

                var dimensionAnalyses = CalculateDimensionalAnalyses(answers, questions);
                var (typeCode, confidence) = DetermineTypeWithConfidence(dimensionAnalyses);
                var statisticalMetrics = CalculateStatisticalMetrics(answers, questions, dimensionAnalyses);

                var result = new EnhancedPersonalityResult
                {
                    PersonalityType = typeCode,
                    TypeCode = typeCode,
                    TypeConfidence = confidence,
                    DimensionAnalyses = dimensionAnalyses,
                    StatisticalMetrics = statisticalMetrics,
                    PreferenceStrengths = dimensionAnalyses.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value.Percentage
                    ),
                    PreferenceClarity = dimensionAnalyses.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value.StrengthCategory
                    )
                };

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating personality type for session {SessionId}", session.Id);
                throw;
            }
        }

        private Dictionary<string, DimensionAnalysis> CalculateDimensionalAnalyses(
            List<AssessmentAnswer> answers, Dictionary<int, Question> questions)
        {
            var analyses = new Dictionary<string, DimensionAnalysis>();
            var dimensionScores = new Dictionary<string, (int First, int Second, int Total)>
            {
                { "EI", (0, 0, 0) },
                { "SN", (0, 0, 0) },
                { "TF", (0, 0, 0) },
                { "JP", (0, 0, 0) }
            };

            foreach (var answer in answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                    continue;

                var dimensionKey = question.Dimension.ToString();
                if (!dimensionScores.ContainsKey(dimensionKey))
                    continue;

                var (first, second, total) = dimensionScores[dimensionKey];
                
                bool mapsToFirst = (answer.SelectedOption == "A" && question.OptionAMapsToFirst) ||
                                  (answer.SelectedOption == "B" && !question.OptionAMapsToFirst);

                if (mapsToFirst)
                    first++;
                else
                    second++;
                
                total++;
                dimensionScores[dimensionKey] = (first, second, total);
            }

            var dimensionLetters = new Dictionary<string, (char First, char Second)>
            {
                { "EI", ('E', 'I') },
                { "SN", ('S', 'N') },
                { "TF", ('T', 'F') },
                { "JP", ('J', 'P') }
            };

            foreach (var (dimKey, (first, second, total)) in dimensionScores)
            {
                if (total == 0) continue;

                var (firstLetter, secondLetter) = dimensionLetters[dimKey];
                var percentage = (double)Math.Max(first, second) / total;
                var preferenceLetter = first > second ? firstLetter : 
                                     second > first ? secondLetter : 
                                     TieBreakingRules[dimKey].Letter;
                
                var rawScore = preferenceLetter == firstLetter ? first : second;
                
                analyses[dimKey] = new DimensionAnalysis
                {
                    Dimension = dimKey,
                    RawScore = rawScore,
                    TotalQuestions = total,
                    Percentage = percentage,
                    PreferenceLetter = preferenceLetter,
                    StrengthCategory = DetermineStrengthCategory(percentage),
                    ConfidenceLevel = CalculateConfidenceLevel(first, second, total),
                    StandardError = CalculateStandardError(total),
                    ZScore = CalculateZScore(percentage)
                };
            }

            return analyses;
        }

        private double CalculateConfidenceLevel(int firstScore, int secondScore, int totalQuestions)
        {
            if (totalQuestions == 0) return 0.0;
            
            var p = (double)Math.Max(firstScore, secondScore) / totalQuestions;
            var n = totalQuestions;
            
            var se = Math.Sqrt(p * (1 - p) / n);
            
            var z = Math.Abs(p - 0.5) / se;
            
            return Math.Min(1.0, z / 2.0);
        }

        private double CalculateStandardError(int totalQuestions)
        {
            if (totalQuestions == 0) return 0.0;
            
            return Math.Sqrt(0.25 / totalQuestions);
        }

        private double CalculateZScore(double percentage)
        {
            var se = Math.Sqrt(0.25); // Standard error at p=0.5
            return (percentage - 0.5) / se;
        }

        private PreferenceStrength DetermineStrengthCategory(double percentage)
        {
            if (percentage < ProfessionalThresholds[PreferenceStrength.SLIGHT])
                return PreferenceStrength.SLIGHT;
            else if (percentage < ProfessionalThresholds[PreferenceStrength.MODERATE])
                return PreferenceStrength.MODERATE;
            else if (percentage < ProfessionalThresholds[PreferenceStrength.CLEAR])
                return PreferenceStrength.CLEAR;
            else
                return PreferenceStrength.VERY_CLEAR;
        }

        private (string TypeCode, double Confidence) DetermineTypeWithConfidence(
            Dictionary<string, DimensionAnalysis> dimensionAnalyses)
        {
            var typeLetters = new List<char>();
            var confidenceScores = new List<double>();

            var orderedDimensions = new[] { "EI", "SN", "TF", "JP" };
            
            foreach (var dimension in orderedDimensions)
            {
                if (dimensionAnalyses.TryGetValue(dimension, out var analysis))
                {
                    typeLetters.Add(analysis.PreferenceLetter);
                    confidenceScores.Add(analysis.ConfidenceLevel);
                }
                else
                {
                    typeLetters.Add(TieBreakingRules[dimension].Letter);
                    confidenceScores.Add(TieBreakingRules[dimension].Confidence);
                }
            }

            var typeCode = new string(typeLetters.ToArray());
            var overallConfidence = confidenceScores.Count > 0 ? confidenceScores.Average() : 0.0;

            return (typeCode, overallConfidence);
        }

        private StatisticalMetrics CalculateStatisticalMetrics(
            List<AssessmentAnswer> answers, Dictionary<int, Question> questions,
            Dictionary<string, DimensionAnalysis> dimensionAnalyses)
        {
            return new StatisticalMetrics
            {
                InternalConsistency = CalculateInternalConsistency(answers, questions),
                ResponseConsistency = CalculateResponseConsistency(answers, questions),
                ExtremeResponseBias = CalculateExtremeResponseBias(answers),
                AcquiescenceBias = CalculateAcquiescenceBias(answers),
                ResponseTimeVariance = 0.0, // Would need response time data
                ConfidenceInterval = CalculateConfidenceInterval(dimensionAnalyses)
            };
        }

        private double CalculateInternalConsistency(List<AssessmentAnswer> answers, Dictionary<int, Question> questions)
        {
            var dimensionGroups = answers
                .Where(a => questions.ContainsKey(a.QuestionId))
                .GroupBy(a => questions[a.QuestionId].Dimension)
                .ToList();

            if (dimensionGroups.Count == 0) return 0.0;

            var consistencyScores = new List<double>();
            
            foreach (var group in dimensionGroups)
            {
                var responses = group.Select(a => a.SelectedOption == "A" ? 1.0 : 0.0).ToList();
                if (responses.Count > 1)
                {
                    var mean = responses.Average();
                    var variance = responses.Sum(r => Math.Pow(r - mean, 2)) / responses.Count;
                    consistencyScores.Add(1.0 - variance); // Higher consistency = lower variance
                }
            }

            return consistencyScores.Count > 0 ? consistencyScores.Average() : 0.0;
        }

        private double CalculateResponseConsistency(List<AssessmentAnswer> answers, Dictionary<int, Question> questions)
        {
            var dimensionResponses = answers
                .Where(a => questions.ContainsKey(a.QuestionId))
                .GroupBy(a => questions[a.QuestionId].Dimension)
                .ToDictionary(g => g.Key, g => g.Select(a => a.SelectedOption == "A" ? 1.0 : 0.0).ToList());

            var consistencyScores = new List<double>();

            foreach (var (dimension, responses) in dimensionResponses)
            {
                if (responses.Count > 1)
                {
                    var mean = responses.Average();
                    var deviations = responses.Select(r => Math.Abs(r - mean)).ToList();
                    var consistency = 1.0 - deviations.Average(); // Lower deviation = higher consistency
                    consistencyScores.Add(Math.Max(0.0, consistency));
                }
            }

            return consistencyScores.Count > 0 ? consistencyScores.Average() : 0.0;
        }

        private double CalculateExtremeResponseBias(List<AssessmentAnswer> answers)
        {
            var aCount = answers.Count(a => a.SelectedOption == "A");
            var bCount = answers.Count(a => a.SelectedOption == "B");
            var total = answers.Count;

            if (total == 0) return 0.0;

            var balance = Math.Abs(aCount - bCount) / (double)total;
            return balance; // Higher values indicate more extreme response bias
        }

        private double CalculateAcquiescenceBias(List<AssessmentAnswer> answers)
        {
            var aCount = answers.Count(a => a.SelectedOption == "A");
            var total = answers.Count;

            if (total == 0) return 0.0;

            var aRatio = aCount / (double)total;
            return Math.Abs(aRatio - 0.5) * 2; // 0 = no bias, 1 = maximum bias
        }

        private (double Lower, double Upper) CalculateConfidenceInterval(Dictionary<string, DimensionAnalysis> dimensionAnalyses)
        {
            var confidenceLevels = dimensionAnalyses.Values.Select(d => d.ConfidenceLevel).ToList();
            
            if (confidenceLevels.Count == 0)
                return (0.0, 0.0);

            var mean = confidenceLevels.Average();
            var stdDev = Math.Sqrt(confidenceLevels.Sum(c => Math.Pow(c - mean, 2)) / confidenceLevels.Count);
            var marginOfError = 1.96 * stdDev / Math.Sqrt(confidenceLevels.Count); // 95% CI

            return (Math.Max(0.0, mean - marginOfError), Math.Min(1.0, mean + marginOfError));
        }

        public async Task<Dictionary<string, object>> GetQualityAssessmentReportAsync(int sessionId)
        {
            try
            {
                throw new NotImplementedException("Data access layer integration needed for quality assessment report");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quality assessment report for session {SessionId}", sessionId);
                throw;
            }
        }

        private string GetQualityLevel(double score)
        {
            if (score >= QualityThresholds["excellent"])
                return "Excellent";
            else if (score >= QualityThresholds["good"])
                return "Good";
            else if (score >= QualityThresholds["acceptable"])
                return "Acceptable";
            else
                return "Questionable";
        }
    }
}
