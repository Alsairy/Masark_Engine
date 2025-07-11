using FluentValidation;
using Masark.Application.Commands.Assessment;

namespace Masark.Application.Validators.Assessment
{
    public class CompleteAssessmentCommandValidator : AbstractValidator<CompleteAssessmentCommand>
    {
        public CompleteAssessmentCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .WithMessage("Session ID is required")
                .Must(x => x > 0)
                .WithMessage("Session ID must be a valid positive integer");

            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("Tenant ID is required")
                .Must(x => x > 0)
                .WithMessage("Tenant ID must be a valid positive integer");

            RuleFor(x => x.ForceComplete)
                .NotNull()
                .WithMessage("Force complete flag must be specified");
        }
    }
}
