using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Logbooks.Commands.CreateLogbook
{
    public class CreateLogbookResponse : IMapFrom<Logbook>
    {
        public int LogbookId { get; set; }
        public int InternshipId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public LogbookStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, CreateLogbookResponse>()
                   .ForMember(dest => dest.Status,
                              opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}