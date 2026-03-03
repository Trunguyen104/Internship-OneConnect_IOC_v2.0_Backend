using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using System;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    public class DeleteLogbookResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public required string Summary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid StudentId { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, DeleteLogbookResponse>();
        }
    }
}
