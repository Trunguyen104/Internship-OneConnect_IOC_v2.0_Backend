using MediatR;

namespace IOCv2.Application.Features.Notifications.Events;

/// <summary>
/// Domain Event phát sinh khi doanh nghiệp chấp nhận đơn xin thực tập của sinh viên.
/// Publish qua: await _mediator.Publish(new ApplicationAcceptedEvent(...))
/// trong Handler của AcceptInternshipApplication.
/// </summary>
public record ApplicationAcceptedEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;
