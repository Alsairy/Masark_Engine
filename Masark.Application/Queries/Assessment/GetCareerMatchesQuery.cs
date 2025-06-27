using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Queries.Assessment
{
    public class GetCareerMatchesQuery : IRequest<GetCareerMatchesResult>
    {
        public string PersonalityType { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public int? Limit { get; set; } = 10;
        public double? MinMatchScore { get; set; } = 0.6;
        public string? Language { get; set; } = "en";
    }

    public class GetCareerMatchesResult
    {
        public List<PersonalityCareerMatch> Matches { get; set; } = new();
        public int TotalCount { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
