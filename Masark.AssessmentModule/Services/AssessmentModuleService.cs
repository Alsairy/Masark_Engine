using Microsoft.Extensions.Logging;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;
using Masark.Application.Services;
using Masark.Domain.Enums;

namespace Masark.AssessmentModule.Services
{
    public interface IAssessmentModuleService
    {
        Task<AssessmentSession> CreateSessionAsync(string userId, string languagePreference, string tenantId);
        Task<bool> SubmitAnswerAsync(string sessionToken, int questionId, string selectedOption, PreferenceStrength strength);
        Task<AssessmentResult> CompleteAssessmentAsync(string sessionToken);
        Task<IEnumerable<Question>> GetQuestionsAsync(string language);
        Task<AssessmentStatistics> GetStatisticsAsync(string tenantId);
    }

    public class AssessmentModuleService : IAssessmentModuleService
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly IPersonalityScoringService _scoringService;
        private readonly IAssessmentStateMachineService _stateMachineService;
        private readonly ILogger<AssessmentModuleService> _logger;

        public AssessmentModuleService(
            IPersonalityRepository personalityRepository,
            IPersonalityScoringService scoringService,
            IAssessmentStateMachineService stateMachineService,
            ILogger<AssessmentModuleService> logger)
        {
            _personalityRepository = personalityRepository;
            _scoringService = scoringService;
            _stateMachineService = stateMachineService;
            _logger = logger;
        }

        public async Task<AssessmentSession> CreateSessionAsync(string userId, string languagePreference, string tenantId)
        {
            _logger.LogInformation("Creating assessment session for user in tenant");

            var session = new AssessmentSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                LanguagePreference = languagePreference,
                State = AssessmentState.Started,
                CreatedAt = DateTime.UtcNow,
                SessionToken = GenerateSessionToken()
            };

            await _personalityRepository.CreateSessionAsync(session);
            
            _logger.LogInformation("Assessment session created with token");
            return session;
        }

        public async Task<bool> SubmitAnswerAsync(string sessionToken, int questionId, string selectedOption, PreferenceStrength strength)
        {
            _logger.LogInformation("Submitting answer for session");

            var session = await _personalityRepository.GetSessionByTokenAsync(sessionToken);
            if (session == null)
            {
                _logger.LogWarning("Session not found for token");
                return false;
            }

            var answer = new AssessmentAnswer
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                QuestionId = questionId,
                SelectedOption = selectedOption,
                PreferenceStrength = strength,
                AnsweredAt = DateTime.UtcNow
            };

            await _personalityRepository.SaveAnswerAsync(answer);
            await _stateMachineService.ProcessAnswerAsync(session, answer);

            _logger.LogInformation("Answer submitted successfully for session");
            return true;
        }

        public async Task<AssessmentResult> CompleteAssessmentAsync(string sessionToken)
        {
            _logger.LogInformation("Completing assessment for session");

            var session = await _personalityRepository.GetSessionByTokenAsync(sessionToken);
            if (session == null)
            {
                throw new InvalidOperationException($"Session not found for token {sessionToken}");
            }

            var answers = await _personalityRepository.GetSessionAnswersAsync(session.Id);
            var personalityResult = await _scoringService.CalculatePersonalityTypeAsync(answers);

            session.State = AssessmentState.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.PersonalityType = personalityResult.PersonalityType;

            await _personalityRepository.UpdateSessionAsync(session);

            var result = new AssessmentResult
            {
                SessionId = session.Id,
                PersonalityType = personalityResult.PersonalityType,
                DimensionScores = personalityResult.DimensionScores,
                ConfidenceLevel = personalityResult.ConfidenceLevel,
                CompletedAt = session.CompletedAt.Value
            };

            _logger.LogInformation("Assessment completed for session");

            return result;
        }

        public async Task<IEnumerable<Question>> GetQuestionsAsync(string language)
        {
            _logger.LogInformation("Retrieving questions for language");
            return await _personalityRepository.GetQuestionsAsync(language);
        }

        public async Task<AssessmentStatistics> GetStatisticsAsync(string tenantId)
        {
            _logger.LogInformation("Retrieving assessment statistics for tenant");
            return await _personalityRepository.GetAssessmentStatisticsAsync(tenantId);
        }

        private string GenerateSessionToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }

    public class AssessmentResult
    {
        public Guid SessionId { get; set; }
        public string PersonalityType { get; set; } = string.Empty;
        public Dictionary<string, double> DimensionScores { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class AssessmentStatistics
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> PersonalityTypeDistribution { get; set; } = new();
        public TimeSpan AverageCompletionTime { get; set; }
    }
}
