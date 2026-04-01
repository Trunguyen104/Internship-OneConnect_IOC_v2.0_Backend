using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentApplications.Commands.HideApplication;
using IOCv2.Application.Features.StudentApplications.Commands.WithdrawApplication;
using IOCv2.Application.Features.StudentApplications.Queries.GetMyApplicationDetail;
using IOCv2.Application.Features.StudentApplications.Queries.GetMyApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Applications;

/// <summary>
/// Quản lý đơn ứng tuyển dành cho Sinh viên (My Applications).
/// </summary>
[Tags("Student Applications")]
[Authorize(Roles = "Student")]
public class StudentApplicationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentApplicationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lấy danh sách đơn ứng tuyển của sinh viên hiện tại (My Applications — AC-01).
    /// Mặc định chỉ trả về các đơn đang active. Truyền <c>includeTerminal=true</c> để xem cả Placed/Rejected/Withdrawn.
    /// </summary>
    [HttpGet("my-applications")]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetMyApplicationsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApplications([FromQuery] GetMyApplicationsQuery query)
        => HandleResult(await _mediator.Send(query));

    /// <summary>
    /// Lấy chi tiết một đơn ứng tuyển của sinh viên hiện tại, bao gồm lịch sử trạng thái.
    /// </summary>
    [HttpGet("my-applications/{id}")]
    [ProducesResponseType(typeof(Result<GetMyApplicationDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApplicationDetail(Guid id)
        => HandleResult(await _mediator.Send(new GetMyApplicationDetailQuery(id)));

    /// <summary>
    /// Sinh viên rút đơn ứng tuyển đang ở trạng thái Applied (AC-02). Không cần lý do.
    /// </summary>
    [HttpPatch("my-applications/{id}/withdraw")]
    [ProducesResponseType(typeof(Result<WithdrawApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> WithdrawApplication(Guid id)
        => HandleResult(await _mediator.Send(new WithdrawApplicationCommand(id)));

    /// <summary>
    /// Sinh viên ẩn đơn đã kết thúc (Rejected / Withdrawn) khỏi danh sách My Applications (AC-04).
    /// Dữ liệu vẫn còn trong DB để HR và Uni Admin audit.
    /// </summary>
    [HttpPatch("my-applications/{id}/hide")]
    [ProducesResponseType(typeof(Result<HideApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> HideApplication(Guid id)
        => HandleResult(await _mediator.Send(new HideApplicationCommand(id)));
}
