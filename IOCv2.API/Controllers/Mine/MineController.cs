using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Mine;

[Tags("Mine")]
[Authorize(Roles = "Student")]
public class MineController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MineController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GetMyInternshipGroupsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetMyInternshipGroupsQuery(), cancellationToken));
    }
}
