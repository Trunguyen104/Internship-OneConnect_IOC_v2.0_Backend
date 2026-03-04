using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

internal class StartSprintValidator : AbstractValidator<StartSprintCommand>
{
    public StartSprintValidator(IMessageService messageService)
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.StartDateRequired));

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.EndDateRequired))
            .Must((cmd, endDate) => endDate > cmd.StartDate)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.EndDateMustBeAfterStart));

        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber >= 7)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.DurationTooShort))
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 28)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.DurationTooLong));
    }
}
