using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    /// <summary>
    /// Response model for logbook creation.
    /// </summary>
    public class CreateLogbookResponse : IMapFrom<Logbook>
    {
        /// <summary>
        /// Unique identifier for the created logbook.
        /// </summary>
        public Guid LogbookId { get; set; }

        /// <summary>
        /// ID of the student who created the logbook.
        /// </summary>
        public Guid StudentId { get; set; }

        /// <summary>
        /// Summary of activities.
        /// </summary>
        public required string Summary { get; set; }

        /// <summary>
        /// Submission status (Punctual/Late).
        /// </summary>
        public LogbookStatus Status { get; set; }

        /// <summary>
        /// Next period plans.
        /// </summary>
        public string? Plan { get; set; }

        /// <summary>
        /// Timestamp of creation.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp of last update.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, CreateLogbookResponse>()
                .ForMember(dest => dest.StudentId,
                    opt => opt.MapFrom(src => src.StudentId));
        }
    }
}