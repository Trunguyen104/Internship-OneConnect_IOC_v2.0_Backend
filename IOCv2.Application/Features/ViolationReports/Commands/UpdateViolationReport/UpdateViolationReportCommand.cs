using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.UpdateViolationReport
{
    public record UpdateViolationReportCommand : IRequest<Result<UpdateViolationReportResponse>>
    {
        [JsonIgnore]
        public Guid ViolationReportId { get; set; }
        public Guid StudentId { get; set; }
        public DateOnly OccurredDate { get; set; }
        public string Description { get; set; } = string.Empty!;
        public DateTime? LastUpdate { get; set; } // for concurrency detection; client should send the last known UpdatedAt timestamp of the record
        public bool ForceUpdate { get; set; } = false; // for concurrency override
    }
}
