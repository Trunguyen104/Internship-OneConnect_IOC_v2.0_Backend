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
            .MaximumLength(200).WithMessage("Tên trường không được quá 200 ký tự");

        RuleFor(v => v.Address)
            .MaximumLength(500).WithMessage("Địa chỉ không được quá 500 ký tự");
    }
}
