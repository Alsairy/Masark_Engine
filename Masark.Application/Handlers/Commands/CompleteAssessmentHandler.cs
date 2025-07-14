using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Commands.Assessment;
using Masark.Application.Events.Assessment;
using Masark.Application.Services;
using Masark.Application.Interfaces;

namespace Masark.Application.Handlers.Commands
{
    public class CompleteAssessmentHandler : IRequestHandler<CompleteAssessmentCommand, CompleteAssessmentResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly IEnhancedPersonalityScoringService _scoringService;
        private readonly IMediator _mediator;
        private readonly ILogger<CompleteAssessmentHandler> _logger;

        public CompleteAssessmentHandler(
            IPersonalityRepository personalityRepository,
            IEnhancedPersonalityScoringService scoringService,
            IMediator mediator,
            ILogger<CompleteAssessmentHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _scoringService = scoringService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<CompleteAssessmentResult> Handle(CompleteAssessmentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(request.SessionId);
                if (session == null)
                {
                    return new CompleteAssessmentResult
                    {
                        Success = false,
                        ErrorMessage = "Assessment session not found"
                    };
                }

                if (session.TenantId != request.TenantId)
                {
                    return new CompleteAssessmentResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied"
                    };
                }

                var answers = await _personalityRepository.GetAnswersBySessionIdAsync(request.SessionId);
                var questions = await _personalityRepository.GetActiveQuestionsAsync();

                var result = await _scoringService.CalculatePersonalityTypeAsync(session, answers, questions);

                session.CompleteAssessment(result.PersonalityType, result.PreferenceStrengths);
                await _personalityRepository.UpdateSessionWithResultsAsync(session);

                await _mediator.Publish(new AssessmentCompletedEvent(session, result.PersonalityType, result.PreferenceStrengths), cancellationToken);

                _logger.LogInformation("Assessment completed with personality type");

                return new CompleteAssessmentResult
                {
                    Success = true,
                    PersonalityType = result.PersonalityType,
                    DimensionScores = result.PreferenceStrengths,
                    CompletedAt = session.CompletedAt ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete assessment");
                return new CompleteAssessmentResult
                {
                    Success = false,
                    ErrorMessage = "Failed to complete assessment"
                };
            }
        }
    }
}
