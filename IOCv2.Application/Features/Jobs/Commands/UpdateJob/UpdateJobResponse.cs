using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public class UpdateJobResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public JobStatus Status { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Job, UpdateJobResponse>()
                .ForMember(d => d.JobId, opt => opt.MapFrom(s => s.JobId))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
        }
    }
}
