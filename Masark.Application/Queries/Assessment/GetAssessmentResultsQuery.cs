using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Queries.Assessment
{
    public class GetAssessmentResultsQuery : IRequest<GetAssessmentResultsResult>
    {
        public int SessionId { get; set; }
        public int TenantId { get; set; }
        public bool IncludeCareerMatches { get; set; } = true;
        public bool IncludeStatistics { get; set; } = false;
    }

    public class GetAssessmentResultsResult
    {
        public AssessmentSession? Session { get; set; }
        public string? PersonalityType { get; set; }
        public Dictionary<string, double>? DimensionScores { get; set; }
        public List<PersonalityCareerMatch>? CareerMatches { get; set; }
        public Dictionary<string, object>? Statistics { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
