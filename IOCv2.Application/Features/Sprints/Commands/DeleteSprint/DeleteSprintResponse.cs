using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public class DeleteSprintResponse : IMapFrom<Sprint>
{
    public Guid SprintId { get; set; }
    public string Name { get; set; } = string.Empty;
}
