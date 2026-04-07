using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public record GetEnterpriseInterPhaseResponse : IMapFrom<InternshipPhase>
    {
        public string EnterpriseName { get; init; } = null!;
        public Guid EnterpriseId { get; init; }
        public string InternPhaseName { get; init; } = null!;
        public Guid InternPhaseId { get; init; }
        public string MajorFields { get; init; } = null!;
        public int Capacity { get; init; }
        public int RemainingCapacity { get; init; }

        public void Profile(Profile profile)
        {
            profile.CreateMap<InternshipPhase, GetEnterpriseInterPhaseResponse>()
                .ForMember(dest => dest.EnterpriseName, opt => opt.MapFrom(src => src.Enterprise!.Name))
                .ForMember(dest => dest.EnterpriseId, opt => opt.MapFrom(src => src.EnterpriseId))
                .ForMember(dest => dest.InternPhaseName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.InternPhaseId, opt => opt.MapFrom(src => src.PhaseId))
                .ForMember(dest => dest.MajorFields, opt => opt.MapFrom(src => src.MajorFields));
        }
    }
}
