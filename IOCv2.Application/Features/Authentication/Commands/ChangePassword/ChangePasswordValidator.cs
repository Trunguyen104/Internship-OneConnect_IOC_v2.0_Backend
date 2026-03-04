using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Authentication.Commands.ChangePassword
{
    internal class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator(Interfaces.IMessageService messageService)
        {
            RuleFor(v => v.CurrentPassword)
                .NotEmpty()
                .WithMessage(messageService.GetMessage(MessageKeys.Password.IncorrectCurrent));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Password.NotEmpty))
                .MinimumLength(8).WithMessage(messageService.GetMessage(MessageKeys.Password.MinLength))
                .Matches(@"[A-Z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireUppercase))
                .Matches(@"[a-z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireLowercase))
                .Matches(@"[0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireDigit))
                .Matches(@"[\W_]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireSpecial))
                .NotEqual(x => x.CurrentPassword).WithMessage(messageService.GetMessage(MessageKeys.Password.MustBeDifferent));

            RuleFor(v => v.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage(messageService.GetMessage(MessageKeys.Password.ConfirmationMismatch));
        }
    }
}
