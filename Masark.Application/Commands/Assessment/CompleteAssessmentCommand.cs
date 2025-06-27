using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Commands.Assessment
{
    public class CompleteAssessmentCommand : IRequest<CompleteAssessmentResult>
    {
        public int SessionId { get; set; }
        public int TenantId { get; set; }
        public bool ForceComplete { get; set; } = false;
    }

    public class CompleteAssessmentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? PersonalityType { get; set; }
        public Dictionary<string, double>? DimensionScores { get; set; }
        public List<PersonalityCareerMatch>? CareerMatches { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
