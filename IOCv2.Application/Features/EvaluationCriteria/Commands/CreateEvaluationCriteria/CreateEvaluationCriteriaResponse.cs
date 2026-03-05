namespace IOCv2.Application.Features.EvaluationCriteria.Commands.CreateEvaluationCriteria;

public class CreateEvaluationCriteriaResponse
{
    public Guid CriteriaId { get; set; }
    public Guid CycleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public DateTime CreatedAt { get; set; }
}
