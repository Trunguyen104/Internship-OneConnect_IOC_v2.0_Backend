using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplicationDetail;

internal class GetMyApplicationDetailValidator : AbstractValidator<GetMyApplicationDetailQuery>
{
    public GetMyApplicationDetailValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage(MessageKeys.Validation.IdRequired);
    }
}
