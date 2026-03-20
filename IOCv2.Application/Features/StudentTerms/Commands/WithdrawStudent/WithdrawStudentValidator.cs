using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public class WithdrawStudentValidator : AbstractValidator<WithdrawStudentCommand>
{
    public WithdrawStudentValidator()
    {
        RuleFor(x => x.StudentTermId)
            .NotEmpty().WithMessage("StudentTermId không được để trống");
    }
}
