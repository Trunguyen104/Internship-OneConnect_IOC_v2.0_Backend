using MediatR;
using IOCv2.Application.Common.Models;
using System;

namespace IOCv2.Application.Features.Students.Queries.GetInternshipDetail
{
    public record GetInternshipDetailQuery(Guid TermId = default) : IRequest<Result<GetInternshipDetailResponse>>;
}
