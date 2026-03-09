using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    /// <summary>
    /// Response model for logbook update.
    /// </summary>
    public class UpdateLogbookResponse : IMapFrom<Logbook>
    {
        /// <summary>
        /// Unique identifier for the updated logbook.
        /// </summary>
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }


        /// <summary>
        /// Updated summary.
        /// </summary>
        public required string Summary { get; set; }

        /// <summary>
        /// Submission status.
        /// </summary>
        public LogbookStatus Status { get; set; }


        /// <summary>
        /// Updated plans.
        /// </summary>
        public string? Plan { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, UpdateLogbook.UpdateLogbookResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        }
    }
}
