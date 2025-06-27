using MediatR;
using Masark.Domain.Enums;

namespace Masark.Application.Commands.Assessment
{
    public class SubmitAnswerCommand : IRequest<SubmitAnswerResult>
    {
        public int SessionId { get; set; }
        public int QuestionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class SubmitAnswerResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalAnswered { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsComplete { get; set; }
    }
}
