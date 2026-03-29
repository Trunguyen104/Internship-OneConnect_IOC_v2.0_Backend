using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectStudents
{
    public record GetProjectStudentsQuery : IRequest<Result<List<GetProjectStudentsResponse>>>
    {
        public Guid ProjectId { get; init; }
    }
}

