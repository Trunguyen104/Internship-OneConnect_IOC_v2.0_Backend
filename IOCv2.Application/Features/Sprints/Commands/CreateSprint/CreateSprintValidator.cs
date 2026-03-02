using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator(IMessageService messageService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.NameRequired))
            .MaximumLength(200)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.NameMaxLength));

        RuleFor(x => x.Goal)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Goal))
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.GoalMaxLength));
    }
}
