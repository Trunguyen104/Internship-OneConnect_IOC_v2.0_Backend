using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.InternshipGroups
{
    public class InternshipGroupsController : ApiControllerBase
    {
        private readonly ISender _mediator;

        public InternshipGroupsController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetInternshipGroups([FromQuery] GetInternshipGroupsQuery query)
        {
            return HandleResult(await _mediator.Send(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInternshipGroupById(Guid id)
        {
            return HandleResult(await _mediator.Send(new GetInternshipGroupByIdQuery(id)));
        }
    }
}
