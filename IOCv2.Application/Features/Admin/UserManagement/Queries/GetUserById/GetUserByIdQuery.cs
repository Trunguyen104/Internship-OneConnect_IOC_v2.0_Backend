using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById
{
    public record GetUserByIdQuery : IRequest<Result<GetUserByIdResponse>>
    {
        public Guid UserId { get; init; }
    }
}
