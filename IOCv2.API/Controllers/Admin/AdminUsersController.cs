using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Models;

using IOCv2.Application.Features.Admin.Users.Commands.CreateAdminUser;
using IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser;
using IOCv2.Application.Features.Admin.Users.Commands.ResetUserPassword;
using IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus;
using IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUserById;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Admin;

/// <summary>
/// Admin &amp; Operations Management — manage administrative accounts across the system.
/// </summary>
[Tags("Admin - User Management")]
[Authorize]
public class AdminUsersController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of admin accounts with optional filters and sorting.
    /// </summary>
    /// <param name="query">Filter and pagination parameters.</param>
    /// <returns code="200">Returns the paginated list of users.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetAdminUsersResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminUsers(
        [FromQuery] GetAdminUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single admin account by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns code="200">Returns the user details.</returns>
    /// <returns code="404">User not found.</returns>
    [HttpGet("{id:guid}", Name = "GetAdminUserById")]
    [ProducesResponseType(typeof(ApiResponse<GetAdminUserByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdminUserById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAdminUserByIdQuery { UserId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new administrative account.
    /// Requires: SuperAdmin role.
    /// </summary>
    /// <param name="command">New account details.</param>
    /// <returns code="201">Returns the created account details.</returns>
    /// <returns code="400">Invalid data.</returns>
    /// <returns code="409">Email already exists.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateAdminUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAdminUser(
        [FromBody] CreateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetAdminUserById), new { id = result.Data?.UserId, version = "1" });
    }

    /// <summary>
    /// Update an existing administrative account.
    /// Requires: SuperAdmin role.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateAdminUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAdminUser(
        [FromRoute] Guid id,
        [FromBody] UpdateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var updateCommand = command with { UserId = id };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete an admin account.
    /// Requires: SuperAdmin role.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteAdminUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdminUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteAdminUserCommand { UserId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Change the status of an admin account (Active / Inactive / Suspended).
    /// Requires: SuperAdmin role.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ToggleUserStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(
        [FromRoute] Guid id,
        [FromBody] UserStatus newStatus,

        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ToggleUserStatusCommand { UserId = id, NewStatus = newStatus }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reset the password of an admin account to a system-generated secure password.
    /// Requires: SuperAdmin or Moderator role.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetUserPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        [FromRoute] Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ResetUserPasswordCommand { UserId = id, Reason = reason }, cancellationToken);
        return HandleResult(result);
    }
}
