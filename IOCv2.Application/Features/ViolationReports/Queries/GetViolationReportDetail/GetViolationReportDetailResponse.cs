using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail
{
    public class GetViolationReportDetailResponse : IMapFrom<ViolationReport>
    {
        public string StudentName { get; set; } = string.Empty!;
        public string StudentCode { get; set; } = string.Empty!;
        public string InternshipGroupName { get; set; } = string.Empty!;
        public string MentorName { get; set; } = string.Empty!;
        public string Description { get; set; } = string.Empty!;
        public List<ViolationAttachmentDto> ViolationAttachments { get; set; } = new List<ViolationAttachmentDto>();
        public DateTime OccurredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public ViolationStatus Status { get; set; }
        public List<ViolationUpdateHistoryDto> ViolationUpdateHistories { get; set; } = new List<ViolationUpdateHistoryDto>();
        public List<ViolationCommentDto> ViolationComments { get; set; } = new List<ViolationCommentDto>();
        public void Mapping(Profile profile)
        {
            profile.CreateMap<ViolationReport, GetViolationReportDetailResponse>()
                .ForMember(d => d.StudentName,
                    opt => opt.MapFrom(s => s.Student.User.FullName))
                .ForMember(d => d.StudentCode,
                    opt => opt.MapFrom(s => s.Student.User.UserCode))
                .ForMember(d => d.InternshipGroupName,
                    opt => opt.MapFrom(s => s.InternshipGroup.GroupName))
                .ForMember(d => d.MentorName,
                    opt => opt.MapFrom(s => s.InternshipGroup.Mentor!.User.FullName))
                .ForMember(d => d.ViolationAttachments,
                    opt => opt.MapFrom(s => s.Attachments))
                .ForMember(d => d.ViolationUpdateHistories,
                    opt => opt.MapFrom(s => s.UpdateHistories))
                .ForMember(d => d.ViolationComments,
                    opt => opt.MapFrom(s => s.Comments));
        }
    }

    public class ViolationAttachmentDto : IMapFrom<ViolationAttachment>
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public string FilePath { get; set; } = string.Empty!;
        public string FileName { get; set; } = string.Empty!;
    }

    public class ViolationUpdateHistoryDto : IMapFrom<ViolationUpdateHistory> 
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public ViolationStatus OldStatus { get; set; }
        public ViolationStatus NewStatus { get; set; }
        public string Reason { get; set; } = string.Empty!;
    }

    public class ViolationCommentDto : IMapFrom<ViolationComment>
    {
        public Guid Id { get; set; }
        public Guid ViolationReportId { get; set; }
        public Guid UserId { get; set; }
        public string Content { get; set; } = string.Empty!;
    }
}
