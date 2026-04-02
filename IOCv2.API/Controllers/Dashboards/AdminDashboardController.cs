using IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Dashboards;

[Authorize(Roles = "SuperAdmin,Moderator")]
public class AdminDashboardController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public AdminDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets high-level platform statistics for the SuperAdmin dashboard.
    /// </summary>
    /// <returns>Aggregated counts for users, universities, enterprises, jobs, and internship status.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminDashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminDashboardStatsQuery(), cancellationToken);
        return HandleResult(result);
    }
}
