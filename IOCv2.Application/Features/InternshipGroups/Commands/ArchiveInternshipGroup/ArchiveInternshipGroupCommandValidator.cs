using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    internal class ArchiveInternshipGroupCommandValidator : AbstractValidator<ArchiveInternshipGroupCommand>
    {
        public ArchiveInternshipGroupCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.InternshipGroupId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Internships.InternshipIdRequired));
        }
    }
}
