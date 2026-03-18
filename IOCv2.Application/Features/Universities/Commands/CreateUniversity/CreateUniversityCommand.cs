using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Universities.Commands.CreateUniversity;

/// <summary>
/// Command to create a new university.
/// </summary>
public record CreateUniversityCommand : IRequest<Result<Guid>>
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}
