using MediatR;
using Microsoft.Extensions.Logging;
using Masark.Application.Commands.Assessment;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;

namespace Masark.Application.Handlers.Commands
{
    public class SubmitAnswerHandler : IRequestHandler<SubmitAnswerCommand, SubmitAnswerResult>
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ILogger<SubmitAnswerHandler> _logger;

        public SubmitAnswerHandler(
            IPersonalityRepository personalityRepository,
            ILogger<SubmitAnswerHandler> logger)
        {
            _personalityRepository = personalityRepository;
            _logger = logger;
        }

        public async Task<SubmitAnswerResult> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(request.SessionId);
                if (session == null)
                {
                    return new SubmitAnswerResult
                    {
                        Success = false,
                        ErrorMessage = "Assessment session not found"
                    };
                }

                if (session.TenantId != request.TenantId)
                {
                    return new SubmitAnswerResult
                    {
                        Success = false,
                        ErrorMessage = "Access denied"
                    };
                }

                var questions = await _personalityRepository.GetActiveQuestionsAsync();
                if (!questions.ContainsKey(request.QuestionId))
                {
                    return new SubmitAnswerResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid question ID"
                    };
                }

                var answer = new AssessmentAnswer(
                    request.SessionId,
                    request.QuestionId,
                    request.SelectedOption,
                    request.TenantId
                );

                session.AddAnswer(answer);
                await _personalityRepository.UpdateSessionWithResultsAsync(session);

                var totalAnswers = session.Answers?.Count ?? 0;
                var totalQuestions = questions.Count;
                var isComplete = totalAnswers >= totalQuestions;

                _logger.LogInformation("Answer submitted for session {SessionId}, question {QuestionId}", 
                    request.SessionId, request.QuestionId);

                return new SubmitAnswerResult
                {
                    Success = true,
                    TotalAnswered = totalAnswers,
                    TotalQuestions = totalQuestions,
                    IsComplete = isComplete
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit answer for session {SessionId}", request.SessionId);
                return new SubmitAnswerResult
                {
                    Success = false,
                    ErrorMessage = "Failed to submit answer"
                };
            }
        }
    }
}
