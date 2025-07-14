using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Events.Assessment;
using Masark.Application.Queries.Assessment;

namespace Masark.Application.Handlers.Events
{
    public class AssessmentCompletedEventHandler : INotificationHandler<AssessmentCompletedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AssessmentCompletedEventHandler> _logger;

        public AssessmentCompletedEventHandler(
            IMediator mediator,
            ILogger<AssessmentCompletedEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(AssessmentCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing assessment completion for session");

                var careerMatchQuery = new GetCareerMatchesQuery
                {
                    PersonalityType = notification.PersonalityType,
                    TenantId = notification.TenantId,
                    Limit = 10,
                    MinMatchScore = 0.6
                };

                var careerMatches = await _mediator.Send(careerMatchQuery, cancellationToken);

                if (careerMatches.Success && careerMatches.Matches.Any())
                {
                    var careerMatchingEvent = new CareerMatchingCompletedEvent(
                        notification.SessionId,
                        notification.UserId,
                        notification.TenantId,
                        notification.PersonalityType,
                        careerMatches.Matches
                    );

                    await _mediator.Publish(careerMatchingEvent, cancellationToken);
                }

                _logger.LogInformation("Assessment completion processing finished for session");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process assessment completion for session");
            }
        }
    }
}
