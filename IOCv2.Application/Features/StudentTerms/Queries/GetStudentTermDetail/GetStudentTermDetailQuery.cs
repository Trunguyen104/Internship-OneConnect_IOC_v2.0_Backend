using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public record GetStudentTermDetailQuery(Guid StudentTermId) : IRequest<Result<GetStudentTermDetailResponse>>;
