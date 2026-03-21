using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipTerms;
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

    [HttpGet("internship-terms")]
    [RateLimit(maxRequests: 20, windowMinutes: 1, blockMinutes: 5)]
    [ProducesResponseType(typeof(ApiResponse<List<GetMyInternshipTermsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTerms(CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetMyInternshipTermsQuery(), cancellationToken));
    }
}
