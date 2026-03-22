using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ResetUserPassword
{
    public record ResetUserPasswordCommand : IRequest<Result<ResetUserPasswordResponse>>
    {
        public Guid UserId { get; init; }
        public string Reason { get; init; } = null!;
        public string? NewPassword { get; init; }
    }
}
