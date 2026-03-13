using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignProject;

/// <summary>
/// Response trả về sau khi Mentor gán Project vào nhóm.
/// </summary>
public class AssignProjectResponse : IMapFrom<Project>
{
    /// <summary>Mã Project được tạo.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Tên Project.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>Mô tả Project.</summary>
    public string? Description { get; set; }

    /// <summary>Mã nhóm thực tập chứa Project này (set thủ công sau Map).</summary>
    public Guid InternshipGroupId { get; set; }

    /// <summary>Thông báo kết quả (set thủ công sau Map).</summary>
    public string Message { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Project, AssignProjectResponse>()
            .ForMember(d => d.InternshipGroupId, opt => opt.MapFrom(s => s.InternshipId))
            .ForMember(d => d.Message, opt => opt.Ignore());
    }
}
