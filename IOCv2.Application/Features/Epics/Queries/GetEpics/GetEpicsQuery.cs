using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using MediatR;

namespace IOCv2.Application.Features.Epics.Queries.GetEpics;

public record GetEpicsQuery(
    Guid ProjectId,
    PaginationParams Pagination
) : IRequest<Result<PagedResult<GetEpicsResponse>>>;
