using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentTeamEvaluations;

public class GetStudentTeamEvaluationsHandler 
    : IRequestHandler<GetStudentTeamEvaluationsQuery, Result<List<GetStudentTeamEvaluationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetStudentTeamEvaluationsHandler> _logger;

    public GetStudentTeamEvaluationsHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<GetStudentTeamEvaluationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetStudentTeamEvaluationsResponse>>> Handle(
        GetStudentTeamEvaluationsQuery request, CancellationToken cancellationToken)
    {
        bool isSuperAdmin = request.Role.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // 1. Lấy thông tin Chu kỳ đánh giá
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle == null)
            return Result<List<GetStudentTeamEvaluationsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.NotFound), ResultErrorType.NotFound);

        Guid internshipId;
        Guid currentStudentId;

        // 2. Tìm nhóm thực tập của sinh viên hiện tại trong Term của Chu kỳ này
        if (isSuperAdmin)
        {
            // Lấy đại 1 thành viên bất kỳ thuộc Term này để test mượn danh
            var mockMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Include(m => m.InternshipGroup)
                .Where(m => m.InternshipGroup.TermId == cycle.TermId)
                .FirstOrDefaultAsync(cancellationToken);

            if (mockMember == null) return Result<List<GetStudentTeamEvaluationsResponse>>.Failure("Không có sinh viên nào trong Term này để test", ResultErrorType.Forbidden);
            
            internshipId = mockMember.InternshipId;
            currentStudentId = mockMember.StudentId;
        }
        else
        {
            currentStudentId = await _unitOfWork.Repository<Student>().Query()
                .Where(s => s.UserId == request.CurrentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentStudentId == Guid.Empty)
                return Result<List<GetStudentTeamEvaluationsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            var studentGroupMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Include(m => m.InternshipGroup)
                .Where(m => m.StudentId == currentStudentId && m.InternshipGroup.TermId == cycle.TermId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentGroupMember == null)
            {
                _logger.LogWarning("Access denied: User {UserId} is not in any group for Term {TermId}", request.CurrentUserId, cycle.TermId);
                return Result<List<GetStudentTeamEvaluationsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }
            internshipId = studentGroupMember.InternshipId;
        }

        // 3. Lấy danh sách thành viên trong nhóm
        var members = await _unitOfWork.Repository<InternshipStudent>().Query()
            .AsNoTracking()
            .Include(m => m.Student)
                .ThenInclude(s => s.User)
            .Where(m => m.InternshipId == internshipId && m.Status != InternshipStatus.Failed) // Failed represents Withdrawn
            .ToListAsync(cancellationToken);

        // 4. Lấy danh sách phiếu đánh giá của nhóm trong Chu kỳ này
        var evaluations = await _unitOfWork.Repository<Evaluation>().Query()
            .AsNoTracking()
            .Where(e => e.CycleId == request.CycleId && e.InternshipId == internshipId)
            .ToListAsync(cancellationToken);

        // 5. Build DTO Response
        var responses = new List<GetStudentTeamEvaluationsResponse>();

        foreach (var member in members)
        {
            var eval = evaluations.FirstOrDefault(e => e.StudentId == member.StudentId);
            
            string evalStatus = EvaluationStatus.Pending.ToString();
            decimal? totalScore = null;

            if (cycle.Status == EvaluationCycleStatus.Pending)
            {
                // Chu kỳ Upcoming: các cột trạng thái và điểm hiển thị Pending và null
                evalStatus = EvaluationStatus.Pending.ToString();
                totalScore = null;
            }
            else
            {
                if (eval != null)
                {
                    evalStatus = eval.Status.ToString();

                    // Chỉ hiển thị điểm nếu đã Published HOẶC đây là dòng của chính sinh viên đang click vô VÀ Phiếu điểm Đã Published
                    // Theo User Story "Chỉ duy nhất chính mình nhìn thấy điểm thật nếu trạng thái Published. Các SV khác bị che"
                    if (member.StudentId == currentStudentId && eval.Status == EvaluationStatus.Published)
                    {
                        totalScore = eval.TotalScore;
                    }
                }
            }

            responses.Add(new GetStudentTeamEvaluationsResponse
            {
                StudentId = member.StudentId,
                FullName = member.Student.User.FullName,
                StudentCode = member.Student.User.UserCode,
                EvaluationStatus = evalStatus,
                TotalScore = totalScore
            });
        }

        return Result<List<GetStudentTeamEvaluationsResponse>>.Success(responses);
    }
}
