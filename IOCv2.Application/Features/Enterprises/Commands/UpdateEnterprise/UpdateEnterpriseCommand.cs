using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public record UpdateEnterpriseCommand : IRequest<Result<UpdateEnterpriseResponse>>, IMapFrom<Enterprise>
    {
        [JsonIgnore]
        public Guid EnterpriseId { get; set; }
        public string? TaxCode { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? BackgroundUrl { get; set; }
        public string? ContactEmail { get; set; }
        public EnterpriseStatus Status { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Enterprise, UpdateEnterpriseCommand>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (EnterpriseStatus)src.Status));
            profile.CreateMap<UpdateEnterpriseCommand, Enterprise>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => (short)src.Status));
        }
    }
}
