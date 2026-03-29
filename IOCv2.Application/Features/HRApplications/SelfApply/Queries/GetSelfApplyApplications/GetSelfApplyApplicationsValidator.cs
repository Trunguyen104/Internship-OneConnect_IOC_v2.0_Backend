using IOCv2.Application.Constants;
using FluentValidation;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Queries.GetSelfApplyApplications;

internal class GetSelfApplyApplicationsValidator : AbstractValidator<GetSelfApplyApplicationsQuery>
{
    public GetSelfApplyApplicationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage(MessageKeys.Page.PageNumberMinValue);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage(MessageKeys.Page.PageSizeMinValue)
            .LessThanOrEqualTo(100).WithMessage(MessageKeys.Page.PageSizeMaxValue);

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm != null);

        RuleFor(x => x.MonthYear)
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("MonthYear phải có dạng yyyy-MM")
            .When(x => !string.IsNullOrEmpty(x.MonthYear));
    }
}
