using FluentValidation;

namespace IOCv2.Application.Features.Universities.Commands.CreateUniversity;

public class CreateUniversityValidator : AbstractValidator<CreateUniversityCommand>
{
    public CreateUniversityValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Mã trường không được để trống")
            .MaximumLength(20).WithMessage("Mã trường không được quá 20 ký tự");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên trường không được để trống")
            .MaximumLength(255).WithMessage("Tên trường không được quá 255 ký tự");

        RuleFor(v => v.ContactEmail)
            .MaximumLength(255).WithMessage("Email liên hệ không được vượt quá 255 ký tự")
            .EmailAddress().WithMessage("Email liên hệ không đúng định dạng")
            .When(v => !string.IsNullOrEmpty(v.ContactEmail));

        RuleFor(v => v.Address)
            .MaximumLength(500).WithMessage("Địa chỉ không được quá 500 ký tự");
    }
}
