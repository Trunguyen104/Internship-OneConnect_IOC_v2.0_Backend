using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public record GetEnterpriseInterPhaseQuery : IRequest<GetEnterpriseInterPhaseResponse>
    {
        public string SearchTerm { get; init; } = null!;
    }
}
