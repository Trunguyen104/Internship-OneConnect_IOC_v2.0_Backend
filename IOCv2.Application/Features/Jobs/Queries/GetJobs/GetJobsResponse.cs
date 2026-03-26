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

        // Quantity to hire (Số lượng tuyển)
        public int? Quantity { get; set; }

        // Number of applications for this job (Số application)
        public int ApplicationCount { get; set; }

        // Internship term name to display as "Kỳ thực tập" (if available)
        public string? TermName { get; set; }

        public short Status { get; set; }

        // Helper for UI: whether this job is Deleted (Status == DELETED)
        public bool IsDeleted { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Job, GetJobsResponse>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Enterprise.Name))
                .ForMember(d => d.CompanyLogoUrl, opt => opt.MapFrom(s => s.Enterprise.LogoUrl))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => (short)s.Status))
                .ForMember(d => d.Quantity, opt => opt.MapFrom(s => s.Quantity))
                .ForMember(d => d.ApplicationCount, opt => opt.MapFrom(s => s.InternshipApplications.Count))
                // Map an enterprise active term name (first open term) if present
                .ForMember(d => d.TermName, opt => opt.MapFrom(s =>
                    s.Enterprise.InternshipApplications
                        .Where(ia => ia.Term != null && ia.Term.Status == TermStatus.Open)
                        .Select(ia => ia.Term.Name)
                        .FirstOrDefault()))
                .ForMember(d => d.IsDeleted, opt => opt.MapFrom(s => s.Status == JobStatus.DELETED));
        }
    }
}