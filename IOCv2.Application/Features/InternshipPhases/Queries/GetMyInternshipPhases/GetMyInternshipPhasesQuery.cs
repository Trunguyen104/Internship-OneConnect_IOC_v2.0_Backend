using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetMyInternshipPhases;

public class GetMyInternshipPhasesQuery : IRequest<Result<List<GetMyInternshipPhasesResponse>>>
{
}
