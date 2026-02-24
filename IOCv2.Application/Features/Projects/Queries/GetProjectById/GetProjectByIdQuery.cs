using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById;

public class GetProjectByIdQuery : IRequest<Result<GetProjectByIdResponse>>
{
    public Guid ProjectId { get; init; }
}
