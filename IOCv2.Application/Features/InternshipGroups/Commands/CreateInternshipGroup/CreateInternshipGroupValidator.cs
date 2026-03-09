using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    internal class CreateInternshipGroupValidator : AbstractValidator<CreateInternshipGroupCommand>
    {
        public CreateInternshipGroupValidator(IMessageService messageService)
        {
            RuleFor(v => v.GroupName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.NameRequired))
                .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.NameMaxLength));

            RuleFor(v => v.TermId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.TermIdRequired));

            RuleFor(v => v.StartDate)
                .LessThan(v => v.EndDate).When(v => v.StartDate.HasValue && v.EndDate.HasValue)
                .WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.StartDateBeforeEndDate));

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
