using FluentValidation;
using IOCv2.Application.Resources;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintValidator : AbstractValidator<CompleteSprintCommand>
{
    private static readonly string[] ValidOptions =
    [
        nameof(MoveIncompleteItemsOption.ToBacklog),
        nameof(MoveIncompleteItemsOption.ToNextPlannedSprint),
        nameof(MoveIncompleteItemsOption.CreateNewSprint)
    ];

    public CompleteSprintValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.SprintId)
            .NotEmpty()
            .WithMessage(localizer["Sprint.IdRequired"]);

        // Validate string option
        RuleFor(x => x.IncompleteItemsOption)
            .NotEmpty()
            .WithMessage(localizer["Sprint.InvalidIncompleteItemsOption"])
            .Must(opt => ValidOptions.Contains(opt, StringComparer.OrdinalIgnoreCase))
            .WithMessage(localizer["Sprint.InvalidIncompleteItemsOption"]);

        // Khi tạo sprint mới, NewSprintName không được dài quá 200 ký tự
        RuleFor(x => x.NewSprintName)
            .MaximumLength(200)
            .WithMessage(localizer["Sprint.NameMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.NewSprintName));
    }
}
