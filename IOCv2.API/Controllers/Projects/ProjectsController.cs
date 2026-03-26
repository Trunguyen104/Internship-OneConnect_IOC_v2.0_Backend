using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Commands.CompleteProject;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Application.Features.Projects.Commands.DeleteProject;
using IOCv2.Application.Features.Projects.Commands.PublishProject;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Application.Features.Projects.Queries.GetAllProjects;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Features.Projects.Queries.GetProjectStudents;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects;

/// <summary>
/// Projects Management — manage internship projects.
/// </summary>
[Tags("Projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of all projects with optional filters.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetAllProjectsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllProjects(
        [FromQuery] GetAllProjectsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves detailed information of a specific project by its unique identifier.
    /// </summary>
    [HttpGet("{projectId:guid}", Name = "GetProjectById")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<GetProjectByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery { ProjectId = projectId };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get list of students in the project's internship group.
    /// </summary>
    [HttpGet("{projectId:guid}/students")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<List<GetProjectStudentsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectStudents(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var query = new GetProjectStudentsQuery { ProjectId = projectId };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated list of projects visible to the currently authenticated student.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetProjectsByStudentIdResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectsByStudentId(
        [FromQuery] GetProjectsByStudentIdQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated list of projects belonging to a specific internship group.
    /// </summary>
    [HttpGet("internship-group")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin,Mentor,Student")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetProjectsByInternshipIdResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectsByInternshipId(
        [FromQuery] Guid internshipId,
        [FromQuery] GetProjectsByInternshipIdQuery query,
        CancellationToken cancellationToken = default)
    {
        query.InternshipId = internshipId;
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new internship project. Only Mentor role can create projects.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<CreateProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetProjectById), new { projectId = result.Data?.ProjectId, version = "1" });
    }

    /// <summary>
    /// Update an existing project. Only the project's Mentor can update.
    /// </summary>
    [HttpPut("{projectId:guid}")]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProject(
        [FromRoute] Guid projectId,
        [FromBody] UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        command.ProjectId = projectId;
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Publish a Draft project (Draft → Published). Only the project's Mentor can publish.
    /// </summary>
    [HttpPatch("{projectId:guid}/publish")]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<PublishProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishProject(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var command = new PublishProjectCommand { ProjectId = projectId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Complete a Published project (Published → Completed). Only the project's Mentor can complete.
    /// </summary>
    [HttpPatch("{projectId:guid}/complete")]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<CompleteProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProject(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var command = new CompleteProjectCommand { ProjectId = projectId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a project. Only the project's Mentor can delete.
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    [Authorize(Roles = "Mentor")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProjectCommand { ProjectId = projectId };
        var result = await _mediator.Send<Result<string>>(command, cancellationToken);
        return HandleResult(result);
    }
}
