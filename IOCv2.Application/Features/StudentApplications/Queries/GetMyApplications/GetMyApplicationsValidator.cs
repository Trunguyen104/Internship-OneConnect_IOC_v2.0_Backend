using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplications;

internal class GetMyApplicationsValidator : AbstractValidator<GetMyApplicationsQuery>
{
    public GetMyApplicationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage(MessageKeys.Common.PageNumberInvalid);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage(MessageKeys.Common.PageSizeInvalid);

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || Enum.TryParse<InternshipApplicationStatus>(status, true, out _))
            .WithMessage(MessageKeys.Common.InvalidRequest);
    }
}
