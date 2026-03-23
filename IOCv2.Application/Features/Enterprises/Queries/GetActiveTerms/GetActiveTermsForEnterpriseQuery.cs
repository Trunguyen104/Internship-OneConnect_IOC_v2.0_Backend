using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActiveTerms;

public class GetActiveTermsForEnterpriseQuery : IRequest<Result<GetActiveTermsForEnterpriseResponse>>
{
    public Guid? UniversityId { get; set; }
}
