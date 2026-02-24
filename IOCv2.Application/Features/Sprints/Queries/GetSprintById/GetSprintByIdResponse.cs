using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprintById;

public class GetSprintByIdResponse : IMapFrom<Sprint>
{
    public Guid SprintId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SprintStatus Status { get; set; }
    public int TotalWorkItems { get; set; }
    public int CompletedWorkItems { get; set; }
    public DateTime CreatedAt { get; set; }
}
