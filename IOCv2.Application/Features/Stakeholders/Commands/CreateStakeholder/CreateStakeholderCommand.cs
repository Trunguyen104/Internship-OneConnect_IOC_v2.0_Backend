using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    public record CreateStakeholderCommand : IRequest<Result<CreateStakeholderResponse>>
    {
        public Guid ProjectId { get; init; }
        public string Name { get; init; } = null!;
        public StakeholderType Type { get; init; } = StakeholderType.Real;
        public string? Role { get; init; }
        public string? Description { get; init; }
        public string Email { get; init; } = null!;
        public string? PhoneNumber { get; init; }
    }
}

