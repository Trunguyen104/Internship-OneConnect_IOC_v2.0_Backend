using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;

public record CreateStakeholderCommand : IRequest<Result<Guid>>
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = null!;
    public StakeholderType Type { get; set; }
    public string? Role { get; set; }
    public string? Description { get; set; }
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}

