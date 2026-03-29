using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Features.Jobs.Queries.GetJobById.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public class GetJobByIdHandler : MediatR.IRequestHandler<GetJobByIdQuery, Result<GetJobByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetJobByIdHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetJobByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetJobByIdHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetJobByIdResponse>> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving job by ID: {JobId}", request.JobId);

            var job = await _unitOfWork.Repository<Job>()
                .Query().IgnoreQueryFilters()
                .AsNoTracking()
                .Include(j => j.Enterprise)
                .Include(j => j.Universities)
                .Include(j => j.InternshipApplications)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", request.JobId);
                return Result<GetJobByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.RecordNotFound),
                    ResultErrorType.NotFound);
            }

            // If caller is an enterprise role, ensure they belong to the enterprise that owns the job
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                var userId = Guid.Parse(_currentUserService.UserId!);
                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null || entUser.EnterpriseId != job.EnterpriseId)
                {
                    _logger.LogWarning("HR user is not associated with enterprise for job {JobId}", request.JobId);
                    return Result<GetJobByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            else
            {
                // For university / student users only allow viewing published jobs that they can see
                var userId = Guid.Parse(_currentUserService.UserId!);
                var uniUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

                if (uniUser == null)
                {
                    _logger.LogWarning("Non-enterprise user attempted to access job details: {JobId}", request.JobId);
                    return Result<GetJobByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                var visible = job.Status == JobStatus.PUBLISHED &&
                              (job.Audience == JobAudience.Public || job.Universities.Any(u => u.UniversityId == uniUser.UniversityId));

                if (!visible)
                {
                    _logger.LogWarning("Job {JobId} is not visible to university user {UserId}", request.JobId, userId);
                    return Result<GetJobByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }

            // Map entity -> response
            var response = _mapper.Map<GetJobByIdResponse>(job);

            // Compute application counts grouped by status
            response.ApplicationStatusCounts = job.InternshipApplications
                .GroupBy(a => a.Status)
                .Select(g => new ApplicationStatusCountDto
                {
                    Status = (short)g.Key,
                    StatusName = g.Key.ToString(),
                    Count = g.Count()
                })
                .OrderBy(x => x.Status)
                .ToList();

            // AC-11: compute Placed count and set banner when Placed == Quantity
            var placedCount = job.InternshipApplications.Count(a => a.Status == InternshipApplicationStatus.Placed);
            response.PlacedCount = placedCount;

            if (job.Quantity.HasValue && placedCount == job.Quantity.Value)
            {
                // localized banner: "Số lượng sinh viên đã Placed đã đạt tối đa [N]/[N]..."
                response.FilledBanner = _messageService.GetMessage("Job.Banner.Filled", placedCount, job.Quantity.Value);
            }

            return Result<GetJobByIdResponse>.Success(response);
        }
    }
}
