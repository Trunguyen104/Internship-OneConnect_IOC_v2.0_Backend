using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Commands.ToggleUserStatus
{
    public record ToggleUserStatusCommand : IRequest<Result<ToggleUserStatusResponse>>
    {
        public Guid UserId { get; init; }
        public string NewStatus { get; init; } = null!;
    }
}
