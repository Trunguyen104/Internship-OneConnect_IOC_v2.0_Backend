using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.HRApplications.Common.Queries.GetApplicationDetail;
using IOCv2.Application.Features.HRApplications.SelfApply.Commands.MarkAsPlaced;
using IOCv2.Application.Features.HRApplications.SelfApply.Commands.MoveToInterviewing;
using IOCv2.Application.Features.HRApplications.SelfApply.Commands.RejectApplication;
using IOCv2.Application.Features.HRApplications.SelfApply.Commands.SendOffer;
using IOCv2.Application.Features.HRApplications.SelfApply.Queries.GetSelfApplyApplications;
using IOCv2.Application.Features.HRApplications.UniAssign.Commands.ApproveUniAssign;
using IOCv2.Application.Features.HRApplications.UniAssign.Commands.RejectUniAssign;
using IOCv2.Application.Features.HRApplications.UniAssign.Queries.GetUniAssignApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Applications;

/// <summary>
/// Quản lý đơn ứng tuyển dành cho HR & Enterprise Admin.
/// </summary>
[Tags("HR Applications")]
[Authorize(Roles = "HR, EnterpriseAdmin")]
public class ApplicationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lấy danh sách đơn ứng tuyển tự do (Self-apply)
    /// </summary>
    [HttpGet("self-apply")]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetSelfApplyApplicationsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSelfApplyApplications([FromQuery] GetSelfApplyApplicationsQuery query)
        => HandleResult(await _mediator.Send(query));

    /// <summary>
    /// Lấy danh sách đơn được trường chỉ định (Uni Assign)
    /// </summary>
    [HttpGet("uni-assign")]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetUniAssignApplicationsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniAssignApplications([FromQuery] GetUniAssignApplicationsQuery query)
        => HandleResult(await _mediator.Send(query));

    /// <summary>
    /// Lấy chi tiết đơn ứng tuyển và lịch sử thay đổi (Timeline)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Result<GetApplicationDetailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplicationDetail(Guid id)
        => HandleResult(await _mediator.Send(new GetApplicationDetailQuery(id)));

    // ── Self-Apply Flow Transitions ───────────────────────────────────────

    /// <summary>
    /// Chuyển trạng thái đơn Self-apply sang "Đang phỏng vấn"
    /// </summary>
    [HttpPatch("{id}/move-to-interviewing")]
    [ProducesResponseType(typeof(Result<MoveToInterviewingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveToInterviewing(Guid id)
        => HandleResult(await _mediator.Send(new MoveToInterviewingCommand(id)));

    /// <summary>
    /// Chuyển trạng thái đơn Self-apply sang "Đã gửi Offer"
    /// </summary>
    [HttpPatch("{id}/send-offer")]
    [ProducesResponseType(typeof(Result<SendOfferResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOffer(Guid id)
        => HandleResult(await _mediator.Send(new SendOfferCommand(id)));

    /// <summary>
    /// Chuyển trạng thái đơn Self-apply sang "Đã nhận (Placed)"
    /// </summary>
    [HttpPatch("{id}/mark-as-placed")]
    [ProducesResponseType(typeof(Result<MarkAsPlacedResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsPlaced(Guid id)
        => HandleResult(await _mediator.Send(new MarkAsPlacedCommand(id)));

    /// <summary>
    /// Từ chối đơn ứng tuyển Self-apply (Yêu cầu lý do)
    /// </summary>
    [HttpPatch("{id}/reject")]
    [ProducesResponseType(typeof(Result<RejectApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectApplication(Guid id, [FromBody] RejectApplicationRequest body)
        => HandleResult(await _mediator.Send(new RejectApplicationCommand { ApplicationId = id, RejectReason = body.RejectReason ?? string.Empty }));

    // ── Uni Assign Flow Transitions ───────────────────────────────────────

    /// <summary>
    /// Phê duyệt sinh viên được trường chỉ định (PendingAssignment -> Placed). Sẽ tự động cascade withdraw các đơn ứng tuyển active khác.
    /// </summary>
    [HttpPatch("{id}/approve-uni-assign")]
    [ProducesResponseType(typeof(Result<ApproveUniAssignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveUniAssign(Guid id)
        => HandleResult(await _mediator.Send(new ApproveUniAssignCommand(id)));

    /// <summary>
    /// Từ chối sinh viên được trường chỉ định (Yêu cầu lý do)
    /// </summary>
    [HttpPatch("{id}/reject-uni-assign")]
    [ProducesResponseType(typeof(Result<RejectUniAssignResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectUniAssign(Guid id, [FromBody] RejectApplicationRequest body)
        => HandleResult(await _mediator.Send(new RejectUniAssignCommand { ApplicationId = id, RejectReason = body.RejectReason ?? string.Empty }));
}

public class RejectApplicationRequest
{
    public string? RejectReason { get; set; }
}
