using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
{
    internal class ToggleUserStatusValidator : AbstractValidator<ToggleUserStatusCommand>
    {
        public ToggleUserStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .Must(status => Enum.TryParse<UserStatus>(status, true, out _))
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));
        }
    }
}
