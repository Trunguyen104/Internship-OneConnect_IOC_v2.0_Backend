using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintValidator : AbstractValidator<CompleteSprintCommand>
{
    public CompleteSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.SprintId)
            .NotEmpty()
            .WithMessage(localizer["Sprint.ProjectIdRequired"]);
        
        RuleFor(x => x.IncompleteItemsOption)
            .IsInEnum()
            .WithMessage("Invalid incomplete items option");
    }
}
