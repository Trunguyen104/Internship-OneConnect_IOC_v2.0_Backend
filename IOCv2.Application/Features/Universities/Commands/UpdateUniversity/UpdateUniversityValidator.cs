using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Universities.Commands.UpdateUniversity;

public class UpdateUniversityValidator : AbstractValidator<UpdateUniversityCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public UpdateUniversityValidator(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;

        RuleFor(v => v.UniversityId).NotEmpty();

        RuleFor(v => v.Code)
            .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.UserManagement.CODE_REQ))
            .MaximumLength(20).WithMessage(_messageService.GetMessage(MessageKeys.UserManagement.CODE_MAX_LEN));

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameRequired))
            .MaximumLength(255).WithMessage(_messageService.GetMessage(MessageKeys.Profile.FullNameMaxLength));

        RuleFor(v => v.ContactEmail)
            .MaximumLength(255).WithMessage(_messageService.GetMessage(MessageKeys.Profile.EmailInvalid))
            .EmailAddress().WithMessage(_messageService.GetMessage(MessageKeys.Profile.EmailInvalid))
            .MustAsync(BeUniqueContactEmail)
                .WithMessage(_messageService.GetMessage(MessageKeys.University.ContactEmailAlreadyExists))
            .When(v => !string.IsNullOrEmpty(v.ContactEmail));

        RuleFor(v => v.Address)
            .MaximumLength(500);

        RuleFor(v => v.Status)
            .IsInEnum().WithMessage(_messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));
    }

    private async Task<bool> BeUniqueContactEmail(UpdateUniversityCommand request, string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email)) return true;

        var existsInUniversities = await _unitOfWork.Repository<Domain.Entities.University>()
            .ExistsAsync(u => u.ContactEmail == email && u.UniversityId != request.UniversityId, cancellationToken);
        
        if (existsInUniversities) return false;

        var existsInEnterprises = await _unitOfWork.Repository<Domain.Entities.Enterprise>()
            .ExistsAsync(e => e.ContactEmail == email, cancellationToken);

        return !existsInEnterprises;
    }
}
