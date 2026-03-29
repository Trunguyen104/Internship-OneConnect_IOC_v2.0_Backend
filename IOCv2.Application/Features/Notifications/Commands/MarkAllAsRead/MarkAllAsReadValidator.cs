using FluentValidation;

namespace IOCv2.Application.Features.Notifications.Commands.MarkAllAsRead;

internal class MarkAllAsReadValidator : AbstractValidator<MarkAllAsReadCommand>
{
    public MarkAllAsReadValidator()
    {
        // No parameters to validate
    }
}
