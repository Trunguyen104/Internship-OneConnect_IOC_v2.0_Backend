using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplicationDetail;

public record GetMyApplicationDetailQuery(Guid ApplicationId) : IRequest<Result<GetMyApplicationDetailResponse>>;
