using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.InternshipGroups;

/// <summary>
/// Internship Groups Management — manage internship groups and their student members.
/// </summary>
[Route("api/internshipgroups")]
[Tags("Internship Groups Management")]
[Authorize]
public class InternshipGroupsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InternshipGroupsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of internship groups with optional search and filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetInternshipGroupsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInternshipGroups(
        [FromQuery] GetInternshipGroupsQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Get details of a single internship group by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<GetInternshipGroupByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInternshipGroupById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetInternshipGroupByIdQuery(id), cancellationToken));
    }

    /// <summary>
    /// Create a new internship group.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateInternshipGroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateInternshipGroup(
        [FromBody] CreateInternshipGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleCreatedResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Update an existing internship group.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<UpdateInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInternshipGroup(
        [FromRoute] Guid id,
        [FromBody] UpdateInternshipGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        var updateCommand = command with { InternshipId = id };
        return HandleResult(await _mediator.Send(updateCommand, cancellationToken));
    }

    /// <summary>
    /// Delete an internship group and its associated student list.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<DeleteInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInternshipGroup(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new DeleteInternshipGroupCommand(id), cancellationToken));
    }

    /// <summary>
    /// Add a list of students to an internship group.
    /// </summary>
    [HttpPost("{id:guid}/students")]
    [ProducesResponseType(typeof(Result<AddStudentsToGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStudentsToGroup(
        [FromRoute] Guid id,
        [FromBody] AddStudentsToGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        var updateCommand = command with { InternshipId = id };
        return HandleResult(await _mediator.Send(updateCommand, cancellationToken));
    }

    /// <summary>
    /// Remove students from an internship group.
    /// </summary>
    [HttpDelete("{id:guid}/students")]
    [ProducesResponseType(typeof(Result<RemoveStudentsFromGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudentsFromGroup(
        [FromRoute] Guid id,
        [FromBody] RemoveStudentsFromGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        var updateCommand = command with { InternshipId = id };
        return HandleResult(await _mediator.Send(updateCommand, cancellationToken));
    }
}
