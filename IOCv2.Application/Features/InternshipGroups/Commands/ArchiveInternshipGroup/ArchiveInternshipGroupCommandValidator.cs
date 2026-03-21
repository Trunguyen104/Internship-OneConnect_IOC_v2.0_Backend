using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    internal class ArchiveInternshipGroupCommandValidator : AbstractValidator<ArchiveInternshipGroupCommand>
    {
        public ArchiveInternshipGroupCommandValidator()
        {
            RuleFor(x => x.InternshipGroupId)
                .NotEmpty().WithMessage(MessageKeys.Internships.InternshipIdRequired);
        }
    }
}
