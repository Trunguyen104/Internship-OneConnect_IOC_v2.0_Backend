using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Terms.Queries.GetTermById;

public class GetTermByIdResponse : IMapFrom<Term>
{
    public Guid TermId { get; set; }
    public Guid UniversityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TermDisplayStatus Status { get; set; }
    public int TotalEnrolled { get; set; }
    public int TotalPlaced { get; set; }
    public int TotalUnplaced { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Term, GetTermByIdResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                TermStatusHelper.GetComputedStatus(src.StartDate, src.EndDate, src.Status)));
    }
}