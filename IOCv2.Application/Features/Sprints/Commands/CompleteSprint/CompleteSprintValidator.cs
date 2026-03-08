using FluentValidation;
using IOCv2.Application.Resources;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

internal class CompleteSprintValidator : AbstractValidator<CompleteSprintCommand>
{
    public CompleteSprintValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.IncompleteItemsOption)
            .IsInEnum()
            .WithMessage(localizer["Sprint.InvalidIncompleteItemsOption"]);

        // Khi tạo sprint mới, NewSprintName không được dài quá 200 ký tự
        RuleFor(x => x.NewSprintName)
            .MaximumLength(200)
            .WithMessage(localizer["Sprint.NameMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.NewSprintName));
    }
}
