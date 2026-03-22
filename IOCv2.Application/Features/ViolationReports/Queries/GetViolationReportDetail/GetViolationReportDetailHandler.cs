using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail
{
    /// <summary>
    /// Handler to retrieve a single violation report detail by id.
    /// Applies Mentor scoping to ensure mentors can only access reports for their groups.
    /// Projects the EF query to GetViolationReportDetailResponse DTO using AutoMapper.
    /// </summary>
    public class GetViolationReportDetailHandler : IRequestHandler<GetViolationReportDetailQuery, Result<GetViolationReportDetailResponse>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetViolationReportDetailHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetViolationReportDetailHandler(IMapper mapper, IUnitOfWork unitOfWork, IMessageService messageService, ILogger<GetViolationReportDetailHandler> logger, ICurrentUserService currentUserService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Handle retrieval:
        /// - Build base query and apply Mentor role filter
        /// - Project to DTO and return NotFound if missing
        /// - Catch exceptions and log an internal server error
        /// </summary>
        public async Task<Result<GetViolationReportDetailResponse>> Handle(GetViolationReportDetailQuery request, CancellationToken cancellationToken)
        {
            // Base query for the requested violation report id.
            var query = _unitOfWork.Repository<ViolationReport>().Query().AsNoTracking().Where(x => x.ViolationReportId == request.ViolationReportId);

            // If current user is Mentor, restrict to reports belonging to mentor's groups.
            if (UserRole.Mentor.ToString().Equals(_currentUserService.Role!))
            {
                var userId = Guid.Parse(_currentUserService.UserId!);
                query = query.Where(x => x.InternshipGroup.Mentor!.UserId == userId);
            }

            // Project to DTO and execute.
            var response = await query.ProjectTo<GetViolationReportDetailResponse>(_mapper.ConfigurationProvider).FirstOrDefaultAsync(cancellationToken);

            if (response == null)
                return Result<GetViolationReportDetailResponse>.NotFound(_messageService.GetMessage(MessageKeys.ViolationReportKey.NotFound));

            return Result<GetViolationReportDetailResponse>.Success(response);
        }
    }
}
