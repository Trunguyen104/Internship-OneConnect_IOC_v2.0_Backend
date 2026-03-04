using FluentValidation;
using IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookValidator : AbstractValidator<CreateLogbookCommand>
    {
        public CreateLogbookValidator() 
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId is required.");

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

            // ACV-3: Optional Status — validate only when provided.
            RuleFor(x => x.Status)
                .Must(v => v == null || Enum.TryParse<LogbookStatus>(v, ignoreCase: true, out _))
                .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<LogbookStatus>())}");
        }
    }
}
