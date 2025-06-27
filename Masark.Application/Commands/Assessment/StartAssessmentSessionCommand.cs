using MediatR;
using Masark.Domain.Entities;
using Masark.Domain.Enums;

namespace Masark.Application.Commands.Assessment
{
    public class StartAssessmentSessionCommand : IRequest<StartAssessmentSessionResult>
    {
        public string SessionToken { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string? LanguagePreference { get; set; } = "en";
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public DeploymentMode DeploymentMode { get; set; } = DeploymentMode.STANDARD;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
    }

    public class StartAssessmentSessionResult
    {
        public int SessionId { get; set; }
        public DateTime StartedAt { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
