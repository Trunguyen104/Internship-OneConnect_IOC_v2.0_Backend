using System;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.Students.Queries.GetInternshipDetail
{
    public class GetInternshipDetailResponse : IMapFrom<Term>
    {
        public Guid TermId { get; set; }
        public string TermName { get; set; } = string.Empty;

        /// <summary>
        /// Represented as string, e.g. "Upcoming", "Active", "Closed"
        /// </summary>
        public string TermStatus { get; set; } = string.Empty;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public int? DaysUntilStart { get; set; }
        public int? DaysUntilEnd { get; set; }

        public bool IsPlaced { get; set; }

        public string PlacementStatus => IsPlaced ? "Placed" : "Unplaced";
        public string PlacementBadge { get; set; } = string.Empty;
        public string? PlacementMessage { get; set; }

        public string EnterpriseName { get; set; } = "Chưa có";

        /// <summary>
        /// Student's enrollment status as string: "Enrolled", "Withdrawn"
        /// </summary>
        public string EnrollmentStatus { get; set; } = string.Empty;

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Term, GetInternshipDetailResponse>();
        }
    }
}
