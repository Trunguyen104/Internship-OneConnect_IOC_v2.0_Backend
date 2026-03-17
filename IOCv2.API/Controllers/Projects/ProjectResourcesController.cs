using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace IOCv2.API.Controllers.Projects;

/// <summary>
/// Project Resources — manage files and resources attached to projects.
/// Exposes endpoints to list, upload, download, read, update and delete project resources.
/// </summary>
[Tags("Project Resources")]
[Authorize]
public class ProjectResourcesController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorageService;

    /// <summary>
    /// Creates a new instance of <see cref="ProjectResourcesController"/>.
    /// </summary>
    /// <param name="mediator">MediatR mediator to dispatch commands and queries to application handlers.</param>
    /// <param name="fileStorageService">Service used to read/write files from external storage (blob, disk, etc.).</param>
    public ProjectResourcesController(IMediator mediator, IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a paginated list of project resources matching the provided query.
    /// </summary>
    /// <param name="query">Query parameters (filters, paging, sorting) for retrieving resources.</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>Paginated result with project resources or an error response.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetAllProjectResourcesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllProjectResources(
        [FromQuery] GetAllProjectResourcesQuery query,
        CancellationToken cancellationToken)
    {
        // Forward query to application layer and return standardized result handling.
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Downloads the binary file for a specific project resource.
    /// </summary>
    /// <param name="resourceId">The id of the project resource to download.</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>File stream result with content-disposition for download, or NotFound / error response.</returns>
    [HttpGet("{resourceId:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDownloadProjectResourceById(
       [FromRoute] Guid resourceId,
       CancellationToken cancellationToken)
    {
        // First, retrieve the resource metadata to get the file path
        var query = new GetDownloadProjectResourceByIdQuery
        {
            ProjectResourceId = resourceId
        };
        var result = await _mediator.Send(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }
        return result.Data!.FileResponse;
    }

    /// <summary>
    /// Returns a readable file stream or inline content for the specified resource (for preview/inline view).
    /// </summary>
    /// <param name="resourceId">The id of the project resource to read/preview.</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>File content or an error response handled by application layer.</returns>
    [HttpGet("{resourceId:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<GetReadProjectResourceByIdResponse>), StatusCodes.Status200OK)]
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
    /// Uploads a new project resource. Expects multipart/form-data with file and metadata.
    /// </summary>
    /// <param name="command">Upload command containing file, project id and other metadata (bound from form-data).</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>Created resource information or an error response.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<UploadProjectResourceResponse>), StatusCodes.Status201Created)]
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
    /// Updates metadata for an existing project resource.
    /// </summary>
    /// <param name="resourceId">The id of the project resource to update.</param>
    /// <param name="command">Update command containing the new metadata (bound from request body).</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>Updated resource information or an error response.</returns>
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
    /// Deletes a project resource by id. This will remove metadata and (if applicable) the stored file.
    /// </summary>
    /// <param name="resourceId">The id of the resource to delete.</param>
    /// <param name="cancellationToken">Cancellation token provided by the framework.</param>
    /// <returns>Result indicating success or the appropriate error response.</returns>
    [HttpDelete("{resourceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteProjectResourceResponse>), StatusCodes.Status200OK)]
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
