using IOC.Application.AdminFeatures.Commands.CreateAdminAccountCommands;
using IOC.Application.AdminFeatures.Commands.DeleteAdminAccountCommands;
using IOC.Application.AdminFeatures.Commands.UpdateAdminAccountCommands;
using IOC.Application.AdminFeatures.Commands.UpdateAdminAccountStatusCommands;
using IOC.Application.AdminFeatures.Commands.ResetAdminAccountPasswordCommands;
using IOC.Application.AdminFeatures.Commands.ChangeAdminAccountRoleCommands;
using IOC.Application.AdminFeatures.Queries.GetAdminAccountListQuerys;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IOC.Domain.Enums;

namespace IOC.API.Controllers
{
    [ApiController]
    [Route("api/admin-accounts")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminAccountsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize]
        [HttpGet("GetListAdminAccount")]
        public async Task<IActionResult> GetList([FromQuery] GetAdminAccountListQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost("CreateAdminAccount")]
        public async Task<IActionResult> Create(
            CreateAdminAccountCommand command)
        {
                var id = await _mediator.Send(command);
                return Ok(new { id });
        }

        [HttpPut("UpdateAdminAccount")]
        public async Task<IActionResult> Update(UpdateAdminAccountCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }

        [HttpDelete("DeleteAdminAccount")]
        public async Task<IActionResult> Delete(DeleteAdminAccountCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromQuery] IOC.Domain.Enums.AccountStatus status)
        {
            var cmd = new UpdateAdminAccountStatusCommand { Id = id, TargetStatus = status };
            var idResult = await _mediator.Send(cmd);
            return Ok(new { id = idResult });
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword([FromRoute] Guid id)
        {
            var cmd = new ResetAdminAccountPasswordCommand { Id = id };
            var idResult = await _mediator.Send(cmd);
            return Ok(new { id = idResult });
        }

        [HttpPost("{id}/change-role")]
        public async Task<IActionResult> ChangeRole([FromRoute] Guid id, [FromQuery] IOC.Domain.Enums.AdminRole role, [FromQuery] Guid? organizationId)
        {
            var cmd = new ChangeAdminAccountRoleCommand { Id = id, Role = role, OrganizationId = organizationId };
            var idResult = await _mediator.Send(cmd);
            return Ok(new { id = idResult });
        }
    }
}
