using MediatR;
using IOCv2.Application.Common.Models;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Students.Queries.GetInternships
{
    public record GetCurrentInternshipsQuery : IRequest<Result<List<GetCurrentInternshipsResponse>>>;
}
