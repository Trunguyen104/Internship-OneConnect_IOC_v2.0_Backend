using IOC.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOC.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST: /api/auth/login
        // Accepts email and password, returns JWT access token and expiry
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}