using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Terms.Commands.CreateTerm;

public class CreateTermResponse : IMapFrom<Term>
{
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Term, CreateTermResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                TermStatusHelper.GetComputedStatus(src.StartDate, src.EndDate, src.Status)));
    }
}