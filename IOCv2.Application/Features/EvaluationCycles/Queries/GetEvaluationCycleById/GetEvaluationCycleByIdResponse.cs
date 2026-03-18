using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycleById;

public class GetEvaluationCycleByIdResponse
{
    public Guid CycleId { get; set; }
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EvaluationCycleStatus Status { get; set; }

    public List<CriteriaDto> Criteria { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CriteriaDto
{
    public Guid CriteriaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}
