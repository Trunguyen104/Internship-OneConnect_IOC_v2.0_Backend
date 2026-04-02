using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AssignMentorToGroup;

public class AssignMentorToGroupValidator : AbstractValidator<AssignMentorToGroupCommand>
{
    public AssignMentorToGroupValidator()
    {
        RuleFor(x => x.InternshipGroupId)
            .NotEmpty()
            .WithMessage(MessageKeys.InternshipGroups.AssignMentorGroupIdRequired);

        RuleFor(x => x.MentorUserId)
            .NotEmpty()
            .WithMessage(MessageKeys.InternshipGroups.AssignMentorMentorRequired);
    }
}
