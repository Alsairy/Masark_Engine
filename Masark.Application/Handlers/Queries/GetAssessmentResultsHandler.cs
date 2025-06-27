using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Queries.Assessment;
using Masark.Application.Services;
using Masark.Application.Interfaces;

namespace Masark.Application.Handlers.Queries
{
    public class GetAssessmentResultsHandler : IRequestHandler<GetAssessmentResultsQuery, GetAssessmentResultsResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly IEnhancedPersonalityScoringService _scoringService;
        private readonly ILogger<GetAssessmentResultsHandler> _logger;

        public GetAssessmentResultsHandler(
            IPersonalityRepository personalityRepository,
            IEnhancedPersonalityScoringService scoringService,
            ILogger<GetAssessmentResultsHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _scoringService = scoringService;
            _logger = logger;
        }

        public async Task<GetAssessmentResultsResult> Handle(GetAssessmentResultsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(request.SessionId);
                if (session == null)
                {
                    return new GetAssessmentResultsResult
                    {
                        Success = false,
                        ErrorMessage = "Assessment session not found"
                    };
                }

                if (session.TenantId != request.TenantId)
                {
                    return new GetAssessmentResultsResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied"
                    };
                }

                var result = new GetAssessmentResultsResult
                {
                    Session = session,
                    PersonalityType = session.GetPersonalityTypeCode(),
                    DimensionScores = session.GetDimensionScores(),
                    Success = true
                };

                if (request.IncludeStatistics && !string.IsNullOrEmpty(session.GetPersonalityTypeCode()))
                {
                    var answers = await _personalityRepository.GetAnswersBySessionIdAsync(request.SessionId);
                    var questions = await _personalityRepository.GetActiveQuestionsAsync();
                    
                    var enhancedResult = await _scoringService.CalculatePersonalityTypeAsync(session, answers, questions);
                    result.Statistics = new Dictionary<string, object>
                    {
                        ["StatisticalMetrics"] = enhancedResult.StatisticalMetrics,
                        ["DimensionAnalyses"] = enhancedResult.DimensionAnalyses
                    };
                }

                _logger.LogInformation("Retrieved assessment results for session {SessionId}", request.SessionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve assessment results for session {SessionId}", request.SessionId);
                return new GetAssessmentResultsResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve assessment results"
                };
            }
        }
    }
}
