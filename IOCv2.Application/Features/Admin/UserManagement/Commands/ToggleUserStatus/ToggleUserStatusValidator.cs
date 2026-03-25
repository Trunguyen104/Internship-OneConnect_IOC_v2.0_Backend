using FluentValidation;
using IOCv2.Domain.Enums;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ToggleUserStatus
{
    internal class ToggleUserStatusValidator : AbstractValidator<ToggleUserStatusCommand>
    {
        public ToggleUserStatusValidator(IMessageService messageService)
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage(messageService.GetMessage(MessageKeys.Validation.UserInvalidStatus));

        }
    }
}
