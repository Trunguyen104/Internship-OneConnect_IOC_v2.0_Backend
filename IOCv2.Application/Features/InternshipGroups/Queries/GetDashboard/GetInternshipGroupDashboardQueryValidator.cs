using FluentValidation;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;

/// <summary>
/// Validator for GetInternshipGroupDashboardQuery.
/// </summary>
public class GetInternshipGroupDashboardQueryValidator : AbstractValidator<GetInternshipGroupDashboardQuery>
{
    public GetInternshipGroupDashboardQueryValidator()
    {
        RuleFor(x => x.InternshipId)
            .NotEmpty().WithMessage("InternshipId is required.");
    }
}
