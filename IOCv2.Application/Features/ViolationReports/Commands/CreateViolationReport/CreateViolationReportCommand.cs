using IOCv2.Application.Common.Models;
using MediatR;
using System;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport
{
    public record CreateViolationReportCommand : IRequest<Result<CreateViolationReportResponse>>
    {
        public Guid StudentId { get; init; }
        public DateOnly OccurredDate { get; init; }
        public string Description { get; init; } = string.Empty;
    }
}