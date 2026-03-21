using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups
{
    internal class MoveStudentsBetweenGroupsCommandValidator : AbstractValidator<MoveStudentsBetweenGroupsCommand>
    {
        public MoveStudentsBetweenGroupsCommandValidator()
        {
            RuleFor(x => x.FromGroupId)
                .NotEmpty().WithMessage(MessageKeys.Internships.InternshipIdRequired);

            RuleFor(x => x.ToGroupId)
                .NotEmpty().WithMessage(MessageKeys.Internships.InternshipIdRequired);

            RuleFor(x => x.StudentIds)
                .NotEmpty().WithMessage(MessageKeys.InternshipGroups.StudentListRequired);
        }
    }
}
