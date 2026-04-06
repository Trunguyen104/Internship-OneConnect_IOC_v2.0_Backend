using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase
{
    public record GetEnterpriseInterPhaseQuery : IRequest<Result<List<GetEnterpriseInterPhaseResponse>>>
    {
        public string? SearchTerm { get; init; }
    }
}
