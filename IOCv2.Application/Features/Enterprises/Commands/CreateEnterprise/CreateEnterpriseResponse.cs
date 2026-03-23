using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseResponse : IMapFrom<Enterprise>
    {
        public Guid EnterpriseId { get; set; }
        public string? TaxCode { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public EnterpriseStatus Status { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Enterprise, CreateEnterpriseResponse>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (EnterpriseStatus)src.Status));
        }
    }
}
