using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Jobs.Queries.GetJobById.DTOs;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public class GetJobByIdResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public Guid EnterpriseId { get; set; }
        public Guid? InternshipPhaseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Benefit { get; set; }
        public string? Location { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public JobAudience Audience { get; set; }
        public JobStatus Status { get; set; }

        // Enterprise snapshot
        public string EnterpriseName { get; set; } = string.Empty;

        // Universities assigned (for Targeted audience)
        public List<UniversityDto> Universities { get; set; } = new();

        // Application counts grouped by status
        public List<ApplicationStatusCountDto> ApplicationStatusCounts { get; set; } = new();

        // Placed count (computed)
        public int PlacedCount { get; set; }

        // Banner message to show when placed == quantity (AC-11)
        public string? FilledBanner { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Job, GetJobByIdResponse>()
                .ForMember(dest => dest.JobId, opt => opt.MapFrom(src => src.JobId))
                .ForMember(dest => dest.EnterpriseName, opt => opt.MapFrom(src => src.Enterprise.Name))
                .ForMember(dest => dest.Universities, opt => opt.MapFrom(src => src.Universities.Select(u => new UniversityDto { UniversityId = u.UniversityId, Name = u.Name })))
                // ApplicationStatusCounts, PlacedCount and FilledBanner are computed in handler, not via automapper
                .ForMember(dest => dest.ApplicationStatusCounts, opt => opt.Ignore())
                .ForMember(dest => dest.PlacedCount, opt => opt.Ignore())
                .ForMember(dest => dest.FilledBanner, opt => opt.Ignore());
        }
    }
}
