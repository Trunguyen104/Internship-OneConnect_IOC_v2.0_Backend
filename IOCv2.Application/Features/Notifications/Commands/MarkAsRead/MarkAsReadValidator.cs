using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Notifications.Commands.MarkAsRead;

internal class MarkAsReadValidator : AbstractValidator<MarkAsReadCommand>
{
    public MarkAsReadValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty().WithMessage(MessageKeys.Notifications.NotificationIdRequired);
    }
}
