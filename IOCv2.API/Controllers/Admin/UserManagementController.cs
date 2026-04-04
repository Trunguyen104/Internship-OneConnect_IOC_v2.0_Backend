using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.UserManagement.Commands.CreateUser;
using IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser;
using IOCv2.Application.Features.Admin.UserManagement.Commands.ResetUserPassword;
using IOCv2.Application.Features.Admin.UserManagement.Commands.ToggleUserStatus;
using IOCv2.Application.Features.Admin.UserManagement.Commands.UpdateUser;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Admin;

/// <summary>
/// User Management — manage accounts across the system with hierarchical permissions.
/// </summary>
[Tags("Admin - User Management")]
[Route("api/v{version:apiVersion}/user-management")]
[Authorize(Roles = "SuperAdmin,Moderator,SchoolAdmin,EnterpriseAdmin,HR")]
public class UserManagementController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UserManagementController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of users with hierarchical scoping.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetUsersResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single account by ID (role-based access).
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetUserById")]
    [Authorize(Roles = "SuperAdmin,Moderator,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<GetUserByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUserByIdQuery { UserId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new account (SuperAdmin creates all, SchoolAdmin creates Students, EnterpriseAdmin creates HR/Mentors).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetUserById), new { id = result.Data?.UserId, version = "1" });
    }

    /// <summary>
    /// Update an existing account with hierarchical validation.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Moderator,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { UserId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete an account with hierarchical validation.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DeleteUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteUserCommand { UserId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Change the status of an account.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin,Moderator,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ToggleUserStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleUserStatus(
        [FromRoute] Guid id,
        [FromBody] UserStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ToggleUserStatusCommand { UserId = id, NewStatus = newStatus }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reset the password of an account.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ResetUserPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(
        [FromRoute] Guid id,
        [FromBody] string reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ResetUserPasswordCommand { UserId = id, Reason = reason }, cancellationToken);
        return HandleResult(result);
    }
}
