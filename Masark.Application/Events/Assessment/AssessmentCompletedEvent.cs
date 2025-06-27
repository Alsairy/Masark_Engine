using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Events.Assessment
{
    public class AssessmentCompletedEvent : INotification
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public string PersonalityType { get; set; } = string.Empty;
        public Dictionary<string, double> DimensionScores { get; set; } = new();
        public DateTime CompletedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalAnswers { get; set; }

        public AssessmentCompletedEvent(AssessmentSession session, string personalityType, Dictionary<string, double> dimensionScores)
        {
            SessionId = session.Id;
            UserId = 0; // Will be set from student info when available
            TenantId = session.TenantId;
            PersonalityType = personalityType;
            DimensionScores = dimensionScores;
            CompletedAt = session.CompletedAt ?? DateTime.UtcNow;
            Duration = CompletedAt - session.StartedAt;
            TotalAnswers = session.Answers?.Count ?? 0;
        }
    }
}
