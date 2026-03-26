using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    /// <summary>
    /// Summary response for an internship group in a list view.
    /// </summary>
    public class GetInternshipGroupsResponse : IMapFrom<InternshipGroup>
    {
        public Guid InternshipId { get; set; }
        public Guid PhaseId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? EnterpriseName { get; set; }
        public string? MentorName { get; set; }
        public GroupStatus Status { get; set; }

        /// <summary>
        /// Total number of students assigned to this group.
        /// </summary>
        public int NumberOfMembers { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<InternshipGroup, GetInternshipGroupsResponse>()
                .ForMember(d => d.EnterpriseName, opt => opt.MapFrom(s => s.Enterprise != null ? s.Enterprise.Name : null))
                .ForMember(d => d.MentorName, opt => opt.MapFrom(s => s.Mentor != null && s.Mentor.User != null ? s.Mentor.User.FullName : null))
                .ForMember(d => d.NumberOfMembers, opt => opt.MapFrom(s => s.Members.Count))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
        }
    }
}
