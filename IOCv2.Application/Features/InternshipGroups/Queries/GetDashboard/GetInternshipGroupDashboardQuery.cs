using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;

/// <summary>
/// Query to get dashboard statistics for an internship group.
/// </summary>
/// <param name="InternshipId">The ID of the internship group.</param>
public record GetInternshipGroupDashboardQuery(Guid InternshipId) : IRequest<Result<GetInternshipGroupDashboardResponse>>;
