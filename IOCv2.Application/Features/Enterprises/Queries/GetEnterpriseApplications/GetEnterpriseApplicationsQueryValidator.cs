using FluentValidation;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseApplications;

public class GetEnterpriseApplicationsQueryValidator : AbstractValidator<GetEnterpriseApplicationsQuery>
{
    public GetEnterpriseApplicationsQueryValidator()
    {
        RuleFor(x => x.TermId).NotEmpty().WithMessage("TermId là bắt buộc.");
        RuleFor(x => x.PageIndex).GreaterThan(0).WithMessage("PageIndex phải lớn hơn 0.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize phải từ 1 đến 100.");
    }
}
