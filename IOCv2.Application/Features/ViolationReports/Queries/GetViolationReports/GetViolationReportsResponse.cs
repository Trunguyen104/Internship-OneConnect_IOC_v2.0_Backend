using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    public class GetViolationReportsResponse : IMapFrom<Domain.Entities.ViolationReport>
    {
        public Guid ViolationReportId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty!;
        public string StudentCode { get; set; } = string.Empty!;
        public string InternshipGroupName { get; set; } = string.Empty!;
        public string? MentorName { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateOnly OccurredDate { get; set; }
        public string Description { get; set; } = string.Empty!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<ViolationReport, GetViolationReportsResponse>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.User.UserCode))
                .ForMember(dest => dest.InternshipGroupName, opt => opt.MapFrom(src => src.InternshipGroup.GroupName))
                .ForMember(dest => dest.MentorName, opt => opt.MapFrom(src =>
                    src.InternshipGroup.Mentor != null ? src.InternshipGroup.Mentor.User.FullName : null));
        }
    }
}
