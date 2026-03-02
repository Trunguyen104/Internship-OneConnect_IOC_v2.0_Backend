using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupValidator : AbstractValidator<AddStudentsToGroupCommand>
    {
        public AddStudentsToGroupValidator(IMessageService messageService)
        {
            RuleFor(v => v.Students).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.StudentListRequired));
        }
    }
}
