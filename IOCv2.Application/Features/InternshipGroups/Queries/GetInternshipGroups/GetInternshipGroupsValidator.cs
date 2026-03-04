using FluentValidation;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    internal class GetInternshipGroupsValidator : AbstractValidator<GetInternshipGroupsQuery>
    {
        public GetInternshipGroupsValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PageSize should be between 1 and 100.");
        }
    }
}
