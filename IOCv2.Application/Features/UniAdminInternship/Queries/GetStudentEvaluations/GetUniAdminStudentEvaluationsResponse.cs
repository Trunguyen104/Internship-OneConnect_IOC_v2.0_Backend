namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;

public class GetUniAdminStudentEvaluationsResponse
{
    public List<EvaluationCycleDto> Cycles { get; set; } = new();
}

public class EvaluationCycleDto
{
    public Guid EvaluationId { get; set; }
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateTime CycleStartDate { get; set; }
    public DateTime CycleEndDate { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public string? GeneralComment { get; set; }
    public List<EvaluationDetailDto> Details { get; set; } = new();
}

public class EvaluationDetailDto
{
    public string CriteriaName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}
