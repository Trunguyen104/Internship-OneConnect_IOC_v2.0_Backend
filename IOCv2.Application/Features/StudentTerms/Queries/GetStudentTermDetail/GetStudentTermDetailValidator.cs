using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public class GetStudentTermDetailValidator : AbstractValidator<GetStudentTermDetailQuery>
{
    public GetStudentTermDetailValidator()
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty().WithMessage("StudentTermId không được để trống");
    }
}
