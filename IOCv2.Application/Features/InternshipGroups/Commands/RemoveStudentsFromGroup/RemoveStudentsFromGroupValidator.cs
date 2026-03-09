using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    internal class RemoveStudentsFromGroupValidator : AbstractValidator<RemoveStudentsFromGroupCommand>
    {
        public RemoveStudentsFromGroupValidator(IMessageService messageService)
        {
            RuleFor(v => v.StudentIds).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.StudentListToRemoveRequired));
        }
    }
}
