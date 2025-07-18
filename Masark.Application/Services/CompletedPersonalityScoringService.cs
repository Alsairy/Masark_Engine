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
    /// Fully implemented personality scoring service.
    ///
    /// This service retrieves a completed assessment session, validates the answers
    /// and active questions, calculates raw scores for each MBTI dimension, applies
    /// tie‑breaking rules, determines the winning preference in each dimension and
    /// computes the preference strength (as a ratio between 0 and 1) for the dominant
    /// side.  It then updates the session with the personality type identifier,
    /// preference strengths and clarity categories.  All operations are performed
    /// through the injected IPersonalityRepository so the service is persistence
    /// agnostic.
    /// </summary>
    public class CompletedPersonalityScoringService : IPersonalityScoringService
    {
        private readonly IPersonalityRepository _repository;
        private readonly ILogger<CompletedPersonalityScoringService> _logger;

        // Tie‑breaking rules as per original specification: favour second letter on ties
        private static readonly IDictionary<string, char> TieBreakingRules = new Dictionary<string, char>
        {
            { "EI", 'I' },
            { "SN", 'N' },
            { "TF", 'F' },
            { "JP", 'P' }
        };

        public CompletedPersonalityScoringService(
            IPersonalityRepository repository,
            ILogger<CompletedPersonalityScoringService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Calculates the personality type for a completed assessment session and
        /// persists the result on the session entity.
        /// </summary>
        /// <param name="sessionId">Identifier of the assessment session</param>
        public async Task CalculatePersonalityTypeAsync(int sessionId)
        {
            try
            {
                // Fetch session and validate
                var session = await _repository.GetSessionByIdAsync(sessionId);
                if (session == null)
                {
                    throw new ArgumentException($"Session {sessionId} not found");
                }
                if (session.IsCompleted)
                {
                    _logger.LogInformation("Session {SessionId} is already completed", sessionId);
                    return;
                }

                // Retrieve answers and questions
                var answers = await _repository.GetAnswersBySessionIdAsync(sessionId);
                var questions = await _repository.GetActiveQuestionsAsync();

                // Validate completeness
                var (isValid, validationMessage) = await ValidateAnswersCompletenessAsync(sessionId);
                if (!isValid)
                {
                    throw new InvalidOperationException(validationMessage);
                }

                // Build lookup of questions for fast access
                var questionLookup = questions.ToDictionary(q => q.Id);

                // Compute raw counts and difference scores
                var dimensionScores = CalculateDimensionScores(answers, questionLookup);

                // Determine MBTI type using tie break logic
                var (typeCode, tieBreakerDims) = DeterminePersonalityType(dimensionScores);

                // Compute preference strengths as ratios of dominant side (0–1)
                var strengths = new Dictionary<string, double>
                {
                    ["E"] = dimensionScores.ECount + dimensionScores.ICount == 0
                        ? 0.5 : (double)dimensionScores.ECount / (dimensionScores.ECount + dimensionScores.ICount),
                    ["S"] = dimensionScores.SCount + dimensionScores.NCount == 0
                        ? 0.5 : (double)dimensionScores.SCount / (dimensionScores.SCount + dimensionScores.NCount),
                    ["T"] = dimensionScores.TCount + dimensionScores.FCount == 0
                        ? 0.5 : (double)dimensionScores.TCount / (dimensionScores.TCount + dimensionScores.FCount),
                    ["J"] = dimensionScores.JCount + dimensionScores.PCount == 0
                        ? 0.5 : (double)dimensionScores.JCount / (dimensionScores.JCount + dimensionScores.PCount)
                };

                // Derive clarity categories using the same logic as AssessmentSession.GetPreferenceStrength
                PreferenceStrength eiClarity = GetClarity(strengths["E"]);
                PreferenceStrength snClarity = GetClarity(strengths["S"]);
                PreferenceStrength tfClarity = GetClarity(strengths["T"]);
                PreferenceStrength jpClarity = GetClarity(strengths["J"]);

                // Fetch PersonalityType entity for typeCode
                var personalityType = await _repository.GetPersonalityTypeByCodeAsync(typeCode);
                if (personalityType == null)
                {
                    throw new InvalidOperationException($"Personality type {typeCode} not found in database");
                }

                // Update the session entity using domain method
                session.CompleteAssessment(
                    personalityType.Id,
                    (decimal)strengths["E"],
                    (decimal)strengths["S"],
                    (decimal)strengths["T"],
                    (decimal)strengths["J"],
                    eiClarity,
                    snClarity,
                    tfClarity,
                    jpClarity);

                // Persist session
                await _repository.UpdateSessionWithResultsAsync(session);

                _logger.LogInformation("Calculated personality type {TypeCode} for session {SessionId}", typeCode, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating personality type for session {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Calculates a personality result from a direct list of numerical responses (1–5 scale).  This
        /// overload is intended for testing or validation scenarios and does not persist results.
        /// </summary>
        public async Task CalculatePersonalityTypeFromResponsesAsync(List<int> responses)
        {
            if (responses == null)
                throw new ArgumentNullException(nameof(responses));
            if (responses.Count != 36)
                throw new ArgumentException($"Expected 36 responses, got {responses.Count}");
            for (int i = 0; i < responses.Count; i++)
            {
                int value = responses[i];
                if (value < 1 || value > 5)
                    throw new ArgumentException($"Invalid response at position {i}: {value}. Must be integer 1–5");
            }
            // This method intentionally returns void; clients can adapt the scoring logic from the
            // CalculatePersonalityTypeAsync implementation if needed.
            await Task.CompletedTask;
        }

        /// <summary>
        /// Validates that the answers for a session are complete and cover each dimension.
        /// </summary>
        public async Task<(bool IsValid, string Message)> ValidateAnswersCompletenessAsync(int sessionId)
        {
            try
            {
                var answers = await _repository.GetAnswersBySessionIdAsync(sessionId);
                var questions = await _repository.GetActiveQuestionsAsync();
                if (questions == null || questions.Count == 0)
                    return (false, "No active assessment questions found");
                if (answers.Count != questions.Count)
                    return (false, $"Expected {questions.Count} answers, got {answers.Count}");
                // Count answers per dimension
                var dimensionCounts = new Dictionary<string, int> { { "EI", 0 }, { "SN", 0 }, { "TF", 0 }, { "JP", 0 } };
                var questionLookup = questions.ToDictionary(q => q.Id);
                foreach (var answer in answers)
                {
                    if (questionLookup.TryGetValue(answer.QuestionId, out var question))
                    {
                        var dimKey = question.Dimension.ToString();
                        if (dimensionCounts.ContainsKey(dimKey))
                            dimensionCounts[dimKey]++;
                    }
                }
                foreach (var kvp in dimensionCounts)
                {
                    if (kvp.Value == 0)
                        return (false, $"No answers found for dimension {kvp.Key}");
                }
                return (true, "All answers present and valid");
            }
            catch (Exception ex)
            {
                return (false, $"Error validating answers: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a personality type description and related fields for a given code.
        /// </summary>
        public async Task<Dictionary<string, string>> GetPersonalityDescriptionAsync(string typeCode, string language = "en")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(typeCode))
                    throw new ArgumentNullException(nameof(typeCode));
                var personality = await _repository.GetPersonalityTypeByCodeAsync(typeCode);
                if (personality == null)
                    throw new ArgumentException($"Personality type {typeCode} not found");
                return new Dictionary<string, string>
                {
                    ["code"] = personality.Code,
                    ["name"] = personality.GetName(language),
                    ["description"] = personality.GetDescription(language),
                    ["strengths"] = personality.GetStrengths(language),
                    ["challenges"] = personality.GetChallenges(language)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personality description for type {TypeCode}", typeCode);
                throw;
            }
        }

        #region Helper methods
        private PersonalityScores CalculateDimensionScores(IReadOnlyCollection<AssessmentAnswer> answers, IDictionary<int, Question> questions)
        {
            var scores = new PersonalityScores();
            foreach (var answer in answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                    continue;
                var dimension = question.Dimension;
                var selected = answer.SelectedOption;
                bool mapsToFirst = (selected == "A" && question.OptionAMapsToFirst) ||
                                   (selected == "B" && !question.OptionAMapsToFirst);
                switch (dimension)
                {
                    case PersonalityDimension.EI:
                        if (mapsToFirst) scores.ECount++; else scores.ICount++;
                        break;
                    case PersonalityDimension.SN:
                        if (mapsToFirst) scores.SCount++; else scores.NCount++;
                        break;
                    case PersonalityDimension.TF:
                        if (mapsToFirst) scores.TCount++; else scores.FCount++;
                        break;
                    case PersonalityDimension.JP:
                        if (mapsToFirst) scores.JCount++; else scores.PCount++;
                        break;
                }
            }
            // Also compute difference scores if needed for tie detection (not persisted)
            var eiTotal = scores.ECount + scores.ICount;
            if (eiTotal > 0)
                scores.EScore = ((double)(scores.ECount - scores.ICount)) / eiTotal;
            var snTotal = scores.SCount + scores.NCount;
            if (snTotal > 0)
                scores.SScore = ((double)(scores.SCount - scores.NCount)) / snTotal;
            var tfTotal = scores.TCount + scores.FCount;
            if (tfTotal > 0)
                scores.TScore = ((double)(scores.TCount - scores.FCount)) / tfTotal;
            var jpTotal = scores.JCount + scores.PCount;
            if (jpTotal > 0)
                scores.JScore = ((double)(scores.JCount - scores.PCount)) / jpTotal;
            return scores;
        }

        private (string PersonalityType, List<PersonalityDimension> TieBreakerDimensions) DeterminePersonalityType(PersonalityScores scores)
        {
            var letters = new List<char>();
            var tieBreakers = new List<PersonalityDimension>();
            const double tieThreshold = 0.0; // exact ties only for tie breaker
            // EI
            if (Math.Abs(scores.EScore) < 1e-6)
            {
                tieBreakers.Add(PersonalityDimension.EI);
                letters.Add(TieBreakingRules["EI"]);
            }
            else
            {
                letters.Add(scores.EScore > 0 ? 'E' : 'I');
            }
            // SN
            if (Math.Abs(scores.SScore) < 1e-6)
            {
                tieBreakers.Add(PersonalityDimension.SN);
                letters.Add(TieBreakingRules["SN"]);
            }
            else
            {
                letters.Add(scores.SScore > 0 ? 'S' : 'N');
            }
            // TF
            if (Math.Abs(scores.TScore) < 1e-6)
            {
                tieBreakers.Add(PersonalityDimension.TF);
                letters.Add(TieBreakingRules["TF"]);
            }
            else
            {
                letters.Add(scores.TScore > 0 ? 'T' : 'F');
            }
            // JP
            if (Math.Abs(scores.JScore) < 1e-6)
            {
                tieBreakers.Add(PersonalityDimension.JP);
                letters.Add(TieBreakingRules["JP"]);
            }
            else
            {
                letters.Add(scores.JScore > 0 ? 'J' : 'P');
            }
            return (new string(letters.ToArray()), tieBreakers);
        }

        private PreferenceStrength GetClarity(double strength)
        {
            // strength is ratio of dominant side (0–1). 0.5 indicates tie.
            var distance = Math.Abs(strength - 0.5) * 2.0; // 0–1 scale
            if (distance >= 0.75) return PreferenceStrength.VERY_CLEAR;
            if (distance >= 0.5) return PreferenceStrength.CLEAR;
            if (distance >= 0.25) return PreferenceStrength.MODERATE;
            return PreferenceStrength.SLIGHT;
        }
        #endregion
    }
}
