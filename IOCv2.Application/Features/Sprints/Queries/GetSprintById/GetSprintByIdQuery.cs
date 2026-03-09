using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprintById;

/// <summary>
/// Query to get a specific sprint by ID.
/// </summary>
/// <param name="ProjectId">The ID of the project.</param>
/// <param name="SprintId">The ID of the sprint.</param>
public record GetSprintByIdQuery(Guid ProjectId, Guid SprintId) : IRequest<Result<GetSprintByIdResponse>>;
