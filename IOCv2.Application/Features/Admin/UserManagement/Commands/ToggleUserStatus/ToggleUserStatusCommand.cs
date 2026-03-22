using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ToggleUserStatus
{
    public record ToggleUserStatusCommand : IRequest<Result<ToggleUserStatusResponse>>
    {
        public Guid UserId { get; init; }
        public UserStatus NewStatus { get; init; }
    }
}

