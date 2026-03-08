namespace IOCv2.Application.Features.Evaluations.Queries.GetInternshipEvaluations;

public class GetInternshipEvaluationsResponse
{
    public Guid CycleId { get; set; }
    public Guid InternshipId { get; set; }
    public List<CriteriaDto> Criteria { get; set; } = new();
    public List<StudentEvaluationDto> Students { get; set; } = new();
}

public class CriteriaDto
{
    public Guid CriteriaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}

public class StudentEvaluationDto
{
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEvaluated { get; set; }
    public string? Status { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Note { get; set; }
    public List<EvaluationDetailDto> Details { get; set; } = new();
}

public class EvaluationDetailDto
{
    public Guid CriteriaId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
