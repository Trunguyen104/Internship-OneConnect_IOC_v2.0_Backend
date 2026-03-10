using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Students.Queries.GetInternships;
using IOCv2.Application.Features.Students.Queries.GetInternshipDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IOCv2.API.Controllers.Students
{
    /// <summary>
    /// Students Management — view and manage current internships for students.
    /// </summary>
    [Tags("Students - Internships")]
    [Authorize(Roles = "Student")]
    [Route("api/students/me/internships")]
    public class StudentsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StudentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get the current internships of the authenticated student.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<GetCurrentInternshipsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCurrentInternships(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCurrentInternshipsQuery(), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Get detailed information of a specific internship by term ID for the authenticated student.
        /// </summary>
        /// <param name="termId">The ID of the term to get details for.</param>
        [HttpGet("{termId:guid}")]
        [ProducesResponseType(typeof(Result<GetInternshipDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInternshipDetail([FromRoute] Guid termId, CancellationToken cancellationToken)
        {
            var query = new GetInternshipDetailQuery(termId);
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }
    }
}
