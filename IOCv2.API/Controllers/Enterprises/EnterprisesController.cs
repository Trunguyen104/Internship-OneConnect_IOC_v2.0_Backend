using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Enterprises.Commands.AcceptApplication;
using IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.RejectApplication;
using IOCv2.Application.Features.Enterprises.Commands.RestoreEnterprise;
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
    /// <param name="query">
    /// The query parameters used for filtering, sorting, and pagination.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the request.
    /// </param>
    /// <returns>
    /// Returns:
    /// - 200 OK if the request is successful.
    /// - 400 Bad Request if the query parameters are invalid.
    /// - 401 Unauthorized if the user is not authenticated.
    /// - 500 Internal Server Error for unexpected failures.
    /// </returns>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetEnterprisesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
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
    /// <param name="command">
    /// The command containing enterprise creation data.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the request.
    /// </param>
    /// <returns>
    /// Returns:
    /// - 201 Created if the enterprise is successfully created.
    /// - 409 Conflict if the enterprise already exists.
    /// - 500 Internal Server Error for unexpected failures.
    /// - 403 Forbidden if the user does not have the required role.
    /// </returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByIdResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEnterprise(
        [FromBody] CreateEnterpriseCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing enterprise.
    /// </summary>
    /// <param name="enterpriseId">
    /// The unique identifier of the enterprise to update.
    /// </param>
    /// <param name="body">
    /// The command containing updated enterprise data.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the request.
    /// </param>
    /// <returns>
    /// Returns:
    /// - 200 OK if the update is successful.
    /// - 400 Bad Request if the request is invalid.
    /// - 403 Forbidden if the user does not have permission.
    /// - 404 Not Found if the enterprise does not exist.
    /// - 500 Internal Server Error for unexpected failures.
    /// </returns>
    [HttpPut("{enterpriseId}")]
    [Authorize(Roles = "SuperAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEnterprise([FromRoute] Guid enterpriseId, [FromBody] UpdateEnterpriseCommand body, CancellationToken cancellationToken)
    {
        body.EnterpriseId = enterpriseId;
        var result = await _mediator.Send(body, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes an enterprise by its unique identifier.
    /// Only users with the EnterpriseAdmin role are authorized to perform this action.
    /// </summary>
    /// <param name="enterpriseId">The unique identifier of the enterprise to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// 200 OK: The enterprise was successfully deleted.
    /// 404 NotFound: The specified enterprise was not found.
    /// 500 InternalServerError: An unexpected server error occurred.
    /// </returns>
    [HttpDelete("{enterpriseId}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DeleteEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEnterprise([FromRoute] Guid enterpriseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEnterpriseCommand { EnterpriseId = enterpriseId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves the enterprise associated with the currently authenticated HR user.
    /// Only users with the HR role are authorized to access this endpoint.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// 200 OK: Returns the enterprise information.
    /// 401 Unauthorized: The user is not authenticated.
    /// 404 NotFound: No enterprise was found for the current HR user.
    /// 429 TooManyRequests: Too many requests were sent in a given amount of time.
    /// 500 InternalServerError: An unexpected server error occurred.
    /// </returns>
    [HttpGet("mine")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<GetEnterpriseByMineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnterpriseByMine(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEnterpriseByMineCommand(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Restores a previously deleted enterprise.
    /// </summary>
    /// <param name="enterpriseId">The unique identifier of the enterprise to restore.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// Returns the restore result containing enterprise information if successful.
    /// Possible responses include Forbidden, NotFound, or InternalServerError.
    /// </returns>
    [HttpPut("{enterpriseId}/restore")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<RestoreEnterpriseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreEnterprise([FromRoute] Guid enterpriseId, CancellationToken cancellationToken)
    {
        var command = new RestoreEnterpriseCommand { EnterpriseId = enterpriseId };
        var result = await _mediator.Send(command, cancellationToken);
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
}
