using FluentValidation;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus;

public class UpdateStakeholderIssueStatusCommandValidator : AbstractValidator<UpdateStakeholderIssueStatusCommand>
{
    public UpdateStakeholderIssueStatusCommandValidator(IStringLocalizer<ErrorMessages> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(localizer["Validation.IdRequired"]);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage(localizer["Issue.InvalidStatus"]);
    }
}
