using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects
{
    [Tags("Project Resources")]
    [Authorize]
    [Route("api/projects/resources")]
    public class ProjectResourcesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ProjectResourcesController(IMediator mediator)
        {
            _mediator = mediator;
        }

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

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(Result<UploadProjectResourceResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadFile(
        [FromForm] UploadProjectResourceCommand command,
        CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return HandleResult(result);
        }

        [HttpGet("{resourceId}/ProjectResourceById")]
        [ProducesResponseType(typeof(Result<GetProjectResourceByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectResourceById(
        [FromRoute] Guid resourceId,
        CancellationToken cancellationToken)
        {
            var query = new GetProjectResourceByIdQuery { ProjectResourceId = resourceId };
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }


        [HttpPut("{resourceId}/update")]
        [ProducesResponseType(typeof(Result<UpdateProjectResourceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProjectResource(
       [FromRoute] Guid resourceId
            , [FromBody] UpdateProjectResourceRequest request,
       CancellationToken cancellationToken)
        {
            var command = new UpdateProjectResourceCommand
            {
                ProjectResourceId = resourceId,
                ProjectId = request.ProjectId,
                ResourceName = request.ResourceName,
                ResourceType = request.ResourceType
            };
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }


        [HttpDelete("{resourceId}/delete")]
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
}
