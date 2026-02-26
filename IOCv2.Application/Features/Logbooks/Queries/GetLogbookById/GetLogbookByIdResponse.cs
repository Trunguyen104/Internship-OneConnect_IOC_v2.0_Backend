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
    public class GetLogbookByIdResponse : IMapFrom<Logbook>
    {
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public String StudentName { get; set; }
        public required string Content { get; set; }
        public string? Issue { get; set; }
        public LogbookStatus Status { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, GetLogbookByIdResponse>()
                   .ForMember(dest => dest.Status,
                              opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
