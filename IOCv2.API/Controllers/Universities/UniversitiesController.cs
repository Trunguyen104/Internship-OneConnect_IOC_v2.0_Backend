using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Universities.Commands.CreateUniversity;
using IOCv2.Application.Features.Universities.Commands.DeleteUniversity;
using IOCv2.Application.Features.Universities.Commands.UpdateUniversity;
using IOCv2.Application.Features.Universities.Queries.GetUniversities;
using IOCv2.Application.Features.Universities.Queries.GetUniversityById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Universities;

[Tags("Universities Management")]
[Authorize(Roles = "SuperAdmin")]
public class UniversitiesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UniversitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get a paginated list of universities.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetUniversitiesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniversities([FromQuery] GetUniversitiesQuery query)
    {
        return HandleResult(await _mediator.Send(query));
    }

    /// <summary>
    /// Get university by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetUniversityByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUniversity(Guid id)
    {
        return HandleResult(await _mediator.Send(new GetUniversityByIdQuery(id)));
    }

    /// <summary>
    /// Create a new university.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUniversity([FromBody] CreateUniversityCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleCreateResult(result, nameof(GetUniversity), new { id = result.Data });
    }

    /// <summary>
    /// Update an existing university.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUniversity(Guid id, [FromBody] UpdateUniversityCommand command)
    {
        if (id != command.UniversityId)
        {
            return BadRequest(ApiResponse<object>.Fail("ID mismatch"));
        }
        return HandleResult(await _mediator.Send(command));
    }

    /// <summary>
    /// Soft delete a university.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUniversity(Guid id)
    {
        return HandleResult(await _mediator.Send(new DeleteUniversityCommand(id)));
    }
}
