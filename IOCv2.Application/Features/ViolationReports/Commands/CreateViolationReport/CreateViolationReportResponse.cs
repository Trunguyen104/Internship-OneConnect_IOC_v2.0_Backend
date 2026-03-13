using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail;
using IOCv2.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public record CreateViolationReportResponse
    {
        public Guid ViolationReportId { get; init; }
        public Guid StudentId { get; init; }
        public string StudentName { get; init; } = string.Empty;
        public Guid InternshipGroupId { get; init; }
        public DateTime OccurredDate { get; init; }
        public string Description { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Severity { get; init; } = string.Empty;
        public IList<ViolationAttachmentDto> Attachments { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public string CreatedByName { get; init; } = string.Empty;
    }
    public record ViolationAttachmentDto
    {
        public Guid AttachmentId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FileUrl { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
    }
}
