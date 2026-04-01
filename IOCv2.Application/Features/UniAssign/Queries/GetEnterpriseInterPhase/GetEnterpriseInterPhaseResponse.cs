using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public record GetEnterpriseInterPhaseResponse
    {
        public string EnterpriseName { get; init; } = null!;
        public string InternPhaseName { get; init; } = null!;
        public string MajorFields { get; init; } = null!;
        public string RemainingCapacity { get; init; } = null!;
    }
}
