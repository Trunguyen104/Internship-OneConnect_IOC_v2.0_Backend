using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    public class GetViolationReportsHandler : IRequestHandler<GetViolationReportsQuery, Result<PaginatedResult<GetViolationReportsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetViolationReportsHandler> _logger;
        private readonly IMessageService _messageService;
     
        public GetViolationReportsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper, ILogger<GetViolationReportsHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<PaginatedResult<GetViolationReportsResponse>>> Handle(GetViolationReportsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Get Violation Reports
                var query = _unitOfWork.Repository<ViolationReport>().Query()
                    .Include(x => x.Student).ThenInclude(x => x.User)
                    .Include(x => x.InternshipGroup).Select(x => x).AsNoTracking();
                // If EnterpriseInternalViewer can only view reports related to their own business.
                if (ViolationReportParam.MentorRole.Equals(_currentUserService.Role))
                {
                    query = query.Where(x => x.InternshipGroup.Mentor!.UserId == Guid.Parse(_currentUserService.UserId!));
                }
                if (query == null) return Result<PaginatedResult<GetViolationReportsResponse>>.NotFound(_messageService.GetMessage(MessageKeys.ViolationReportKey.NotFound));

                // Apply Search (tên sinh viên hoặc MSSV)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    query = query.Where(x => x.Student.User.FullName.ToLower().Contains(term) || x.Student.User.UserCode.ToLower().Contains(term));
                }

                // Apply Filters
                if (request.ViolationType.HasValue)
                    query = query.Where(x => x.Type == request.ViolationType);

                if (request.SeverityLevel.HasValue)
                    query = query.Where(x => x.Severity == request.SeverityLevel);

                if (request.ProcessingStatus.HasValue)
                    query = query.Where(x => x.Status == request.ProcessingStatus);

                if (request.CreatedById.HasValue)
                    query = query.Where(x => x.CreatedBy == request.CreatedById.Value);

                if (request.OccurredFrom.HasValue)
                    query = query.Where(x => x.OccurredDate >= request.OccurredFrom.Value);

                if (request.OccurredTo.HasValue)
                    query = query.Where(x => x.OccurredDate <= request.OccurredTo.Value);

                if (request.GroupId.HasValue)
                    query = query.Where(x => x.InternshipGroupId == request.GroupId.Value);

                // Total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);
                if (totalCount == 0) return Result<PaginatedResult<GetViolationReportsResponse>>.NotFound(_messageService.GetMessage(MessageKeys.ViolationReportKey.FilteredNotFound));

                // Apply Pagination
                var items = await query
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize).ProjectTo<GetViolationReportsResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var response = PaginatedResult<GetViolationReportsResponse>.Create(
                    items,
                    totalCount,
                    request.PageNumber,
                    request.PageSize
                );

                return Result<PaginatedResult<GetViolationReportsResponse>>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ViolationReportKey.GetViolationReportsError));
                return Result<PaginatedResult<GetViolationReportsResponse>>.Failure(MessageKeys.ViolationReportKey.GetViolationReportsError, ResultErrorType.InternalServerError);
            }
        }
    }
}
