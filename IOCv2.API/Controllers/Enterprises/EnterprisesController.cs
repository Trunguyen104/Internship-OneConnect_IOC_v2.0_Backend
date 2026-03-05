using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise;
using IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterprises;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Enterprises
{
    [Route("api/Enterprises")]
    [Authorize]
    [ApiController]
    public class EnterprisesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EnterprisesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Retrieves a paginated list of enterprises based on filter criteria.
        /// </summary>
        /// <param name="query">
        /// The query parameters used for filtering, sorting, and pagination.
        /// </param>
        /// <param name="cancellationToken">
        /// Token to cancel the request.
        /// </param>
        /// <returns>
        /// Returns:
        /// - 200 OK if the request is successful.
        /// - 400 Bad Request if the query parameters are invalid.
        /// - 401 Unauthorized if the user is not authenticated.
        /// - 500 Internal Server Error for unexpected failures.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PaginatedResult<GetEnterprisesResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEnterprises(
            [FromQuery] GetEnterprisesQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            // Success case
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            // Validation or bad request error
            if (result.ErrorType == ResultErrorType.BadRequest ||
                result.ErrorType == ResultErrorType.BadRequest)
            {
                return BadRequest(result);
            }

            // Unexpected system error
            return StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        /// <summary>
        /// Retrieves an enterprise by its unique identifier.
        /// </summary>
        /// <param name="enterpriseId">
        /// The unique identifier of the enterprise.
        /// </param>
        /// <param name="cancellationToken">
        /// Token to cancel the request.
        /// </param>
        /// <returns>
        /// Returns:
        /// - 200 OK if the enterprise is found.
        /// - 404 Not Found if the enterprise does not exist.
        /// - 401 Unauthorized if the user is not authenticated.
        /// - 500 Internal Server Error for unexpected failures.
        /// </returns>
        [HttpGet("{enterpriseId}")]
        [ProducesResponseType(typeof(Result<GetEnterpriseByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEnterpriseById(
            [FromRoute] Guid enterpriseId,
            CancellationToken cancellationToken)
        {
            var query = new GetEnterpriseByIdQuery { Id = enterpriseId };
            var result = await _mediator.Send(query, cancellationToken);
            // If enterprise is found
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            // If not found
            if (result.ErrorType == ResultErrorType.NotFound)
            {
                return NotFound(result);
            }

            // Unexpected error
            return StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        /// <summary>
        /// Creates a new enterprise.
        /// </summary>
        /// <param name="command">
        /// The command containing enterprise creation data.
        /// </param>
        /// <param name="cancellationToken">
        /// Token to cancel the request.
        /// </param>
        /// <returns>
        /// Returns:
        /// - 201 Created if the enterprise is successfully created.
        /// - 409 Conflict if the enterprise already exists.
        /// - 500 Internal Server Error for unexpected failures.
        /// - 403 Forbidden if the user does not have the required role.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = "EnterpriseAdmin")]
        [ProducesResponseType(typeof(Result<GetEnterpriseByIdResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEnterprise(
            [FromBody] CreateEnterpriseCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            // Success case: return 201 Created with location header
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetEnterpriseById), new { enterpriseId = result.Data!.EnterpriseId }, result);
            }
            // Business conflict (e.g., duplicate tax code)
            else if (result.ErrorType == ResultErrorType.Conflict)
            {
                return Conflict(result);
            }
            // Unexpected system error
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// Updates an existing enterprise.
        /// </summary>
        /// <param name="enterpriseId">
        /// The unique identifier of the enterprise to update.
        /// </param>
        /// <param name="body">
        /// The command containing updated enterprise data.
        /// </param>
        /// <param name="cancellationToken">
        /// Token to cancel the request.
        /// </param>
        /// <returns>
        /// Returns:
        /// - 200 OK if the update is successful.
        /// - 400 Bad Request if the request is invalid.
        /// - 403 Forbidden if the user does not have permission.
        /// - 404 Not Found if the enterprise does not exist.
        /// - 500 Internal Server Error for unexpected failures.
        /// </returns>
        [HttpPut("{enterpriseId}")]
        [Authorize(Roles = "HR,EnterpriseAdmin")]
        [ProducesResponseType(typeof(Result<UpdateEnterpriseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEnterprise([FromRoute] Guid enterpriseId, [FromBody] UpdateEnterpriseCommand body, CancellationToken cancellationToken) {
            body.EnterpriseId = enterpriseId;
            var result = await _mediator.Send(body, cancellationToken);
            if (result.IsSuccess) return Ok(result);
            if (result.ErrorType == ResultErrorType.BadRequest) return BadRequest(result);
            if (result.ErrorType == ResultErrorType.NotFound) return NotFound(result);
            if (result.ErrorType == ResultErrorType.Forbidden) return StatusCode(StatusCodes.Status403Forbidden, result);
            return StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        /// <summary>
        /// Deletes an existing enterprise.
        /// </summary>
        [HttpDelete("{enterpriseId}")]
        [Authorize(Roles = "EnterpriseAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEnterprise([FromRoute] Guid enterpriseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteEnterpriseCommand { EnterpriseId = enterpriseId }, cancellationToken);
            if (result.IsSuccess) return NoContent();
            if (result.ErrorType == ResultErrorType.NotFound) return NotFound(result);
            return StatusCode(StatusCodes.Status500InternalServerError, result);
        }
    }
}
