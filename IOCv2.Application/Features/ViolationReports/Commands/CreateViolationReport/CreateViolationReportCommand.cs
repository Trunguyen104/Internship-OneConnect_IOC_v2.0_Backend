using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public record CreateViolationReportCommand : IRequest<Result<CreateViolationReportResponse>>
    {
        public Guid StudentId { get; init; }
        public DateTime OccurredDate { get; init; }
        public string Description { get; init; } = string.Empty;
        public IList<IFormFile>? Attachments { get; init; }
        public ViolationType Type { get; init; }
        public ViolationSeverity Severity { get; init; }
        public ViolationStatus Status { get; init; } = ViolationStatus.Pending;
    }
}
