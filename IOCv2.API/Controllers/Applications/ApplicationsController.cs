using IOCv2.Application.Features.Jobs.Queries.GetAllJobApplications;
using IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IOCv2.API.Controllers;

[Authorize(Roles = "HR")]
public class ApplicationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications([FromQuery] GetAllInternshipApplicationsQuery q)
    {
        var res = await _mediator.Send(q);
        return HandleResult(res);
    }

    [HttpPut("applications/{id}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateInternshipApplicationStatusCommand cmd)
    {
        cmd = cmd with { ApplicationId = id };
        var res = await _mediator.Send(cmd);
        return HandleResult(res);
    }
}