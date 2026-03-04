using FluentValidation;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    internal class GetInternshipGroupByIdValidator : AbstractValidator<GetInternshipGroupByIdQuery>
    {
        public GetInternshipGroupByIdValidator()
        {
            RuleFor(x => x.InternshipId)
                .NotEmpty().WithMessage("InternshipId must not be empty.");
        }
    }
}
