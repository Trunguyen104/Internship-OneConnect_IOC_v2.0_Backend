using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    internal class CreateStakeholderIssueCommandValidator : AbstractValidator<CreateStakeholderIssueCommand>
    {
        public CreateStakeholderIssueCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(MessageKeys.Issue.TitleRequired)
                .MaximumLength(200)
                .WithMessage(MessageKeys.Validation.NameMaxLength);

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage(MessageKeys.Issue.DescriptionRequired)
                .MaximumLength(2000)
                .WithMessage(MessageKeys.Validation.DescriptionMaxLength);

            RuleFor(x => x.StakeholderId)
                .NotEmpty()
                .WithMessage(MessageKeys.Issue.StakeholderIdRequired);
        }
    }
}

