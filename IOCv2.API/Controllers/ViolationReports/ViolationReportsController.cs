using System;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.ViolationReports
{
    [Route("api/[controller]")]
    [ApiController]
    public class ViolationReportsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public ViolationReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetViolationReportsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetViolationReports([FromQuery] GetViolationReportsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // GET api/ViolationReports/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<GetViolationReportDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetViolationReportDetail([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            var query = new GetViolationReportDetailQuery
            {
                ViolationReportId = id
            };

            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }
    }
}
