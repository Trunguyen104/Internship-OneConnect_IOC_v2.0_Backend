using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintValidator : AbstractValidator<StartSprintCommand>
{
    public StartSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.SprintId)
            .NotEmpty()
            .WithMessage(localizer["Sprint.ProjectIdRequired"]);

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(localizer["Sprint.StartDateRequired"]);

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(localizer["Sprint.EndDateRequired"])
            .Must((cmd, endDate) => endDate > cmd.StartDate)
            .WithMessage(localizer["Sprint.EndDateMustBeAfterStart"]);

        // Sprint duration: 7-28 ngày
        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber >= 7)
            .WithMessage(localizer["Sprint.DurationTooShort"])
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 28)
            .WithMessage(localizer["Sprint.DurationTooLong"]);
    }
}
