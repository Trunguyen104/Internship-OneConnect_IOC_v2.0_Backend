using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Commands.DeleteViolationReport
{
    public record DeleteViolationReportCommand : IRequest<Result<DeleteViolationReportResponse>>
    {
        public Guid ViolationReportId { get; init; }
    }
}