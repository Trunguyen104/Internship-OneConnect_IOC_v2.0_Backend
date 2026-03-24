using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using System;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobs
{
    public class GetJobsResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string? CompanyLogoUrl { get; set; }
        public DateTime? ExpireDate { get; set; }
        public short Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Job, GetJobsResponse>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Enterprise.Name))
                .ForMember(d => d.CompanyLogoUrl, opt => opt.MapFrom(s => s.Enterprise.LogoUrl))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (short)s.Status));
        }
    }
}