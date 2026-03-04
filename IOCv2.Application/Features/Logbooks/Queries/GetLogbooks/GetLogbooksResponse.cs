using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    /// <summary>
    /// Output model for logbook listing.
    /// </summary>
    public class GetLogbooksResponse : IMapFrom<Logbook>
    {
        /// <summary>
        /// Logbook entry ID.
        /// </summary>
        public Guid LogbookId { get; set; }
        /// <summary>
        /// Student ID who submitted.
        /// </summary>
        public Guid StudentId { get; set; }
        /// <summary>
        /// Full name of the student.
        /// </summary>
        public required String StudentName { get; set; }
        /// <summary>
        /// Date of the report.
        /// </summary>
        public DateTime DateReport { get; set; }
        /// <summary>
        /// Summary of activities.
        /// </summary>
        public required string Summary { get; set; }
        /// <summary>
        /// Description of issues.
        /// </summary>
        public string? Issue { get; set; }
        /// <summary>
        /// Submission status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Count of work items linked.
        /// </summary>
        public int TotalWorkItems { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, GetLogbooksResponse>()
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : "N/A"))
                .ForMember(dest => dest.TotalWorkItems,
                    opt => opt.MapFrom(src => src.WorkItem.Count))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
