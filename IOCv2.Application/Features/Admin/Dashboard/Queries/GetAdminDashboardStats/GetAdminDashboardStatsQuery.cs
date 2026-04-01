using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;

public record GetAdminDashboardStatsQuery : IRequest<Result<AdminDashboardStatsResponse>>;
