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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.InternshipGroups;

/// <summary>
/// Internship Groups Management — manage internship groups and their student members.
/// </summary>
[Tags("Internship Groups Management")]
[Authorize]
public class InternshipGroupsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InternshipGroupsController> _logger;

    public InternshipGroupsController(IMediator mediator, ILogger<InternshipGroupsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of internship groups with optional search and filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetInternshipGroupsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInternshipGroups(
        [FromQuery] GetInternshipGroupsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get paginated internship groups with query: {@Query}", query);
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Get details of a single internship group by ID.
    /// </summary>
    /// <param name="id">Internship group ID.</param>
    /// <returns code="200">Returns the group details.</returns>
    /// <returns code="404">Group not found.</returns>
    [HttpGet("{id:guid}", Name = "GetInternshipGroupById")]
    [ProducesResponseType(typeof(ApiResponse<GetInternshipGroupByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInternshipGroupById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get internship group by ID: {Id}", id);
        return HandleResult(await _mediator.Send(new GetInternshipGroupByIdQuery(id), cancellationToken));
    }

    /// <summary>
    /// Create a new internship group.
    /// </summary>
    /// <param name="command">Group creation data.</param>
    /// <returns code="201">Returns the created group details.</returns>
    /// <returns code="400">Invalid data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateInternshipGroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInternshipGroup(
        [FromBody] CreateInternshipGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to create a new internship group: {GroupName}", command.GroupName);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetInternshipGroupById), new { id = result.Data?.InternshipId, version = "1" });
    }

    /// <summary>
    /// Update an existing internship group.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInternshipGroup(
        [FromRoute] Guid id,
        [FromBody] UpdateInternshipGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to update internship group: {Id}", id);
        var updateCommand = command with { InternshipId = id };
        return HandleResult(await _mediator.Send(updateCommand, cancellationToken));
    }

    /// <summary>
    /// Delete an internship group and its associated student list.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInternshipGroup(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to delete internship group: {Id}", id);
        return HandleResult(await _mediator.Send(new DeleteInternshipGroupCommand { InternshipId = id }, cancellationToken));
    }

    /// <summary>
    /// Add a list of students to an internship group.
    /// </summary>
    [HttpPost("students")]
    [ProducesResponseType(typeof(ApiResponse<AddStudentsToGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStudentsToGroup(
        [FromBody] AddStudentsToGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to add {Count} students to internship group {Id}", command.Students.Count, command.InternshipId);
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Remove students from an internship group.
    /// </summary>
    [HttpDelete("students")]
    [ProducesResponseType(typeof(ApiResponse<RemoveStudentsFromGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudentsFromGroup(
        [FromBody] RemoveStudentsFromGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to remove {Count} students from internship group {Id}", command.StudentIds.Count, command.InternshipId);
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }
}
