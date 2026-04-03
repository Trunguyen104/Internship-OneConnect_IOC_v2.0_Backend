﻿using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Stakeholders;

/// <summary>
/// Stakeholder Management — manage stakeholders for a project.
/// </summary>
[Tags("Stakeholder Management")]
[Authorize]
public class StakeholdersController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StakeholdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of stakeholders for an internship group with optional search and sorting.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Student,Mentor,SchoolAdmin,SuperAdmin,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetStakeholdersResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStakeholders(
        [FromQuery] Guid internshipId,
        [FromQuery] GetStakeholdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var queryWithInternship = query with { InternshipId = internshipId };
        var result = await _mediator.Send(queryWithInternship, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single stakeholder by ID.
    /// </summary>
    /// <param name="stakeholderId">Stakeholder ID.</param>
    /// <param name="internshipId">Internship ID.</param>
    /// <returns code="200">Returns the stakeholder details.</returns>
    [HttpGet("{stakeholderId:guid}", Name = "GetStakeholderById")]
    [Authorize(Roles = "Student,Mentor,SchoolAdmin,SuperAdmin,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<GetStakeholderByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStakeholderById(
        [FromRoute] Guid stakeholderId,
        [FromQuery] Guid internshipId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetStakeholderByIdQuery { InternshipId = internshipId, StakeholderId = stakeholderId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new stakeholder for a project.
    /// </summary>
    /// <param name="command">Stakeholder data.</param>
    /// <returns code="201">Returns the created stakeholder.</returns>
    [HttpPost]
    [Authorize(Roles = "Student,SchoolAdmin,SuperAdmin,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<CreateStakeholderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStakeholder(
        [FromBody] CreateStakeholderCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetStakeholderById), new { stakeholderId = result.Data?.Id, internshipId = command.InternshipId, version = "1" });
    }

    /// <summary>
    /// Update an existing stakeholder. All fields are optional (partial update).
    /// </summary>
    [HttpPut("{stakeholderId:guid}")]
    [Authorize(Roles = "Student,SchoolAdmin,SuperAdmin,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<UpdateStakeholderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStakeholder(
        [FromRoute] Guid stakeholderId,
        [FromBody] UpdateStakeholderCommand command,
        CancellationToken cancellationToken = default)
    {
        var updateCommand = command with { StakeholderId = stakeholderId };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete a stakeholder by ID.
    /// </summary>
    [HttpDelete("{stakeholderId:guid}")]
    [Authorize(Roles = "Student,SchoolAdmin,SuperAdmin,Moderator")]
    [ProducesResponseType(typeof(ApiResponse<DeleteStakeholderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStakeholder(
        [FromRoute] Guid stakeholderId,
        [FromBody] DeleteStakeholderCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { StakeholderId = stakeholderId }, cancellationToken);
        return HandleResult(result);
    }
}
