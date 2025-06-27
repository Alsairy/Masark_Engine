using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Queries.Assessment;
using Masark.Application.Interfaces;

namespace Masark.Application.Handlers.Queries
{
    public class GetQuestionsHandler : IRequestHandler<GetQuestionsQuery, GetQuestionsResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ILogger<GetQuestionsHandler> _logger;

        public GetQuestionsHandler(
            IPersonalityRepository personalityRepository,
            ILogger<GetQuestionsHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _logger = logger;
        }

        public async Task<GetQuestionsResult> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var questions = await _personalityRepository.GetQuestionsOrderedAsync();

                if (request.ActiveOnly)
                {
                    questions = questions.Where(q => q.IsActive).ToList();
                }

                _logger.LogInformation("Retrieved {Count} questions for tenant {TenantId}", questions.Count, request.TenantId);

                return new GetQuestionsResult
                {
                    Questions = questions,
                    TotalCount = questions.Count,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve questions for tenant {TenantId}", request.TenantId);
                return new GetQuestionsResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve questions"
                };
            }
        }
    }
}
