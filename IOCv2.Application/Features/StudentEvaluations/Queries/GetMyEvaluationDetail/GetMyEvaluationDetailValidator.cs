using FluentValidation;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetMyEvaluationDetail;

public class GetMyEvaluationDetailValidator : AbstractValidator<GetMyEvaluationDetailQuery>
{
    public GetMyEvaluationDetailValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.CycleId).NotEmpty();
    }
}
