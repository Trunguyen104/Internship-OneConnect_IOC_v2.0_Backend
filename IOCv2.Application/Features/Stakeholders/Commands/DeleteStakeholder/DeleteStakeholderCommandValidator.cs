using FluentValidation;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;

public class DeleteStakeholderCommandValidator : AbstractValidator<DeleteStakeholderCommand>
{
    public DeleteStakeholderCommandValidator(IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(localizer["Validation.IdRequired"]);
    }
}

