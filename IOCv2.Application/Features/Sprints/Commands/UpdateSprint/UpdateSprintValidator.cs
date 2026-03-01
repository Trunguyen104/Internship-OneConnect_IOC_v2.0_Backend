using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator(IMessageService messageService)
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

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.StartDateRequired));

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.EndDateRequired))
            .Must((cmd, endDate) => !cmd.StartDate.HasValue || !endDate.HasValue || endDate.Value > cmd.StartDate.Value)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.EndDateMustBeAfterStart));

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue ||
                       x.EndDate.Value.DayNumber - x.StartDate.Value.DayNumber >= 7)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.DurationTooShort))
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue ||
                       x.EndDate.Value.DayNumber - x.StartDate.Value.DayNumber <= 28)
            .WithMessage(messageService.GetMessage(MessageKeys.Sprint.DurationTooLong));
    }
}
