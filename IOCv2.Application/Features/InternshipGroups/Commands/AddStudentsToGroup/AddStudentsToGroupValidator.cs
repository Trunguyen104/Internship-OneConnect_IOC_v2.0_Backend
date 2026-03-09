using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    internal class AddStudentsToGroupValidator : AbstractValidator<AddStudentsToGroupCommand>
    {
        public AddStudentsToGroupValidator(IMessageService messageService)
        {
            RuleFor(v => v.Students).NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.StudentListRequired));

            // ACV-3: Validate Enum string input for each student's Role.
            RuleForEach(v => v.Students)
                .ChildRules(student =>
                {
                    student.RuleFor(s => s.Role)
                        .IsInEnum().WithMessage("Invalid student role.");

                });
        }
    }
}
