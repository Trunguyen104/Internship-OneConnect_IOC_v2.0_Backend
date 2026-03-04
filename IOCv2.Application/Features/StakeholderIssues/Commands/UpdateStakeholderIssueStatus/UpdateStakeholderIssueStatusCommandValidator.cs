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
                .WithMessage(MessageKeys.Issue.InvalidStatus)
                .Must(BeValidStatus)
                .WithMessage(MessageKeys.Issue.InvalidStatus);
        }

        private bool BeValidStatus(string status)
        {
            return Enum.TryParse<StakeholderIssueStatus>(status, true, out _);
        }
    }
}

