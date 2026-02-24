using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public class GetLogbooksResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public string Content { get; set; }
        public string? Issue { get; set; }
        public LogbookStatus Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, GetLogbooksResponse>()
                   .ForMember(dest => dest.Status,
                              opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
