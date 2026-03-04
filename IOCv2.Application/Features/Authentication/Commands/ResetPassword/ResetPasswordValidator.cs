using FluentValidation;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Authentication.Commands.ResetPassword
{
    internal class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator(IMessageService messageService) 
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.TokenRequired));

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.NewPasswordRequired))
                .MinimumLength(8).WithMessage(messageService.GetMessage(MessageKeys.Password.MinLength))
                .Matches(@"[A-Z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireUppercase))
                .Matches(@"[a-z]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireLowercase))
                .Matches(@"[0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireDigit))
                .Matches(@"[^a-zA-Z0-9]").WithMessage(messageService.GetMessage(MessageKeys.Password.RequireSpecial));

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(messageService.GetMessage(MessageKeys.Auth.ConfirmPasswordRequired))
                .Equal(x => x.NewPassword).WithMessage(messageService.GetMessage(MessageKeys.Auth.ConfirmPasswordMismatch));
        }
    }
}
