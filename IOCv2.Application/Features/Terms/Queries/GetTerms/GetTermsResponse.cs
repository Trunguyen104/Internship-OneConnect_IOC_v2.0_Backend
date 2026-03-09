using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Terms.Queries.GetTerms;

public class GetTermsResponse : IMapFrom<Term>
{
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TermDisplayStatus Status { get; set; }
    public int TotalEnrolled { get; set; }
    public int TotalPlaced { get; set; }
    public int TotalUnplaced { get; set; }
    public DateTime CreatedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Term, GetTermsResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                TermStatusHelper.GetComputedStatus(src.StartDate, src.EndDate, src.Status)));
    }
}