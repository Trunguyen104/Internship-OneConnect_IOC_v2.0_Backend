using FluentValidation;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public class UpdateStudentTermValidator : AbstractValidator<UpdateStudentTermCommand>
{
    public UpdateStudentTermValidator()
    {
        RuleFor(x => x.FullName)
            .Matches(@"^[\p{L}\s]+$").WithMessage("Họ và tên chỉ chứa chữ cái và khoảng trắng")
            .When(x => !string.IsNullOrWhiteSpace(x.FullName));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^(\+84|0)[0-9]{9,10}$").WithMessage("Số điện thoại không hợp lệ")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Must(dob => !dob.HasValue || IsAtLeast15(dob.Value))
            .WithMessage("Sinh viên phải đủ 15 tuổi");

        RuleFor(x => x.EnterpriseId)
            .NotNull().WithMessage("Phải chọn doanh nghiệp khi xếp chỗ")
            .When(x => x.PlacementStatus == PlacementStatus.Placed);
    }

    private static bool IsAtLeast15(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.Year - dob.Year >= 15;
    }
}
