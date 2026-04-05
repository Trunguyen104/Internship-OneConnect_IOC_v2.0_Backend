using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Universities.Commands.UpdateUniversity;

public class UpdateUniversityValidator : AbstractValidator<UpdateUniversityCommand>
{
    private readonly IMessageService _messageService;

    public UpdateUniversityValidator(IMessageService messageService)
    {
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
            .When(v => !string.IsNullOrEmpty(v.ContactEmail));

        RuleFor(v => v.Address)
            .MaximumLength(500);

        RuleFor(v => v.Status)
            .IsInEnum().WithMessage(_messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));
    }
}
