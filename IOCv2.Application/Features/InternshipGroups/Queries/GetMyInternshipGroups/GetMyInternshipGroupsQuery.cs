using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;

public record GetMyInternshipGroupsQuery : IRequest<Result<List<GetMyInternshipGroupsResponse>>>;
