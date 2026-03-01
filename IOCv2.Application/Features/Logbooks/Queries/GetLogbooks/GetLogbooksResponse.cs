using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public class GetLogbooksResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public required String StudentName { get; set; }
        public DateTime DateReport { get; set; }
        public required string Summary { get; set; }
        public string? Issue { get; set; }
        public LogbookStatus Status { get; set; }

        public int TotalWorkItems { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, GetLogbooksResponse>()
                .ForMember(dest => dest.StudentName,
                    opt => opt.MapFrom(src => src.Student != null && src.Student.User != null ? src.Student.User.FullName : "N/A"))
                .ForMember(dest => dest.TotalWorkItems,
                    opt => opt.MapFrom(src => src.WorkItem.Count))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status));
        }
    }
}
