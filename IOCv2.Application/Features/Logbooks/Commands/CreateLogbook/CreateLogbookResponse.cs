using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public Guid StudentId { get; set; }
        public required string Summary { get; set; }
        public LogbookStatus Status { get; set; }
        public string? Plan { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, CreateLogbookResponse>()
                .ForMember(dest => dest.StudentId,
                    opt => opt.MapFrom(src => src.StudentId));
        }
    }
}