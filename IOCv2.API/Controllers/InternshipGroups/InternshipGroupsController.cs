using IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup;
using IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup;
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

        [HttpPost]
        public async Task<IActionResult> CreateInternshipGroup([FromBody] CreateInternshipGroupCommand command)
        {
            return HandleResult(await _mediator.Send(command));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInternshipGroup(Guid id, [FromBody] UpdateInternshipGroupCommand command)
        {
            if (id != command.InternshipId)
            {
                command.InternshipId = id;
            }
            return HandleResult(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInternshipGroup(Guid id)
        {
            return HandleResult(await _mediator.Send(new DeleteInternshipGroupCommand(id)));
        }

        [HttpPost("{id}/students")]
        public async Task<IActionResult> AddStudentsToGroup(Guid id, [FromBody] AddStudentsToGroupCommand command)
        {
            if (id != command.InternshipId)
            {
                command.InternshipId = id;
            }
            return HandleResult(await _mediator.Send(command));
        }

        [HttpDelete("{id}/students")]
        public async Task<IActionResult> RemoveStudentsFromGroup(Guid id, [FromBody] RemoveStudentsFromGroupCommand command)
        {
            if (id != command.InternshipId)
            {
                command.InternshipId = id;
            }
            return HandleResult(await _mediator.Send(command));
        }
    }
}
