using FluentValidation;

namespace IOCv2.Application.Features.Notifications.Queries.GetUnreadCount;

internal class GetUnreadCountValidator : AbstractValidator<GetUnreadCountQuery>
{
    public GetUnreadCountValidator()
    {
        // No parameters to validate — query is always valid for authenticated users
    }
}
