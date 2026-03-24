using IOCv2.Application.Common.Models;
using System;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public record GetJobByIdQuery(Guid JobId) : MediatR.IRequest<Result<GetJobByIdResponse>>;
}
