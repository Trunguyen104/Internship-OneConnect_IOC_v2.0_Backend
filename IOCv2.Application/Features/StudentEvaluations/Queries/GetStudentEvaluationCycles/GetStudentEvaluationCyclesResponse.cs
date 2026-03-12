using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentEvaluationCycles;

public class GetStudentEvaluationCyclesResponse : IMapFrom<EvaluationCycle>
{
    public Guid CycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
