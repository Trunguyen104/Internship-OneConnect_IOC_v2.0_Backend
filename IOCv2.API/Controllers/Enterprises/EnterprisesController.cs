using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Enterprises.Commands.AcceptApplication;
using IOCv2.Application.Features.Enterprises.Commands.AssignMentor;
using IOCv2.Application.Features.Enterprises.Commands.AssignProject;
using IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.RejectApplication;
using IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise;
using IOCv2.Application.Features.Enterprises.Queries.GetApplicationDetail;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseApplications;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseByHR;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterprises;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.Enterprises;

[Authorize]
[Tags("Enterprises")]
public class EnterprisesController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnterprisesController> _logger;

    public EnterprisesController(IMediator mediator, ILogger<EnterprisesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of enterprises based on filter criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetEnterprisesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEnterprises(
        [FromQuery] GetEnterprisesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves an enterprise by its unique identifier.
    /// </summary>
    [HttpGet("{enterpriseId:guid}", Name = "GetEnterpriseById")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnterpriseById(
        [FromRoute] Guid enterpriseId,
        CancellationToken cancellationToken)
    {
        var query = new GetEnterpriseByIdQuery { Id = enterpriseId };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new enterprise.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CreateEnterpriseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEnterprise(
        [FromBody] CreateEnterpriseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetEnterpriseById), new { enterpriseId = result.Data?.EnterpriseId, version = "1" });
    }

    /// <summary>
    /// Updates an existing enterprise.
    /// </summary>
    [HttpPut("{enterpriseId:guid}")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnterprise(
        [FromRoute] Guid enterpriseId,
        [FromBody] UpdateEnterpriseCommand command,
        CancellationToken cancellationToken)
    {
        var updateCommand = command with { EnterpriseId = enterpriseId };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes an enterprise by its unique identifier.
    /// </summary>
    [HttpDelete("{enterpriseId:guid}")]
    [Authorize(Roles = "EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DeleteEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEnterprise(
        [FromRoute] Guid enterpriseId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEnterpriseCommand { EnterpriseId = enterpriseId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves the enterprise associated with the currently authenticated HR user.
    /// </summary>
    [HttpGet("HR")]
    [Authorize(Roles = "HR")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByHRResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnterpriseByHR(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnterpriseByHRCommand(), cancellationToken);
        return HandleResult(result);
    }

    // ──────── Issue 77: Enterprise Application Management ────────

    /// <summary>
    /// Lấy danh sách đơn ứng tuyển thực tập của doanh nghiệp theo kỳ (phân trang).
    /// HR/EnterpriseAdmin xem tất cả. Mentor chỉ xem sinh viên trong nhóm của mình.
    /// </summary>
    [HttpGet("me/applications")]
    [Authorize(Roles = "HR,EnterpriseAdmin,Mentor")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetEnterpriseApplicationsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEnterpriseApplications(
        [FromQuery] GetEnterpriseApplicationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy chi tiết một đơn ứng tuyển thực tập.
    /// </summary>
    [HttpGet("me/applications/{applicationId:guid}")]
    [Authorize(Roles = "HR,EnterpriseAdmin,Mentor")]
    [ProducesResponseType(typeof(ApiResponse<GetApplicationDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationDetail(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationDetailQuery(applicationId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Chấp nhận đơn ứng tuyển của sinh viên (Pending → Approved).
    /// </summary>
    [HttpPatch("me/applications/{applicationId:guid}/accept")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<AcceptApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptApplication(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcceptApplicationCommand(applicationId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Từ chối đơn ứng tuyển của sinh viên (bắt buộc có lý do).
    /// </summary>
    [HttpPatch("me/applications/{applicationId:guid}/reject")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<RejectApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectApplication(
        [FromRoute] Guid applicationId,
        [FromBody] RejectApplicationCommand body,
        CancellationToken cancellationToken)
    {
        var command = body with { ApplicationId = applicationId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// [HR/EnterpriseAdmin] Gán Mentor cho sinh viên sau khi Accept đơn.
    /// Tự động tạo Group mới nếu Mentor chưa có nhóm trong kỳ này.
    /// </summary>
    [HttpPatch("me/applications/{applicationId:guid}/assign")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<AssignMentorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignMentor(
        [FromRoute] Guid applicationId,
        [FromBody] AssignMentorCommand body,
        CancellationToken cancellationToken)
    {
        var command = body with { ApplicationId = applicationId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// [Mentor] Gán Project vào nhóm của mình. Mentor chỉ được gán project
    /// cho sinh viên đã được HR assign vào nhóm của mình.
    /// </summary>
    [HttpPatch("me/applications/{applicationId:guid}/assign-project")]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<AssignProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignProject(
        [FromRoute] Guid applicationId,
        [FromBody] AssignProjectCommand body,
        CancellationToken cancellationToken)
    {
        var command = body with { ApplicationId = applicationId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
