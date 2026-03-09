
using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseResponse : IMapFrom<Enterprise>
    {
        public Guid EnterpriseId { get; set; }
        public string? TaxCode { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? BackgroundUrl { get; set; }
        public bool IsVerified { get; set; } = false;
        public EnterpriseStatus Status { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Enterprise, UpdateEnterpriseResponse>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (EnterpriseStatus)src.Status));
            profile.CreateMap<UpdateEnterpriseResponse, Enterprise>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (short)src.Status));
        }
    }
}
