using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail
{
    public record GetViolationReportDetailQuery : IRequest<Result<GetViolationReportDetailResponse>>
    {
        public Guid ViolationReportId { get; set; }
    }
}
