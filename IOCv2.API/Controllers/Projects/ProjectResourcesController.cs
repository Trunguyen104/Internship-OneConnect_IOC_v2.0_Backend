using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetDownloadProjectResourceById;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects;

/// <summary>
/// Project Resources — manage files and resources attached to projects.
/// </summary>
[Tags("Project Resources")]
[Route("api/v{version:apiVersion}/project-resources")]
[Authorize]
public class ProjectResourcesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ProjectResourcesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a paginated list of project resources with optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetAllProjectResourcesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllProjectResources(
        [FromQuery] GetAllProjectResourcesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{resourceId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownloadProjectResourceById(
       [FromRoute] Guid resourceId,
       CancellationToken cancellationToken)
    {
        var query = new GetDownloadProjectResourceByIdQuery { ProjectResourceId = resourceId };
        var result = await _mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Data?.Content == null)
        {
            return HandleResult(result);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("{resourceId:guid}/read")]
    [ProducesResponseType(typeof(Result<GetReadProjectResourceByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReadProjectResourceById(
        [FromRoute] Guid resourceId,
        CancellationToken cancellationToken)
    {
        var command = new GetReadProjectResourceByIdQuery { ResourceId = resourceId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Upload a new file resource to a project.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(Result<UploadProjectResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProjectResource(
    [FromForm] UploadProjectResourceCommand command,
    CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update metadata of an existing project resource.
    /// </summary>
    [HttpPut("{resourceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateProjectResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProjectResource(
        [FromRoute] Guid resourceId,
        [FromBody] UpdateProjectResourceCommand command,
        CancellationToken cancellationToken)
    {
        var updateCommand = command with { ProjectResourceId = resourceId };
        var result = await _mediator.Send(updateCommand, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a project resource by ID.
    /// </summary>
    [HttpDelete("{resourceId:guid}")]
    [ProducesResponseType(typeof(Result<DeleteProjectResourceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProjectResource(
        [FromRoute] Guid resourceId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProjectResourceCommand { ResourceId = resourceId };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
