using AutoMapper;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook
{
    public class DeleteLogbookResponse
    {
        public Guid LogbookId { get; set; }
        public Guid StudentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Logbook, DeleteLogbookResponse>()
                .ForMember(dest => dest.StudentId,
                    opt => opt.MapFrom(src => src.StudentId));
        }
    }
}
