using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActivePhases;

public record GetActivePhasesForEnterpriseQuery : IRequest<Result<GetActivePhasesForEnterpriseResponse>>;
