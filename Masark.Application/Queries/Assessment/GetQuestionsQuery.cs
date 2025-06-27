using MediatR;
using Masark.Domain.Entities;

namespace Masark.Application.Queries.Assessment
{
    public class GetQuestionsQuery : IRequest<GetQuestionsResult>
    {
        public int TenantId { get; set; }
        public string? Language { get; set; } = "en";
        public bool ActiveOnly { get; set; } = true;
    }

    public class GetQuestionsResult
    {
        public List<Question> Questions { get; set; } = new();
        public int TotalCount { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
