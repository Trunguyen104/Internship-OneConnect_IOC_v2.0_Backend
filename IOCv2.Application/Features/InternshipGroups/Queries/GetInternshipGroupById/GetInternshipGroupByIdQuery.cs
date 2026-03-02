using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    public record GetInternshipGroupByIdQuery(Guid InternshipId) : IRequest<Result<GetInternshipGroupByIdResponse>>;
}
