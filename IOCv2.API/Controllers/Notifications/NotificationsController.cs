using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Notifications.Commands.MarkAllAsRead;
using IOCv2.Application.Features.Notifications.Commands.MarkAsRead;
using IOCv2.Application.Features.Notifications.Queries.GetNotifications;
using IOCv2.Application.Features.Notifications.Queries.GetUnreadCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Notifications;

/// <summary>
/// Quản lý thông báo cá nhân của người dùng.
/// </summary>
[Tags("Notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (phân trang, sort CreatedAt DESC).
    /// </summary>
    [HttpGet]
    [Route("notifications")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetNotificationsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] GetNotificationsQuery query)
    {
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc của người dùng hiện tại.
    /// </summary>
    [HttpGet]
    [Route("notifications/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<GetUnreadCountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery());
        return HandleResult(result);
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// </summary>
    [HttpPatch]
    [Route("notifications/{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<MarkAsReadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new MarkAsReadCommand(id));
        return HandleResult(result);
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo chưa đọc là đã đọc.
    /// </summary>
    [HttpPatch]
    [Route("notifications/read-all")]
    [ProducesResponseType(typeof(ApiResponse<MarkAllAsReadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var result = await _mediator.Send(new MarkAllAsReadCommand());
        return HandleResult(result);
    }
}
