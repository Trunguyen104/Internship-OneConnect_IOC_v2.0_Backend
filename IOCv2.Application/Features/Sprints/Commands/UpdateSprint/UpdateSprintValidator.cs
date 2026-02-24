using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.SprintId)
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
        
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage(localizer["Sprint.StartDateRequired"]);
        
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage(localizer["Sprint.EndDateRequired"])
            .GreaterThan(x => x.StartDate)
            .WithMessage(localizer["Sprint.EndDateMustBeAfterStart"]);
        
        // Sprint duration validation (1-4 weeks)
        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays >= 7)
            .WithMessage(localizer["Sprint.DurationTooShort"])
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= 28)
            .WithMessage(localizer["Sprint.DurationTooLong"]);
    }
}
