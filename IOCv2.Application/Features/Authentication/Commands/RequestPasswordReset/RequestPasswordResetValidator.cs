using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Authentication.Commands.RequestPasswordReset
{
    public class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
    {
        public RequestPasswordResetValidator(IMessageService messageService)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.EmailRequired))
                .EmailAddress().WithMessage(messageService.GetMessage(MessageKeys.Auth.EmailInvalidFormat));
        }
    }
}
