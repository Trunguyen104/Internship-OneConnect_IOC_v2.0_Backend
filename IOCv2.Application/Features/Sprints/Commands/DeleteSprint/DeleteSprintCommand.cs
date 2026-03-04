using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

/// <summary>
/// Command to delete a planned sprint.
/// </summary>
/// <param name="ProjectId">The ID of the project the sprint belongs to.</param>
/// <param name="SprintId">The ID of the sprint to delete.</param>
public record DeleteSprintCommand(Guid ProjectId, Guid SprintId) : IRequest<Result<DeleteSprintResponse>>;
