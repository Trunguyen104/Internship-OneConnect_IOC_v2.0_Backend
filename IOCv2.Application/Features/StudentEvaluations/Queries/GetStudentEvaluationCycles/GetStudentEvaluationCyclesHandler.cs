using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentEvaluationCycles;

public class GetStudentEvaluationCyclesHandler
    : IRequestHandler<GetStudentEvaluationCyclesQuery, Result<List<GetStudentEvaluationCyclesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetStudentEvaluationCyclesHandler> _logger;

    public GetStudentEvaluationCyclesHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<GetStudentEvaluationCyclesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetStudentEvaluationCyclesResponse>>> Handle(
        GetStudentEvaluationCyclesQuery request, CancellationToken cancellationToken)
    {
        bool isSuperAdmin = request.Role.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // 1. Kiểm tra quyền sở hữu (ownership): Sinh viên có thuộc nhóm thực tập này không?
        if (!isSuperAdmin)
        {
            var studentId = await _unitOfWork.Repository<Student>().Query()
                .Where(s => s.UserId == request.CurrentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                return Result<List<GetStudentEvaluationCyclesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }

            var isMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AnyAsync(m => m.InternshipId == request.InternshipId && m.StudentId == studentId, cancellationToken);

            if (!isMember)
            {
                _logger.LogWarning("Access denied for Student {StudentId} to InternshipGroup {InternshipId}", studentId, request.InternshipId);
                return Result<List<GetStudentEvaluationCyclesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }
        }

        // 2. Lấy PhaseId của nhóm thực tập
        var phaseId = await _unitOfWork.Repository<InternshipGroup>().Query()
            .Where(ig => ig.InternshipId == request.InternshipId)
            .Select(ig => ig.PhaseId)
            .FirstOrDefaultAsync(cancellationToken);

        if (phaseId == Guid.Empty)
        {
            return Result<List<GetStudentEvaluationCyclesResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.NotFound),
                ResultErrorType.NotFound);
        }

        // 3. Lấy ra danh sách các đợt đánh giá ứng với Đợt thực tập đó
        var cycles = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .Where(c => c.PhaseId == phaseId)
            .OrderBy(c => c.StartDate)
            .ProjectTo<GetStudentEvaluationCyclesResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Result<List<GetStudentEvaluationCyclesResponse>>.Success(cycles);
    }
}
