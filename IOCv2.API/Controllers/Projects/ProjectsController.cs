using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Application.Features.Projects.Commands.DeleteProject;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Application.Features.Projects.Queries.GetAProjects;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using IOCv2.Domain.Enums;
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

        [HttpPost("create")]
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

        [HttpPut("{projectId}")]
        [ProducesResponseType(typeof(Result<UpdateProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProject(
        [FromRoute] Guid projectId
            , [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
        {
            var command = new UpdateProjectCommand
            {
                ProjectId = projectId,
                InternshipId = request.InternshipId,
                ProjectName = request.ProjectName,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status
            };
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("{projectId}")]
        [ProducesResponseType(typeof(DeleteProjectResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject([FromRoute] Guid projectId, CancellationToken cancellationToken)
        {
            var command = new DeleteProjectCommand { ProjectId = projectId };
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

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

        [HttpGet("internship-group/{internshipId}/projects")]
        [ProducesResponseType(typeof(Result<PaginatedResult<GetProjectsByInternshipIdResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectsByInternshipId(
        [FromRoute] Guid internshipId, [FromQuery] string? searchTerm, [FromQuery] ProjectStatus? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken cancellationToken, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetProjectsByInternshipIdQuery
            {
                InternshipId = internshipId,
                SearchTerm = searchTerm,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

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
    }
}
