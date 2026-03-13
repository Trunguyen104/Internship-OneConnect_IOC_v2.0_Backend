using FluentValidation;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentTeamEvaluations;

public class GetStudentTeamEvaluationsValidator : AbstractValidator<GetStudentTeamEvaluationsQuery>
{
    public GetStudentTeamEvaluationsValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.CycleId).NotEmpty();
    }
}
