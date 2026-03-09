using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    internal class UpdateStakeholderIssueStatusCommandValidator : AbstractValidator<UpdateStakeholderIssueStatusCommand>
    {
        public UpdateStakeholderIssueStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(MessageKeys.Validation.IdRequired);

            RuleFor(x => x.Status)
                .NotEmpty()
                .IsInEnum()
                .WithMessage(MessageKeys.Issue.InvalidStatus);
        }
    }
}

