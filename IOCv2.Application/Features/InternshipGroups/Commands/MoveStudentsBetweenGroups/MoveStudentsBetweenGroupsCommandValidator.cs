using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups
{
    internal class MoveStudentsBetweenGroupsCommandValidator : AbstractValidator<MoveStudentsBetweenGroupsCommand>
    {
        public MoveStudentsBetweenGroupsCommandValidator(IMessageService messageService)
        {
            RuleFor(x => x.FromGroupId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Internships.InternshipIdRequired));

            RuleFor(x => x.ToGroupId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Internships.InternshipIdRequired));

            RuleFor(x => x.StudentIds)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.StudentListRequired));
        }
    }
}
