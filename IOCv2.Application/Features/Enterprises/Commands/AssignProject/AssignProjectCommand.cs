using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignProject;

public record AssignProjectCommand : IRequest<Result<AssignProjectResponse>>
{
    public Guid ApplicationId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string? ProjectDescription { get; init; }
}
