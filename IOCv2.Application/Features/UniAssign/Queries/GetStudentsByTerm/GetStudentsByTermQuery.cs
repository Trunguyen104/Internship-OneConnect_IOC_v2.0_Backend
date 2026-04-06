using IOCv2.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm
{
    public record GetStudentsByTermQuery : MediatR.IRequest<Result<PaginatedResult<GetStudentsByTermResponse>>>
    {
        [JsonIgnore]
        public Guid TermId { get; init; }
        public int? PageNumber { get; init; } = 1;
        public int? PageSize { get; init; } = 10;
    }
}
