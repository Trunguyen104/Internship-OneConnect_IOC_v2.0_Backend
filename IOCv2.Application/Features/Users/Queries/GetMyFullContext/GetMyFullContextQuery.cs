using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Users.Queries.GetMyFullContext;

public class GetMyFullContextQuery : IRequest<Result<GetMyFullContextResponse>>
{
}
