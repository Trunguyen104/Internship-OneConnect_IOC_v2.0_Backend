using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    public class GetViolationReportsResponse : IMapFrom<Domain.Entities.ViolationReport>
    {
        public string StudentName { get; set; } = string.Empty!;
        public string StudentCode { get; set; } = string.Empty!;
        public string InternshipGroupName { get; set; } = string.Empty!;
        public DateOnly OccurredDate { get; set; }
        public Guid CreatedBy { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ViolationStatus ViolationStatus { get; set; }
        public void Mapping(Profile profile) {
            profile.CreateMap<ViolationReport, GetViolationReportsResponse>()
                .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.User.UserCode))
                .ForMember(dest => dest.InternshipGroupName, opt => opt.MapFrom(src => src.InternshipGroup.GroupName))
                .ForMember(dest => dest.OccurredDate, opt => opt.MapFrom(src => src.OccurredDate))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.ViolationStatus, opt => opt.MapFrom(src => src.Status));
        }
    }
}
