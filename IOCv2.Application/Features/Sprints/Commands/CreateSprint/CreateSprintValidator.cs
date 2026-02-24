using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage(localizer["Sprint.ProjectIdRequired"]);
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localizer["Sprint.NameRequired"])
            .MaximumLength(200)
            .WithMessage(localizer["Sprint.NameMaxLength"]);
        
        RuleFor(x => x.Goal)
            .MaximumLength(1000)
            .WithMessage(localizer["Sprint.GoalMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Goal));
    }
}
