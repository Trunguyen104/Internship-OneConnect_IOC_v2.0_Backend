/*
Pseudocode:
1. Identify the NotFound return in Handle method.
2. Replace the empty NotFound() call with NotFound(message) where message is retrieved via:
   _messageService.GetMessage(MessageKeys.ViolationReportKey.NotFound)
3. Keep other logic unchanged.
*/

using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports;
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

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail
{
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

        public async Task<Result<GetViolationReportDetailResponse>> Handle(
    GetViolationReportDetailQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<ViolationReport>()
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.ViolationReportId == request.ViolationReportId);

                // Role filter
                if (ViolationReportParam.MentorRole.Equals(_currentUserService.Role!))
                {
                    var userId = Guid.Parse(_currentUserService.UserId!);

                    query = query.Where(x =>
                        x.InternshipGroup.Mentor!.UserId == userId);
                }

                var response = await query
                    .ProjectTo<GetViolationReportDetailResponse>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);

                if (response == null)
                    return Result<GetViolationReportDetailResponse>.NotFound(
                        _messageService.GetMessage(MessageKeys.ViolationReportKey.NotFound));

                return Result<GetViolationReportDetailResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting violation report detail");

                return Result<GetViolationReportDetailResponse>.Failure(
                    ex.Message,
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
