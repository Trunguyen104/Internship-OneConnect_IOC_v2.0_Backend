using FluentValidation;
using IOCv2.Application.Resources;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public class DeleteSprintValidator : AbstractValidator<DeleteSprintCommand>
{
    public DeleteSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.SprintId)
            .NotEmpty()
            .WithMessage(localizer["Sprint.IdRequired"]);
    }
}
