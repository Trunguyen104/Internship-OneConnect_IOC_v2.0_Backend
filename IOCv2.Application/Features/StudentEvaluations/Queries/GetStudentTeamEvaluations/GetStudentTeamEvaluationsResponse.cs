using System;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentTeamEvaluations;

public class GetStudentTeamEvaluationsResponse
{
    public Guid StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string EvaluationStatus { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
}
