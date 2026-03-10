using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;


namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintResponse : IMapFrom<Sprint>
{
    public Guid SprintId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public SprintStatus Status { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Mapping(MappingProfile profile)
    {
        profile.CreateMap<Sprint, StartSprintResponse>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));

    }
}
