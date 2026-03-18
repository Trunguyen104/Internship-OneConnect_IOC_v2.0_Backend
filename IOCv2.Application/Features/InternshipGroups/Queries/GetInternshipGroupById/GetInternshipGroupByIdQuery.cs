using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    /// <summary>
    /// Query to retrieve detailed information about a specific internship group.
    /// </summary>
    /// <param name="InternshipId">Unique identifier of the internship group.</param>
    public record GetInternshipGroupByIdQuery(Guid InternshipId) : IRequest<Result<GetInternshipGroupByIdResponse>>;
}
