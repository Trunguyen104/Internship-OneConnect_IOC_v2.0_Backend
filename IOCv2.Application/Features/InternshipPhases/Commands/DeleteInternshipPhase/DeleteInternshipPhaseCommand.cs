using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipPhases.Commands.DeleteInternshipPhase;

public record DeleteInternshipPhaseCommand(Guid PhaseId)
    : IRequest<Result<bool>>;
