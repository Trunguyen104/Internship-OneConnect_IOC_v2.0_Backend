using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.ViolationReports.Commands.UpdateViolationReport;
using IOCv2.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.DeleteViolationReport
{
    public class DeleteViolationReportResponse : IMapFrom<ViolationReport>
    {
        public string StudentName { get; set; } = string.Empty!;
        public string StudentCode { get; set; } = string.Empty!;
        public string InternshipGroupName { get; set; } = string.Empty!;
        public string MentorName { get; set; } = string.Empty!;
        public string Description { get; set; } = string.Empty!;
        public DateOnly OccurredDate { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<ViolationReport, DeleteViolationReportResponse>()
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s => s.Student.User.FullName))
                .ForMember(d => d.StudentCode,
                    opt => opt.MapFrom(s => s.Student.User.UserCode))
                .ForMember(d => d.InternshipGroupName,
                    opt => opt.MapFrom(s => s.InternshipGroup.GroupName))
                .ForMember(d => d.MentorName,
                    opt => opt.MapFrom(s => s.InternshipGroup.Mentor!.User.FullName));
        }
    }
}
