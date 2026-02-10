using FluentValidation;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;

public class CreateStakeholderCommandValidator : AbstractValidator<CreateStakeholderCommand>
{
    public CreateStakeholderCommandValidator(IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage(localizer["Validation.ProjectIdRequired"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localizer["Validation.NameRequired"])
            .MaximumLength(200).WithMessage(localizer["Validation.NameMaxLength"]);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(localizer["Validation.EmailRequired"])
            .EmailAddress().WithMessage(localizer["Validation.EmailInvalid"])
            .MaximumLength(150).WithMessage(localizer["Validation.EmailMaxLength"]);

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

