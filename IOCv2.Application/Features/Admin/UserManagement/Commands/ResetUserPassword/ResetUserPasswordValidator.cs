using FluentValidation;
using IOCv2.Application.Interfaces;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ResetUserPassword
{
    internal class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
    {
        public ResetUserPasswordValidator(IMessageService messageService)
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(messageService.GetMessage("ResetPassword.UserIdRequired"));

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage(messageService.GetMessage("ResetPassword.ReasonRequired"))
                .MinimumLength(10)
                .WithMessage(messageService.GetMessage("ResetPassword.ReasonMinLength"))
                .MaximumLength(500)
                .WithMessage(messageService.GetMessage("ResetPassword.ReasonMaxLength"));

            When(x => !string.IsNullOrEmpty(x.NewPassword), () =>
            {
                RuleFor(x => x.NewPassword)
                    .MinimumLength(8)
                    .WithMessage(messageService.GetMessage("Password.MinLength"))
                    .Matches(@"[A-Z]")
                    .WithMessage(messageService.GetMessage("Password.RequireUppercase"))
                    .Matches(@"[a-z]")
                    .WithMessage(messageService.GetMessage("Password.RequireLowercase"))
                    .Matches(@"[0-9]")
                    .WithMessage(messageService.GetMessage("Password.RequireDigit"))
                    .Matches(@"[\W_]")
                    .WithMessage(messageService.GetMessage("Password.RequireSpecial"));
            });
        }
    }
}
