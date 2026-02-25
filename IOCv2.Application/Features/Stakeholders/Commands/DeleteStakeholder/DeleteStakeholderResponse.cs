using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    public class DeleteStakeholderResponse : IMapFrom<Stakeholder>
    {
        public Guid Id { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
