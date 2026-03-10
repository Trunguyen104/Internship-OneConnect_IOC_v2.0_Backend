using FluentValidation;

namespace IOCv2.Application.Features.Students.Queries.GetInternshipDetail
{
    public class GetInternshipDetailQueryValidator : AbstractValidator<GetInternshipDetailQuery>
    {
        public GetInternshipDetailQueryValidator()
        {
            RuleFor(v => v.TermId)
                .NotEmpty().WithMessage("TermId is required.");
        }
    }
}
