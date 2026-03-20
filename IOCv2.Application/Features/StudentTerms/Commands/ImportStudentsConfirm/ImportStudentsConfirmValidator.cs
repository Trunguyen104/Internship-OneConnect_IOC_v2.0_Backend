using FluentValidation;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsConfirm;

public class ImportStudentsConfirmValidator : AbstractValidator<ImportStudentsConfirmCommand>
{
    public ImportStudentsConfirmValidator()
    {
        RuleFor(x => x.TermId)
            .NotEmpty().WithMessage("TermId không được để trống");

        RuleFor(x => x.ValidRecords)
            .NotNull().WithMessage("Danh sách bản ghi không được null")
            .Must(r => r != null && r.Count > 0).WithMessage("Phải có ít nhất 1 bản ghi hợp lệ");

        RuleForEach(x => x.ValidRecords).ChildRules(record =>
        {
            record.RuleFor(r => r.StudentCode)
                .NotEmpty().WithMessage("Mã sinh viên không được để trống")
                .Matches(@"^[a-zA-Z0-9\-_\.]+$").WithMessage("Mã sinh viên không hợp lệ");

            record.RuleFor(r => r.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .Matches(@"^[\p{L}\s]+$").WithMessage("Họ và tên chỉ chứa chữ cái và khoảng trắng");

            record.RuleFor(r => r.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng");

            record.RuleFor(r => r.Phone)
                .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Số điện thoại không hợp lệ")
                .When(r => !string.IsNullOrWhiteSpace(r.Phone));
        });
    }
}
