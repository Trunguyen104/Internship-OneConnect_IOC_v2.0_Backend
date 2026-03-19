using System;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public record CreateViolationReportResponse
    {
        public Guid ViolationReportId { get; init; }
        public Guid StudentId { get; init; }
        public DateOnly OccurredDate { get; init; }
        public string Description { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string CreatedBy { get; init; } = string.Empty;
        public string GroupName { get; init; } = string.Empty;

    }
}
