using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;

public class RestoreStudentValidator : AbstractValidator<RestoreStudentCommand>
{
    public RestoreStudentValidator()
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty().WithMessage("StudentTermId không được để trống");
    }
}
