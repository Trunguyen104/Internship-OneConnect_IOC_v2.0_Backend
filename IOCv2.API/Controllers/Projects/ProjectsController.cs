using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Projects
{
    [Tags("Projects")]
    [Authorize]
    [Route("api/projects")]
    public class ProjectsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        public ProjectsController(IMediator mediator) => _mediator = mediator;

        [Authorize(Roles = "Student")]
        [HttpGet("student")]
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
    }
}
