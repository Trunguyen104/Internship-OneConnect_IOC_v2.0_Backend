using FluentValidation;
using IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    internal class UpdateLogbookValidator : AbstractValidator<CreateLogbookCommand>
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
