using FluentValidation;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Universities.Commands.CreateUniversity;

public class CreateUniversityValidator : AbstractValidator<CreateUniversityCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public CreateUniversityValidator(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;

        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Mã trường không được để trống")
            .MaximumLength(20).WithMessage("Mã trường không được quá 20 ký tự");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Tên trường không được để trống")
            .MaximumLength(255).WithMessage("Tên trường không được quá 255 ký tự");

        RuleFor(v => v.ContactEmail)
            .MaximumLength(255).WithMessage("Email liên hệ không được vượt quá 255 ký tự")
            .EmailAddress().WithMessage("Email liên hệ không đúng định dạng")
            .MustAsync(BeUniqueContactEmail)
                .WithMessage(_messageService.GetMessage(MessageKeys.University.ContactEmailAlreadyExists))
            .When(v => !string.IsNullOrEmpty(v.ContactEmail));

        RuleFor(v => v.Address)
            .MaximumLength(500).WithMessage("Địa chỉ không được quá 500 ký tự");
    }

    private async Task<bool> BeUniqueContactEmail(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email)) return true;

        var existsInUniversities = await _unitOfWork.Repository<University>()
            .ExistsAsync(u => u.ContactEmail == email, cancellationToken);
        
        if (existsInUniversities) return false;

        var existsInEnterprises = await _unitOfWork.Repository<Enterprise>()
            .ExistsAsync(e => e.ContactEmail == email, cancellationToken);

        return !existsInEnterprises;
    }
}
