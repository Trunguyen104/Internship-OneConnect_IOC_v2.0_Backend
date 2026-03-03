using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook
{
    public class UpdateLogbookResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public required string Summary { get; set; }
        public LogbookStatus Status { get; set; }
        public string? Plan { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, UpdateLogbook.UpdateLogbookResponse>();
        }
    }
}
