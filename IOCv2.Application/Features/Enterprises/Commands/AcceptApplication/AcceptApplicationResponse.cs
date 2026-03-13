using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Enterprises.Commands.AcceptApplication;

/// <summary>
/// Response trả về sau khi chấp nhận đơn ứng tuyển.
/// </summary>
public class AcceptApplicationResponse : IMapFrom<IOCv2.Domain.Entities.InternshipApplication>
{
    /// <summary>Mã đơn ứng tuyển.</summary>
    public Guid ApplicationId { get; set; }

    /// <summary>Mã sinh viên.</summary>
    public Guid StudentId { get; set; }

    /// <summary>Trạng thái đơn sau khi xử lý (dạng string, ví dụ: "Approved").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Thời gian xét duyệt.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>ID nhân viên doanh nghiệp đã xét duyệt.</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>Thông báo kết quả (set thủ công sau Map).</summary>
    public string Message { get; set; } = string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<IOCv2.Domain.Entities.InternshipApplication, AcceptApplicationResponse>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Message, opt => opt.Ignore());
    }
}
