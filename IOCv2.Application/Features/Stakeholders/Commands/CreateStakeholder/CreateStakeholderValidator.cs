using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    public class CreateStakeholderValidator : AbstractValidator<CreateStakeholderCommand>
    {
        public CreateStakeholderValidator()
        {
            RuleFor(x => x.InternshipId)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.ProjectIdRequired);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.NameRequired)
                .MaximumLength(200)
                .WithMessage(MessageKeys.Stakeholder.NameMaxLength);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.EmailRequired)
                .EmailAddress()
                .WithMessage(MessageKeys.Stakeholder.EmailInvalid)
                .MaximumLength(150)
                .WithMessage(MessageKeys.Stakeholder.EmailMaxLength);

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
