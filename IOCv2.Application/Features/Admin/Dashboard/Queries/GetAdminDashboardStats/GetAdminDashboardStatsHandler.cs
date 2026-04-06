using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;

public class GetAdminDashboardStatsHandler
    : IRequestHandler<GetAdminDashboardStatsQuery, Result<AdminDashboardStatsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminDashboardStatsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AdminDashboardStatsResponse>> Handle(
        GetAdminDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Await each query sequentially to avoid EF Core DbContext concurrency issues
        var totalUsers = await _unitOfWork.Repository<User>().CountAsync(u => u.DeletedAt == null, cancellationToken);
        var totalUniversities = await _unitOfWork.Repository<University>().CountAsync(u => u.DeletedAt == null, cancellationToken);
        var totalEnterprises = await _unitOfWork.Repository<Enterprise>().CountAsync(e => e.DeletedAt == null, cancellationToken);
        var totalJobs = await _unitOfWork.Repository<Job>().CountAsync(j => j.DeletedAt == null, cancellationToken);
        var totalStudents = await _unitOfWork.Repository<Student>().CountAsync(s => s.DeletedAt == null, cancellationToken);
        
        var activeInternships = await _unitOfWork.Repository<Student>().CountAsync(
            s => s.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS && s.DeletedAt == null, cancellationToken);
        
        var activeTerms = await _unitOfWork.Repository<Term>().CountAsync(
            t => t.Status == TermStatus.Open && t.DeletedAt == null, cancellationToken);

        // Calculate Pending Applications (Applied or Pending Univ/Enterprise assignment)
        var pendingApplications = await _unitOfWork.Repository<InternshipApplication>().CountAsync(
            a => (a.Status == InternshipApplicationStatus.Applied || a.Status == InternshipApplicationStatus.PendingAssignment) 
                 && a.DeletedAt == null, cancellationToken);

        // 2. Recent audit trail (actor + human-readable summary)
        var auditLogs = await _unitOfWork.Repository<AuditLog>()
            .Query()
            .Include(a => a.PerformedBy)
            .OrderByDescending(a => a.CreatedAt)
            .Take(15)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var recentActivities = auditLogs.Select(AuditActivityMapper.Map).ToList();

        // 3. System Health Metrics (Dynamic checks)
        // If we reached here, DB queries succeeded, so DB is connected
        bool dbConnected = true; 
        
        var systemHealth = new List<SystemHealthDto>
        {
            new SystemHealthDto { 
                Label = "Database Status", 
                Value = dbConnected ? "Online" : "Offline", 
                Status = dbConnected ? "good" : "critical" 
            },
            new SystemHealthDto { 
                Label = "API Infrastructure", 
                Value = "Healthy", // If this code is running, the API is healthy
                Status = "good" 
            },
            new SystemHealthDto { 
                Label = "Audit Logs Sync", 
                Value = auditLogs.Any() ? "Active" : "No Logs", 
                Status = auditLogs.Any() ? "good" : "warning" 
            }
        };

        return Result<AdminDashboardStatsResponse>.Success(new AdminDashboardStatsResponse
        {
            TotalUsers          = totalUsers,
            TotalUniversities   = totalUniversities,
            TotalEnterprises    = totalEnterprises,
            TotalJobs           = totalJobs,
            TotalStudents       = totalStudents,
            ActiveInternships   = activeInternships,
            ActiveTerms         = activeTerms,
            PendingApplications = pendingApplications,
            RecentActivities    = recentActivities,
            SystemHealth        = systemHealth
        });
    }
}
