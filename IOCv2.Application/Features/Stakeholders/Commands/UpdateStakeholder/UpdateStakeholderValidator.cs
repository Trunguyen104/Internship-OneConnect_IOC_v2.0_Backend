using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder
{
    internal class UpdateStakeholderValidator : AbstractValidator<UpdateStakeholderCommand>
    {
        public UpdateStakeholderValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.IdRequired);

            RuleFor(x => x.Name)
                .MaximumLength(200)
                .WithMessage(MessageKeys.Stakeholder.NameMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Name));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage(MessageKeys.Stakeholder.EmailInvalid)
                .MaximumLength(150)
                .WithMessage(MessageKeys.Stakeholder.EmailMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.Role)
                .MaximumLength(100)
                .WithMessage(MessageKeys.Stakeholder.RoleMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Role));

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage(MessageKeys.Stakeholder.DescriptionMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,9}$")
                .WithMessage(MessageKeys.Stakeholder.PhoneNumberInvalid)
                .MaximumLength(15)
                .WithMessage(MessageKeys.Stakeholder.PhoneNumberMaxLength)
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        }
    }
}

