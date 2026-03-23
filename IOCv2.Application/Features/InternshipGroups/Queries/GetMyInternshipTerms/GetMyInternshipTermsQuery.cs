using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipTerms;

public class GetMyInternshipTermsQuery : IRequest<Result<List<GetMyInternshipTermsResponse>>>
{
}
