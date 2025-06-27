using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Queries.Assessment;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;

namespace Masark.Application.Handlers.Queries
{
    public class GetCareerMatchesHandler : IRequestHandler<GetCareerMatchesQuery, GetCareerMatchesResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ILogger<GetCareerMatchesHandler> _logger;

        public GetCareerMatchesHandler(
            IPersonalityRepository personalityRepository,
            ILogger<GetCareerMatchesHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _logger = logger;
        }

        public async Task<GetCareerMatchesResult> Handle(GetCareerMatchesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var personalityType = await _personalityRepository.GetPersonalityTypeByCodeAsync(request.PersonalityType);
                if (personalityType == null)
                {
                    return new GetCareerMatchesResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid personality type"
                    };
                }

                var matches = new List<PersonalityCareerMatch>();

                _logger.LogInformation("Retrieved {Count} career matches for personality type {PersonalityType}", 
                    matches.Count, request.PersonalityType);

                return new GetCareerMatchesResult
                {
                    Matches = matches,
                    TotalCount = matches.Count,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve career matches for personality type {PersonalityType}", request.PersonalityType);
                return new GetCareerMatchesResult
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve career matches"
                };
            }
        }
    }
}
