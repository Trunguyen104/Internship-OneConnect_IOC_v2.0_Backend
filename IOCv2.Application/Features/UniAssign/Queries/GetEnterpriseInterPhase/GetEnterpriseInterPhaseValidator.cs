using FluentValidation;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public class GetEnterpriseInterPhaseValidator : AbstractValidator<GetEnterpriseInterPhaseQuery>
    {
        public GetEnterpriseInterPhaseValidator()
        {
            // SearchTerm is optional. When provided, trim and require length between 3 and 300.
            RuleFor(x => x.SearchTerm)
                .Must(s => string.IsNullOrWhiteSpace(s) || (s.Trim().Length >= 3 && s.Trim().Length <= 300))
                .WithMessage("SearchTerm must be between 3 and 300 characters when provided.");
        }
    }
}
