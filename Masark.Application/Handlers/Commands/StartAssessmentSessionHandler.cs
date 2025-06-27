using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Commands.Assessment;
using Masark.Application.Events.Assessment;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;

namespace Masark.Application.Handlers.Commands
{
    public class StartAssessmentSessionHandler : IRequestHandler<StartAssessmentSessionCommand, StartAssessmentSessionResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly IMediator _mediator;
        private readonly ILogger<StartAssessmentSessionHandler> _logger;

        public StartAssessmentSessionHandler(
            IPersonalityRepository personalityRepository,
            IMediator mediator,
            ILogger<StartAssessmentSessionHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<StartAssessmentSessionResult> Handle(StartAssessmentSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var session = new AssessmentSession(
                    request.SessionToken,
                    request.DeploymentMode,
                    request.LanguagePreference ?? "en",
                    request.IpAddress ?? "",
                    request.UserAgent ?? "",
                    request.TenantId
                );

                await _personalityRepository.UpdateSessionWithResultsAsync(session);

                await _mediator.Publish(new AssessmentSessionStartedEvent(session), cancellationToken);

                _logger.LogInformation("Assessment session {SessionId} started with token {SessionToken}", session.Id, request.SessionToken);

                return new StartAssessmentSessionResult
                {
                    SessionId = session.Id,
                    StartedAt = session.StartedAt,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start assessment session for token {SessionToken}", request.SessionToken);
                return new StartAssessmentSessionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start assessment session"
                };
            }
        }
    }
}
