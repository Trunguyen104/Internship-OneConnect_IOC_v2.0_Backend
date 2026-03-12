using FluentValidation;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentEvaluationCycles;

public class GetStudentEvaluationCyclesValidator : AbstractValidator<GetStudentEvaluationCyclesQuery>
{
    public GetStudentEvaluationCyclesValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.InternshipId).NotEmpty();
    }
}
