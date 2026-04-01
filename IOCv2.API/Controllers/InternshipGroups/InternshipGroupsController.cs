using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups;
using IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;
using IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipTerms;
using IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents;
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
[Route("api/v{version:apiVersion}/internship-groups")]
[Authorize] // Yêu cầu đăng nhập cho tất cả endpoint
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
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
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
    /// Get paginated list of archived internship groups with optional search and filter.
    /// </summary>
    [HttpGet("archived")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetInternshipGroupsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetArchivedInternshipGroups(
        [FromQuery] GetInternshipGroupsQuery query,
        CancellationToken cancellationToken = default)
    {
        var archivedQuery = query with 
        { 
            Status = IOCv2.Domain.Enums.GroupStatus.Archived, 
            IncludeArchived = true 
        };
        _logger.LogInformation("Request to get archived internship groups with query: {@Query}", archivedQuery);
        return HandleResult(await _mediator.Send(archivedQuery, cancellationToken));
    }

    /// <summary>
    /// Get details of a single internship group by ID.
    /// </summary>
    /// <param name="id">Internship group ID.</param>
    /// <returns code="200">Returns the group details.</returns>
    /// <returns code="404">Group not found.</returns>
    [HttpGet("{id:guid}", Name = "GetInternshipGroupById")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
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
    /// Create a new internship group. Chỉ HR hoặc EnterpriseAdmin mới được tạo nhóm thực tập.
    /// </summary>
    /// <param name="command">Group creation data.</param>
    /// <returns code="201">Returns the created group details.</returns>
    /// <returns code="400">Invalid data.</returns>
    /// <returns code="403">Forbidden — only HR or EnterpriseAdmin can create groups.</returns>
    [HttpPost]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<CreateInternshipGroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
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
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
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
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DeleteInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
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
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<AddStudentsToGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
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
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<RemoveStudentsFromGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveStudentsFromGroup(
        [FromBody] RemoveStudentsFromGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to remove {Count} students from internship group {Id}", command.StudentIds.Count, command.InternshipId);
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Get placed students for the specific term and HR enterprise.
    /// </summary>
    [HttpGet("placed-students")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetPlacedStudentsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlacedStudents(
        [FromQuery] GetPlacedStudentsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get placed students with query: {@Query}", query);
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Move students between internship groups.
    /// </summary>
    [HttpPost("move-students")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<MoveStudentsBetweenGroupsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveStudentsBetweenGroups(
        [FromBody] MoveStudentsBetweenGroupsCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to move students between groups: from {FromGroupId} to {ToGroupId}", command.FromGroupId, command.ToGroupId);
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Archive an internship group.
    /// </summary>
    [HttpPatch("{id:guid}/archive")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ArchiveInternshipGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveInternshipGroup(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to archive internship group: {Id}", id);
        return HandleResult(await _mediator.Send(new ArchiveInternshipGroupCommand { InternshipGroupId = id }, cancellationToken));
    }

    /// <summary>
    /// Get dashboard statistics for an internship group.
    /// </summary>
    /// <param name="id">Internship group ID.</param>
    [HttpGet("{id:guid}/dashboard")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<GetInternshipGroupDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInternshipGroupDashboard(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get dashboard for internship group ID: {Id}", id);
        return HandleResult(await _mediator.Send(new GetInternshipGroupDashboardQuery(id), cancellationToken));
    }

    /// <summary>
    /// Get all internship terms the current student is enrolled in,
    /// together with their group assignment and placement status.
    /// Used by the Student Home page to render the InternshipCard list.
    /// </summary>
    [HttpGet("mine/internship-terms")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<List<GetMyInternshipTermsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyInternshipTerms(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get internship terms for current student.");
        return HandleResult(await _mediator.Send(new GetMyInternshipTermsQuery(), cancellationToken));
    }

    /// <summary>
    /// Get internship groups the current user (Student/Mentor/HR) is a member of.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Student,Mentor,HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<GetMyInternshipGroupsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInternshipGroups(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get internship groups for current user.");
        return HandleResult(await _mediator.Send(new GetMyInternshipGroupsQuery(), cancellationToken));
    }
}

