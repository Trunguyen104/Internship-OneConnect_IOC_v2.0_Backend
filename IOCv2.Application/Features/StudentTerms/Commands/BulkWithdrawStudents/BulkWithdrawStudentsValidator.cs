using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public class BulkWithdrawStudentsValidator : AbstractValidator<BulkWithdrawStudentsCommand>
{
    public BulkWithdrawStudentsValidator()
    {
        RuleFor(x => x.StudentTermIds)
            .NotEmpty().WithMessage("Danh sách ID không được để trống")
            .Must(ids => ids.Count > 0).WithMessage("Phải chọn ít nhất 1 sinh viên");
    }
}
