using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;

public class GetAvailableMentorsValidator : AbstractValidator<GetAvailableMentorsQuery>
{
    public GetAvailableMentorsValidator()
    {
        RuleFor(x => x.InternshipGroupId)
            .NotEmpty()
            .WithMessage(MessageKeys.InternshipGroups.AssignMentorGroupIdRequired);
    }
}
