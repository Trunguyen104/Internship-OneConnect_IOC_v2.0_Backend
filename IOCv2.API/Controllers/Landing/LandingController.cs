using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Public.SendReservationEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Landing
{
    [Tags("Landing")]
    [RateLimit(maxRequests: 3, windowMinutes: 10, blockMinutes: 10)]
    public class LandingController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public LandingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Sends a reservation/contact email from the landing page.
        /// </summary>
        /// <param name="command">Reservation details</param>
        /// <returns>Success or failure message</returns>
        [HttpPost("reservation")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendReservation([FromBody] SendReservationEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
