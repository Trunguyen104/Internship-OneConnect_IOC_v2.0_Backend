using FluentValidation;

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    internal class UpdateLogbookValidator : AbstractValidator<UpdateLogbookCommand>
    {
        public UpdateLogbookValidator() 
        {
            RuleFor(x => x.InternshipId)
                .NotEmpty()
                .WithMessage("InternshipId is required.");

            RuleFor(x => x.StudentId)
                .NotEmpty()
                .WithMessage("StudentId is required.");

            RuleFor(x => x.Summary)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Issue)
                .MaximumLength(200);

            RuleFor(x => x.Plan)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.DateReport)
                .NotEmpty()
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("DateReport cannot be in the future.");
        }
    }
}
