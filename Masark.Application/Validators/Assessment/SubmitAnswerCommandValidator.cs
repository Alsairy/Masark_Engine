using FluentValidation;
using Masark.Application.Commands.Assessment;

namespace Masark.Application.Validators.Assessment
{
    public class SubmitAnswerCommandValidator : AbstractValidator<SubmitAnswerCommand>
    {
        public SubmitAnswerCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("Session ID is required")
                .Must(x => x > 0)
                .WithMessage("Session ID must be a valid positive integer");

            RuleFor(x => x.QuestionId)
                .NotEmpty()
                .WithMessage("Question ID is required")
                .Must(x => x > 0)
                .WithMessage("Question ID must be a valid positive integer");

            RuleFor(x => x.SelectedOption)
                .NotEmpty()
                .WithMessage("Selected option is required")
                .Must(BeValidSelectedOption)
                .WithMessage("Selected option must be valid");

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("Tenant ID is required")
                .Must(x => x > 0)
                .WithMessage("Tenant ID must be a valid positive integer");

            RuleFor(x => x.SubmittedAt)
                .NotEmpty()
                .WithMessage("Submitted at timestamp is required")
                .Must(BeValidTimestamp)
                .WithMessage("Submitted at must be a valid timestamp");
        }

        private bool BeValidSelectedOption(string selectedOption)
        {
            return !string.IsNullOrWhiteSpace(selectedOption) && selectedOption.Length <= 100;
        }

        private bool BeValidTimestamp(DateTime submittedAt)
        {
            return submittedAt <= DateTime.UtcNow && submittedAt >= DateTime.UtcNow.AddDays(-1);
        }
    }
}
