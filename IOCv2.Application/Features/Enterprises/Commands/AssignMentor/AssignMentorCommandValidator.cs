using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignMentor;

public class AssignMentorCommandValidator : AbstractValidator<AssignMentorCommand>
{
    public AssignMentorCommandValidator(IMessageService messageService)
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.ApplicationIdRequired));

        RuleFor(x => x.MentorEnterpriseUserId)
            .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.InternshipApplication.MentorIdRequired));
    }
}
