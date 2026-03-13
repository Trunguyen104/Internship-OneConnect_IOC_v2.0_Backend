using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignMentor;

/// <summary>
/// Response trả về sau khi HR gán Mentor cho sinh viên.
/// </summary>
public class AssignMentorResponse : IMapFrom<InternshipGroup>
{
    /// <summary>Mã nhóm thực tập được tạo hoặc cập nhật.</summary>
    public Guid InternshipGroupId { get; set; }

    /// <summary>Tên nhóm thực tập.</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Mã Mentor được gán.</summary>
    public Guid? MentorId { get; set; }

    /// <summary>Mã đơn ứng tuyển liên quan (set thủ công sau Map).</summary>
    public Guid ApplicationId { get; set; }

    /// <summary>Thông báo kết quả (set thủ công sau Map).</summary>
    public string Message { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<InternshipGroup, AssignMentorResponse>()
            .ForMember(d => d.InternshipGroupId, opt => opt.MapFrom(s => s.InternshipId))
            .ForMember(d => d.ApplicationId, opt => opt.Ignore())
            .ForMember(d => d.Message, opt => opt.Ignore());
    }
}
