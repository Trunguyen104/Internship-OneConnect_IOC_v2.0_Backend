using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;

public record CreateInternshipPhaseCommand : IRequest<Result<CreateInternshipPhaseResponse>>
{
    public Guid EnterpriseId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int? MaxStudents { get; init; }
    public string? Description { get; init; }
}
