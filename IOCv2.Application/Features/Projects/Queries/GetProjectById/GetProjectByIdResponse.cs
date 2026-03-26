using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class GroupInfoDto
    {
        public Guid InternshipId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? MentorName { get; set; }
        public int StudentCount { get; set; }
    }

    /// <summary>
    /// Detailed response model for a specific project.
    /// </summary>
    public class GetProjectByIdResponse : IMapFrom<Project>
    {
        /// <summary>
        /// Unique identifier for the project.
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Identity of the internship group associated with this project.
        /// </summary>
        public Guid? InternshipId { get; set; }

        /// <summary>
        /// Name of the project.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Mã dự án (auto-generated hoặc do mentor cung cấp)</summary>
        public string ProjectCode { get; set; } = string.Empty;

        /// <summary>Lĩnh vực dự án (VD: CNTT, Mobile, IoT)</summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>Yêu cầu dự án</summary>
        public string Requirements { get; set; } = string.Empty;

        /// <summary>Kết quả bàn giao (tùy chọn)</summary>
        public string? Deliverables { get; set; }

        /// <summary>Template dự án (None, Scrum, Kanban)</summary>
        public ProjectTemplate Template { get; set; }

        /// <summary>ID EnterpriseUser của mentor phụ trách project</summary>
        public Guid? MentorId { get; set; }

        /// <summary>
        /// Detailed description of the project goal and scope.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Planned or actual start date of the project.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Planned or actual end date of the project.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Current lifecycle status of the project.
        /// </summary>
        public ProjectStatus? Status { get; set; }

        /// <summary>
        /// Date and time when the project record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of resources (documents, links) associated with this project.
        /// </summary>
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();

        public GroupInfoDto? GroupInfo { get; set; }

        /// <summary>
        /// Configures the mapping between the Project entity and its resources to this response DTO.
        /// </summary>
        /// <param name="profile">The Automapper profile.</param>
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Project, GetProjectByIdResponse>()
                .ForMember(dest => dest.ProjectResources,
                           opt => opt.MapFrom(src => src.ProjectResources))
                .ForMember(dest => dest.GroupInfo,
                           opt => opt.MapFrom(src => src.InternshipGroup == null
                               ? null
                               : new GroupInfoDto
                               {
                                   InternshipId = src.InternshipGroup.InternshipId,
                                   GroupName = src.InternshipGroup.GroupName,
                                   MentorName = src.InternshipGroup.Mentor != null ? src.InternshipGroup.Mentor.User.FullName : null,
                                   StudentCount = src.InternshipGroup.Members.Count
                               }));
        }
    }
}
