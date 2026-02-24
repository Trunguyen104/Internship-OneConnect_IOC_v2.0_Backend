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
    }
}
