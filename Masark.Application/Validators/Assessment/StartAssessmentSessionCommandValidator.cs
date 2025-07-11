using FluentValidation;
using Masark.Application.Commands.Assessment;

namespace Masark.Application.Validators.Assessment
{
    public class StartAssessmentSessionCommandValidator : AbstractValidator<StartAssessmentSessionCommand>
    {
        public StartAssessmentSessionCommandValidator()
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("Tenant ID is required")
                .Must(x => x > 0)
                .WithMessage("Tenant ID must be a valid positive integer");

            RuleFor(x => x.LanguagePreference)
                .Must(BeValidLanguageCode)
                .WithMessage("Language must be a valid language code (en, ar, es, etc.)");

            RuleFor(x => x.StudentName)
                .NotEmpty()
                .WithMessage("Student name is required")
                .MaximumLength(100)
                .WithMessage("Student name must not exceed 100 characters");

            RuleFor(x => x.StudentEmail)
                .NotEmpty()
                .WithMessage("Student email is required")
                .EmailAddress()
                .WithMessage("Student email must be a valid email address");

            RuleFor(x => x.StudentId)
                .NotEmpty()
                .WithMessage("Student ID is required")
                .MaximumLength(50)
                .WithMessage("Student ID must not exceed 50 characters");

            RuleFor(x => x.SessionToken)
                .NotEmpty()
                .WithMessage("Session token is required");
        }

        private bool BeValidLanguageCode(string? language)
        {
            if (string.IsNullOrEmpty(language))
                return true;

            var validLanguages = new[] { "en", "ar", "es", "zh", "fr", "de" };
            return validLanguages.Contains(language.ToLower());
        }
    }
}
