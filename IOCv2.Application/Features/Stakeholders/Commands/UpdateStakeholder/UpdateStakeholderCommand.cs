using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder
{
    public record UpdateStakeholderCommand : IRequest<Result<UpdateStakeholderResponse>>
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public StakeholderType? Type { get; init; }
        public string? Role { get; init; }
        public string? Description { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
    }
}

