using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;
using IOCv2.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;
using IOCv2.Application.Features.PublicHolidays.Commands.SyncPublicHolidays;
using IOCv2.Application.Features.PublicHolidays.Queries.GetPublicHolidays;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.PublicHolidays;

/// <summary>
/// Public Holiday Management — manage and sync national public holidays
/// used to exclude non-working days from logbook calculations.
/// </summary>
[Tags("Public Holidays")]
[Route("api/v{version:apiVersion}/public-holidays")]
[Authorize(Roles = "HR")]
public class PublicHolidaysController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PublicHolidaysController> _logger;

    public PublicHolidaysController(IMediator mediator, ILogger<PublicHolidaysController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>
    /// Get all public holidays for a specific year.
    /// </summary>
    /// <param name="year">The year to query (e.g. 2026).</param>
    /// <returns code="200">List of public holidays for the given year.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GetPublicHolidaysResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPublicHolidays(
        [FromQuery] int year,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to get public holidays for year {Year}.", year);
        return HandleResult(await _mediator.Send(new GetPublicHolidaysQuery { Year = year }, cancellationToken));
    }

    /// <summary>
    /// Manually create a single public holiday entry.
    /// </summary>
    /// <param name="command">Date and optional description of the holiday.</param>
    /// <returns code="201">The created holiday entry.</returns>
    /// <returns code="409">A holiday on this date already exists.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreatePublicHolidayResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePublicHoliday(
        [FromBody] CreatePublicHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to create public holiday on {Date}.", command.Date);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetPublicHolidays),
            new { year = result.Data?.Date.Year, version = "1" });
    }

    /// <summary>
    /// Delete a public holiday by its ID.
    /// </summary>
    /// <param name="id">The public holiday ID to delete.</param>
    /// <returns code="200">Confirmation of deletion.</returns>
    /// <returns code="404">Holiday not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeletePublicHolidayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePublicHoliday(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Request to delete public holiday {Id}.", id);
        return HandleResult(await _mediator.Send(
            new DeletePublicHolidayCommand { PublicHolidayId = id }, cancellationToken));
    }

    /// <summary>
    /// Sync public holidays for a given year from the Calendarific external API.
    /// Only new holidays are inserted; existing dates are skipped automatically.
    /// </summary>
    /// <param name="command">Year and optional country code (default: VN).</param>
    /// <returns code="200">Summary of synced vs. skipped holidays.</returns>
    /// <returns code="500">External API call failed.</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncPublicHolidaysResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncPublicHolidays(
        [FromBody] SyncPublicHolidaysCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Request to sync public holidays for {Country}/{Year}.",
            command.CountryCode, command.Year);
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }
}
