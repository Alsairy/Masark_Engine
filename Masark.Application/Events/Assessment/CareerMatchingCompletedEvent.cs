using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Events.Assessment
{
    public class CareerMatchingCompletedEvent : INotification
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public string PersonalityType { get; set; } = string.Empty;
        public List<PersonalityCareerMatch> CareerMatches { get; set; } = new();
        public DateTime MatchedAt { get; set; }
        public int TotalMatches { get; set; }
        public double HighestMatchScore { get; set; }

        public CareerMatchingCompletedEvent(int sessionId, int userId, int tenantId, string personalityType, List<PersonalityCareerMatch> matches)
        {
            SessionId = sessionId;
            UserId = userId;
            TenantId = tenantId;
            PersonalityType = personalityType;
            CareerMatches = matches;
            MatchedAt = DateTime.UtcNow;
            TotalMatches = matches.Count;
            HighestMatchScore = matches.Any() ? (double)matches.Max(m => m.MatchScore) : 0.0;
        }
    }
}
