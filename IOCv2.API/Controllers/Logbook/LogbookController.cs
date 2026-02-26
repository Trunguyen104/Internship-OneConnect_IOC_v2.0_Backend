using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Logbook
{
    [Tags("Logbook")]
    public class LogbookController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public LogbookController(IMediator mediator, ILogger<LogbookController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        //Get all logbooks with pagination, filtering, and sorting
        [HttpGet]
        [Route("logbooks")]
        [ProducesResponseType(typeof(Result<GetLogbooksResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogbooks([FromQuery] GetLogbooksQuery query)
        {
            var result = await _mediator.Send(query);
            return HandleResult(result);
        }

        //Get a specific logbook by ID
        [HttpGet]
        [Route("logbooks/{id:guid}")]
        [ProducesResponseType(typeof(Result<GetLogbooksResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<GetLogbooksResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLogbookById(Guid id)
        {
            var result = await _mediator.Send(new GetLogbooksByIdQuery { LogbookId = id });
            return HandleResult(result);
        }

        //Create a new logbook entry
        [HttpPost]
        [Route("logbooks")]
        [ProducesResponseType(typeof(Result<GetLogbooksResponse>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateLogbook([FromBody] CreateLogbookCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }
    }
}
