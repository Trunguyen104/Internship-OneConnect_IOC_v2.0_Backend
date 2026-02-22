using FluentValidation;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
{
    internal class ToggleUserStatusValidator : AbstractValidator<ToggleUserStatusCommand>
    {
        public ToggleUserStatusValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .Must(status => Enum.TryParse<UserStatus>(status, true, out _))
                .WithMessage("Invalid status value.");
        }
    }
}
