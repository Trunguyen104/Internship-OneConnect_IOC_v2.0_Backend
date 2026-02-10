using FluentValidation;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;

public class CreateStakeholderIssueCommandValidator : AbstractValidator<CreateStakeholderIssueCommand>
{
    public CreateStakeholderIssueCommandValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(localizer["Issue.TitleRequired"])
            .MaximumLength(200).WithMessage(localizer["Validation.NameMaxLength"]);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(localizer["Issue.DescriptionRequired"])
            .MaximumLength(2000).WithMessage(localizer["Validation.DescriptionMaxLength"]);

        RuleFor(x => x.StakeholderId)
            .NotEmpty().WithMessage(localizer["Issue.StakeholderIdRequired"]);
    }
}
