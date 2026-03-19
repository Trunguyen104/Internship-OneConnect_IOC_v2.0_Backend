using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Universities.Commands.UpdateUniversity;

public record UpdateUniversityCommand : IRequest<Result<bool>>
{
    public Guid UniversityId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? LogoUrl { get; init; }
}
