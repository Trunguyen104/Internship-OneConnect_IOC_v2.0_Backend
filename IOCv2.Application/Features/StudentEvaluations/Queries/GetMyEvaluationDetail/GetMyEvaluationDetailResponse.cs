using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetMyEvaluationDetail;

public class CriteriaScoreDto
{
    public string CriteriaName { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? Comment { get; set; }
}

public class GetMyEvaluationDetailResponse
{
    public Guid? EvaluationId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public string? EvaluatorName { get; set; }
    public DateTime? GradedAt { get; set; }
    public decimal? TotalScore { get; set; }
    public string? GeneralComment { get; set; }
    public List<CriteriaScoreDto> CriteriaScores { get; set; } = new List<CriteriaScoreDto>();
}
