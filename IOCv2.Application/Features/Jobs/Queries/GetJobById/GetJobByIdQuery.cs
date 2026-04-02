using IOCv2.Application.Common.Models;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public record GetJobByIdQuery() : MediatR.IRequest<Result<GetJobByIdResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }
    }
}
