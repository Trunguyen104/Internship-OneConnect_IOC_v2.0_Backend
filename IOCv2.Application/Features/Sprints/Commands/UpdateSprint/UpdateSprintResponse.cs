using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintResponse : IMapFrom<Sprint>
{
    public Guid SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public void Mapping(MappingProfile profile)
    {
        profile.CreateMap<Sprint, UpdateSprintResponse>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
