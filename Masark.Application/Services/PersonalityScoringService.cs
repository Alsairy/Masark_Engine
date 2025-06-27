using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Masark.Application.Services
{
    public class PersonalityScores
    {
        public int EScore { get; set; } = 0;  // Extraversion score
        public int IScore { get; set; } = 0;  // Introversion score
        public int SScore { get; set; } = 0;  // Sensing score
        public int NScore { get; set; } = 0;  // Intuition score
        public int TScore { get; set; } = 0;  // Thinking score
        public int FScore { get; set; } = 0;  // Feeling score
        public int JScore { get; set; } = 0;  // Judging score
        public int PScore { get; set; } = 0;  // Perceiving score
    }

    public class PersonalityResult
    {
        public string PersonalityType { get; set; }
        public string TypeCode { get; set; }
        public PersonalityScores DimensionScores { get; set; }
        public Dictionary<string, double> PreferenceStrengths { get; set; }  // E.g., {'E': 0.67, 'S': 0.56, ...}
        public Dictionary<string, PreferenceStrength> PreferenceClarity { get; set; }
        public List<string> BorderlineDimensions { get; set; }  // Dimensions that were close calls
        public Dictionary<string, int> TotalQuestionsPerDimension { get; set; }

        public PersonalityResult()
        {
            DimensionScores = new PersonalityScores();
            PreferenceStrengths = new Dictionary<string, double>();
            PreferenceClarity = new Dictionary<string, PreferenceStrength>();
            BorderlineDimensions = new List<string>();
            TotalQuestionsPerDimension = new Dictionary<string, int>();
        }
    }

    public interface IPersonalityScoringService
    {
        Task<PersonalityResult> CalculatePersonalityTypeAsync(int sessionId);
        Task<PersonalityResult> CalculatePersonalityTypeFromResponsesAsync(List<int> responses);
        Task<(bool IsValid, string Message)> ValidateAnswersCompletenessAsync(int sessionId);
        Task<Dictionary<string, object>> GetPersonalityDescriptionAsync(string typeCode, string language = "en");
    }

    public class PersonalityScoringService : IPersonalityScoringService
    {
        private readonly ILogger<PersonalityScoringService> _logger;
        
        private static readonly Dictionary<string, char> TieBreakingRules = new Dictionary<string, char>
        {
            { "EI", 'I' },  // If E = I, assign I
            { "SN", 'N' },  // If S = N, assign N  
            { "TF", 'F' },  // If T = F, assign F
            { "JP", 'P' }   // If J = P, assign P
        };
        
        private static readonly Dictionary<PreferenceStrength, double> StrengthThresholds = new Dictionary<PreferenceStrength, double>
        {
            { PreferenceStrength.SLIGHT, 0.60 },      // <60% = slight
            { PreferenceStrength.MODERATE, 0.75 },    // 60-75% = moderate
            { PreferenceStrength.CLEAR, 0.90 },       // 76-90% = clear
            { PreferenceStrength.VERY_CLEAR, 1.0 }    // >90% = very clear
        };

        public PersonalityScoringService(ILogger<PersonalityScoringService> logger)
        {
            _logger = logger;
        }

        public async Task<PersonalityResult> CalculatePersonalityTypeAsync(int sessionId)
        {
            try
            {
                throw new NotImplementedException("Data access layer integration needed");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating personality type for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<PersonalityResult> CalculatePersonalityTypeFromResponsesAsync(List<int> responses)
        {
            try
            {
                if (responses.Count != 36)
                    throw new ArgumentException($"Expected 36 responses, got {responses.Count}");

                for (int i = 0; i < responses.Count; i++)
                {
                    if (responses[i] < 1 || responses[i] > 5)
                        throw new ArgumentException($"Invalid response at position {i}: {responses[i]}. Must be integer 1-5");
                }

                throw new NotImplementedException("Question data access needed for response calculation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating personality type from responses");
                throw;
            }
        }

        private PersonalityScores CalculateDimensionScores(List<AssessmentAnswer> answers, Dictionary<int, Question> questions)
        {
            var scores = new PersonalityScores();

            foreach (var answer in answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                    continue;

                var dimension = question.Dimension;
                var selectedOption = answer.SelectedOption;

                bool mapsToFirst = (selectedOption == "A" && question.OptionAMapsToFirst) ||
                                  (selectedOption == "B" && !question.OptionAMapsToFirst);

                switch (dimension)
                {
                    case PersonalityDimension.EI:
                        if (mapsToFirst)
                            scores.EScore++;
                        else
                            scores.IScore++;
                        break;
                    case PersonalityDimension.SN:
                        if (mapsToFirst)
                            scores.SScore++;
                        else
                            scores.NScore++;
                        break;
                    case PersonalityDimension.TF:
                        if (mapsToFirst)
                            scores.TScore++;
                        else
                            scores.FScore++;
                        break;
                    case PersonalityDimension.JP:
                        if (mapsToFirst)
                            scores.JScore++;
                        else
                            scores.PScore++;
                        break;
                }
            }

            return scores;
        }

        private string DeterminePersonalityType(PersonalityScores scores)
        {
            var typeLetters = new List<char>();

            if (scores.EScore > scores.IScore)
                typeLetters.Add('E');
            else if (scores.IScore > scores.EScore)
                typeLetters.Add('I');
            else
            {
                typeLetters.Add(TieBreakingRules["EI"]);
                _logger.LogDebug("Applied tie-breaking rule for E-I dimension");
            }

            if (scores.SScore > scores.NScore)
                typeLetters.Add('S');
            else if (scores.NScore > scores.SScore)
                typeLetters.Add('N');
            else
            {
                typeLetters.Add(TieBreakingRules["SN"]);
                _logger.LogDebug("Applied tie-breaking rule for S-N dimension");
            }

            if (scores.TScore > scores.FScore)
                typeLetters.Add('T');
            else if (scores.FScore > scores.TScore)
                typeLetters.Add('F');
            else
            {
                typeLetters.Add(TieBreakingRules["TF"]);
                _logger.LogDebug("Applied tie-breaking rule for T-F dimension");
            }

            if (scores.JScore > scores.PScore)
                typeLetters.Add('J');
            else if (scores.PScore > scores.JScore)
                typeLetters.Add('P');
            else
            {
                typeLetters.Add(TieBreakingRules["JP"]);
                _logger.LogDebug("Applied tie-breaking rule for J-P dimension");
            }

            return new string(typeLetters.ToArray());
        }

        private Dictionary<string, double> CalculatePreferenceStrengths(PersonalityScores scores)
        {
            var strengths = new Dictionary<string, double>();

            int eiTotal = scores.EScore + scores.IScore;
            int snTotal = scores.SScore + scores.NScore;
            int tfTotal = scores.TScore + scores.FScore;
            int jpTotal = scores.JScore + scores.PScore;

            if (eiTotal > 0)
            {
                double eStrength = (double)scores.EScore / eiTotal;
                double iStrength = (double)scores.IScore / eiTotal;
                strengths["E"] = eStrength;
                strengths["I"] = iStrength;
            }

            if (snTotal > 0)
            {
                double sStrength = (double)scores.SScore / snTotal;
                double nStrength = (double)scores.NScore / snTotal;
                strengths["S"] = sStrength;
                strengths["N"] = nStrength;
            }

            if (tfTotal > 0)
            {
                double tStrength = (double)scores.TScore / tfTotal;
                double fStrength = (double)scores.FScore / tfTotal;
                strengths["T"] = tStrength;
                strengths["F"] = fStrength;
            }

            if (jpTotal > 0)
            {
                double jStrength = (double)scores.JScore / jpTotal;
                double pStrength = (double)scores.PScore / jpTotal;
                strengths["J"] = jStrength;
                strengths["P"] = pStrength;
            }

            return strengths;
        }

        private Dictionary<string, PreferenceStrength> CalculatePreferenceClarity(Dictionary<string, double> strengths)
        {
            var clarity = new Dictionary<string, PreferenceStrength>();

            var dimensions = new[]
            {
                ("EI", Math.Max(strengths.GetValueOrDefault("E", 0), strengths.GetValueOrDefault("I", 0))),
                ("SN", Math.Max(strengths.GetValueOrDefault("S", 0), strengths.GetValueOrDefault("N", 0))),
                ("TF", Math.Max(strengths.GetValueOrDefault("T", 0), strengths.GetValueOrDefault("F", 0))),
                ("JP", Math.Max(strengths.GetValueOrDefault("J", 0), strengths.GetValueOrDefault("P", 0)))
            };

            foreach (var (dimName, strength) in dimensions)
            {
                if (strength < StrengthThresholds[PreferenceStrength.SLIGHT])
                    clarity[dimName] = PreferenceStrength.SLIGHT;
                else if (strength < StrengthThresholds[PreferenceStrength.MODERATE])
                    clarity[dimName] = PreferenceStrength.MODERATE;
                else if (strength < StrengthThresholds[PreferenceStrength.CLEAR])
                    clarity[dimName] = PreferenceStrength.CLEAR;
                else
                    clarity[dimName] = PreferenceStrength.VERY_CLEAR;
            }

            return clarity;
        }

        private List<string> IdentifyBorderlineDimensions(Dictionary<string, double> strengths, double threshold = 0.55)
        {
            var borderline = new List<string>();

            var dimensions = new[]
            {
                ("EI", Math.Max(strengths.GetValueOrDefault("E", 0), strengths.GetValueOrDefault("I", 0))),
                ("SN", Math.Max(strengths.GetValueOrDefault("S", 0), strengths.GetValueOrDefault("N", 0))),
                ("TF", Math.Max(strengths.GetValueOrDefault("T", 0), strengths.GetValueOrDefault("F", 0))),
                ("JP", Math.Max(strengths.GetValueOrDefault("J", 0), strengths.GetValueOrDefault("P", 0)))
            };

            foreach (var (dimName, strength) in dimensions)
            {
                if (strength < threshold)  // Less than 55% means it was close
                    borderline.Add(dimName);
            }

            return borderline;
        }

        private Dictionary<string, int> CountQuestionsPerDimension(List<Question> questions)
        {
            var counts = new Dictionary<string, int> { { "EI", 0 }, { "SN", 0 }, { "TF", 0 }, { "JP", 0 } };

            foreach (var question in questions)
            {
                string dimKey = question.Dimension.ToString();
                if (counts.ContainsKey(dimKey))
                    counts[dimKey]++;
            }

            return counts;
        }

        public async Task<(bool IsValid, string Message)> ValidateAnswersCompletenessAsync(int sessionId)
        {
            try
            {
                throw new NotImplementedException("Data access layer integration needed");
            }
            catch (Exception ex)
            {
                return (false, $"Error validating answers: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, object>> GetPersonalityDescriptionAsync(string typeCode, string language = "en")
        {
            try
            {
                throw new NotImplementedException("Data access layer integration needed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personality description for type {TypeCode}", typeCode);
                throw;
            }
        }
    }
}
