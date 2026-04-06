using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Linq;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobs
{
    public class GetJobsResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string? CompanyLogoUrl { get; set; }
        public DateTime? ExpireDate { get; set; }

        // Number of applications for this job (Số application)
        public int ApplicationCount { get; set; }
        public string InternshipPhaseName { get; set; } = null!;
        public short Status { get; set; }

        // Helper for UI: whether this job is Deleted (Status == DELETED)
        public bool IsDeleted { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Job, GetJobsResponse>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Enterprise.Name))
                .ForMember(d => d.CompanyLogoUrl, opt => opt.MapFrom(s => s.Enterprise.LogoUrl))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (short)(s.Status ?? JobStatus.DRAFT)))
                .ForMember(d => d.ApplicationCount, opt => opt.MapFrom(s => s.InternshipApplications.Count))
                .ForMember(d => d.InternshipPhaseName, opt => opt.MapFrom(s => s.InternshipPhase!.Name ?? string.Empty))
                .ForMember(d => d.IsDeleted, opt => opt.MapFrom(s => s.Status == JobStatus.DELETED));
        }
    }
}