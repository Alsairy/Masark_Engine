using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Events.Assessment
{
    public class AssessmentSessionStartedEvent : INotification
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public DateTime StartedAt { get; set; }
        public string? Language { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }

        public AssessmentSessionStartedEvent(AssessmentSession session)
        {
            SessionId = session.Id;
            UserId = 0; // Will be set from student info when available
            TenantId = session.TenantId;
            StartedAt = session.StartedAt;
            Language = session.LanguagePreference;
            UserAgent = session.UserAgent;
            IpAddress = session.IpAddress;
        }
    }
}
