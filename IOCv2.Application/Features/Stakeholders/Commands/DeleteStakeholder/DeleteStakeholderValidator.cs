using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    internal class DeleteStakeholderValidator : AbstractValidator<DeleteStakeholderCommand>
    {
        public DeleteStakeholderValidator()
        {
            RuleFor(x => x.StakeholderId)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.IdRequired);

            RuleFor(x => x.InternshipId)
                .NotEmpty()
                .WithMessage(MessageKeys.Stakeholder.InternshipIdRequired);
        }
    }
}

