using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;

public record UpdateStakeholderCommand : IRequest<Result>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public StakeholderType? Type { get; set; }
    public string? Role { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}


