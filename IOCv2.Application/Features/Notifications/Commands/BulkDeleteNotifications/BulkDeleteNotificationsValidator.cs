using FluentValidation;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Notifications.Commands.BulkDeleteNotifications;

public class BulkDeleteNotificationsValidator : AbstractValidator<BulkDeleteNotificationsCommand>
{
    public BulkDeleteNotificationsValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage(MessageKeys.Notifications.BulkDeleteEmptyIds);
    }
}
