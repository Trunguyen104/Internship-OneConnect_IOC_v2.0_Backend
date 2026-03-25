using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhaseById;

public record GetInternshipPhaseByIdQuery(Guid PhaseId)
    : IRequest<Result<GetInternshipPhaseByIdResponse>>;
