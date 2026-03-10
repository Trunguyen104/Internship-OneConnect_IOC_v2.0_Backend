using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Commands.CloseTerm;
using IOCv2.Application.Features.Terms.Commands.CreateTerm;
using IOCv2.Application.Features.Terms.Commands.DeleteTerm;
using IOCv2.Application.Features.Terms.Commands.UpdateTerm;
using IOCv2.Application.Features.Terms.Queries.GetTermById;
using IOCv2.Application.Features.Terms.Queries.GetTerms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Terms;

/// <summary>
/// Terms Management — Manage internship terms (create, view, update, close, delete)
/// </summary>
[Route("api/terms")]
[Tags("Terms Management")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class TermsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public TermsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get a paginated list of terms with optional filters
    /// </summary>
    /// <param name="query">Query parameters for filtering, searching, and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of terms</returns>
    [HttpGet]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<PaginatedResult<GetTermsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetTerms(
        [FromQuery] GetTermsQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Get term details by ID
    /// </summary>
    /// <param name="id">Term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Term details</returns>
    [HttpGet("{id:guid}")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<GetTermByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetTermById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetTermByIdQuery(id), cancellationToken));
    }

    /// <summary>
    /// Create a new internship term
    /// </summary>
    /// <param name="command">Term creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created term</returns>
    [HttpPost]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<CreateTermResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateTerm(
        [FromBody] CreateTermCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleCreatedResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Update an existing term
    /// </summary>
    /// <param name="id">Term ID</param>
    /// <param name="command">Updated term data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated term</returns>
    [HttpPut("{id:guid}")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<UpdateTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateTerm(
        [FromRoute] Guid id,
        [FromBody] UpdateTermCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { TermId = id }, cancellationToken));
    }

    /// <summary>
    /// Close an active term
    /// </summary>
    /// <param name="id">Term ID</param>
    /// <param name="command">Close term data including version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with a warning if unplaced students exist</returns>
    [HttpPatch("{id:guid}/close")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<CloseTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CloseTerm(
        [FromRoute] Guid id,
        [FromBody] CloseTermCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { TermId = id }, cancellationToken));
    }

    /// <summary>
    /// Delete a term (soft delete) - Only Upcoming terms can be deleted
    /// </summary>
    /// <param name="id">Term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success message with related data information</returns>
    [HttpDelete("{id:guid}")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(Result<DeleteTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteTerm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new DeleteTermCommand(id), cancellationToken));
    }
}
