using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    internal class UpdateInternshipGroupValidator : AbstractValidator<UpdateInternshipGroupCommand>
    {
        public UpdateInternshipGroupValidator(IMessageService messageService)
        {
            RuleFor(v => v.GroupName)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.NameRequired))
                .MaximumLength(255).WithMessage(messageService.GetMessage(MessageKeys.InternshipGroups.NameMaxLength));

            RuleFor(v => v.PhaseId)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipPhase.PhaseIdRequired));


        }
    }
}
