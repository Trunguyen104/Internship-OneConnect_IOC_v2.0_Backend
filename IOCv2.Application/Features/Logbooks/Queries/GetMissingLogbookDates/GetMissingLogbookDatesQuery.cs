using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Logbooks.Queries.GetMissingLogbookDates;

/// <summary>
/// Query to calculate the dates on which a student has NOT submitted a logbook,
/// excluding weekends and public holidays.
/// </summary>
public record GetMissingLogbookDatesQuery : IRequest<Result<GetMissingLogbookDatesResponse>>
{
    /// <summary>
    /// The student whose missing logbook dates should be calculated.
    /// When null, the current authenticated student is used.
    /// </summary>
    public Guid? StudentId { get; init; }
}
