using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseByHR
{
    public class GetEnterpriseByHRResponse : IMapFrom<Domain.Entities.Enterprise>
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
        public EnterpriseStatus Status { get; set; } = EnterpriseStatus.Active; // 0=Inactive, 1=Active, 2=Suspended
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Enterprise, GetEnterpriseByHRResponse>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (EnterpriseStatus)src.Status));
        }
    }
}
