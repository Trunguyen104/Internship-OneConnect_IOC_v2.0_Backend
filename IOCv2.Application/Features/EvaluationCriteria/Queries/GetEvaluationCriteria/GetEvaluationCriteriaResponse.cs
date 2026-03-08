namespace IOCv2.Application.Features.EvaluationCriteria.Queries.GetEvaluationCriteria;

public class GetEvaluationCriteriaResponse
{
    public Guid CriteriaId { get; set; }
    public Guid CycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
