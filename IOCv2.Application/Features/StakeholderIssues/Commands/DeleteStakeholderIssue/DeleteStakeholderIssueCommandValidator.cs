using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue
{
    internal class DeleteStakeholderIssueCommandValidator : AbstractValidator<DeleteStakeholderIssueCommand>
    {
        public DeleteStakeholderIssueCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(MessageKeys.Validation.IdRequired);
        }
    }
}

