using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Application.Features.Projects.Commands.DeleteProject;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Application.Features.Projects.Queries.GetAllProjects;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects;

/// <summary>
/// Projects Management — manage internship projects.
/// </summary>
[Tags("Projects")]
[Authorize]
[Route("api/projects")]
public class ProjectsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of all projects with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetAllProjectsResponse>>), StatusCodes.Status200OK)]
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
    /// <param name="projectId">The unique identifier of the project to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// Returns 200 OK with <see cref="GetProjectByIdResponse"/> if found.
    /// Returns 400 Bad Request if the request is invalid.
    /// Returns 401 Unauthorized if the user is not authenticated.
    /// </returns>
    [HttpGet("{projectId}")]
    [ProducesResponseType(typeof(Result<GetProjectByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProjectById([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery { ProjectId = projectId };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated list of projects assigned to the currently authenticated student.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetProjectsByStudentIdResponse>>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(Result<PaginatedResult<GetProjectsByInternshipIdResponse>>), StatusCodes.Status200OK)]
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
    /// Create a new internship project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing project by ID.
    /// </summary>
    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(typeof(Result<UpdateProjectResponse>), StatusCodes.Status200OK)]
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
    /// Soft delete a project by ID.
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
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
