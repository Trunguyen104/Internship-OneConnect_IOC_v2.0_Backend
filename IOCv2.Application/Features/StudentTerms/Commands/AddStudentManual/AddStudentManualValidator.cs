using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public class AddStudentManualValidator : AbstractValidator<AddStudentManualCommand>
{
    public AddStudentManualValidator(IMessageService messageService)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống")
            .Matches(@"^[\p{L}\s]+$").WithMessage("Họ và tên chỉ chứa chữ cái và khoảng trắng");

        RuleFor(x => x.StudentCode)
            .NotEmpty().WithMessage("Mã sinh viên không được để trống")
            .Matches(@"^[a-zA-Z0-9\-_\.]+$").WithMessage("Mã sinh viên không hợp lệ");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Phone)
            .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Must(dob => !dob.HasValue || IsAtLeast15(dob.Value))
            .WithMessage("Sinh viên phải đủ 15 tuổi");
    }

    private static bool IsAtLeast15(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.Year - dob.Year >= 15;
    }
}
