using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using System;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    /// <summary>
    /// Response model for logbook deletion.
    /// </summary>
    public class DeleteLogbookResponse : IMapFrom<Logbook>
    {
        /// <summary>
        /// ID of the deleted logbook.
        /// </summary>
        public Guid LogbookId { get; set; }

        /// <summary>
        /// Summary of the deleted logbook.
        /// </summary>
        public required string Summary { get; set; }

        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last updated timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// ID of the student who owned the logbook.
        /// </summary>
        public Guid StudentId { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, DeleteLogbookResponse>();
        }
    }
}
