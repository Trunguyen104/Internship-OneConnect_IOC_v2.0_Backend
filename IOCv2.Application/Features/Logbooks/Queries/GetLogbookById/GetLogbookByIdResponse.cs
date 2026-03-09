using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbookById
{
    /// <summary>
    /// Detailed response for a single logbook entry.
    /// </summary>
    public class GetLogbookByIdResponse : IMapFrom<Logbook>
    {
        /// <summary>
        /// Logbook entry ID.
        /// </summary>
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public required String StudentName { get; set; }
        /// <summary>
        /// Summary content.
        /// </summary>
        public required string Summary { get; set; }
        /// <summary>
        /// Issues content.
        /// </summary>
        public string? Issue { get; set; }
        /// <summary>
        /// Submission status.
        /// </summary>
        public LogbookStatus Status { get; set; }
        /// <summary>
        /// Plans content.
        /// </summary>
        public required string Plan { get; set; }

        public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, GetLogbookByIdResponse>()
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : "N/A"))
                .ForMember(dest => dest.WorkItems,
                    opt => opt.MapFrom(src => src.WorkItems))

                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status));
        }
    }
}
