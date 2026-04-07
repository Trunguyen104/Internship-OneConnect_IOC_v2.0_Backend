using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm.GetStudentsByTermDTOs
{
    public record StudentDto
    {
        public Guid StudentId { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public string ClassName { get; init; } = string.Empty;
        public string Major { get; init; } = string.Empty;
        public InternshipApplicationStatus InternshipApplicationStatus { get; init; }
        public string? EnterpriseName { get; init; }
        public string? InternPhaseName { get; init; }
        public PlacementStatus PlacementStatus { get; init; }
    }
}
