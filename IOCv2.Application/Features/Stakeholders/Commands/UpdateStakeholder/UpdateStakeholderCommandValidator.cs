using FluentValidation;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;

public class UpdateStakeholderCommandValidator : AbstractValidator<UpdateStakeholderCommand>
{
    public UpdateStakeholderCommandValidator(IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage(localizer["Validation.IdRequired"]);

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage(localizer["Validation.NameMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(localizer["Validation.EmailInvalid"])
            .MaximumLength(150).WithMessage(localizer["Validation.EmailMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage(localizer["Validation.RoleMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Role));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage(localizer["Validation.DescriptionMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(15).WithMessage(localizer["Validation.PhoneNumberMaxLength"])
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

