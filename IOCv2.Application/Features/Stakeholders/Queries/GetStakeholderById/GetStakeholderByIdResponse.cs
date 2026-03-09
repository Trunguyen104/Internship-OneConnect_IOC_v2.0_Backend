using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    public class GetStakeholderByIdResponse : IMapFrom<Stakeholder>
    {
        public Guid Id { get; set; }
        public Guid InternshipId { get; set; }
        public string Name { get; set; } = string.Empty;
        public StakeholderType Type { get; set; }
        public string? Role { get; set; }
        public string? Description { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Stakeholder, GetStakeholderByIdResponse>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
        }
    }
}

