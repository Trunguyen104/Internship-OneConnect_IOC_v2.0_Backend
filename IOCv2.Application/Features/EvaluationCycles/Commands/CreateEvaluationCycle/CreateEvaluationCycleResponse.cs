using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;

public class CreateEvaluationCycleResponse : IMapFrom<EvaluationCycle>
{
    public Guid CycleId { get; set; }
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
