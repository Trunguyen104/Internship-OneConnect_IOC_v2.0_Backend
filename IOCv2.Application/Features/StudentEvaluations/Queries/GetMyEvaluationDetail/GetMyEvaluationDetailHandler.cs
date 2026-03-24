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

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetMyEvaluationDetail;

public class GetMyEvaluationDetailHandler 
    : IRequestHandler<GetMyEvaluationDetailQuery, Result<GetMyEvaluationDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMyEvaluationDetailHandler> _logger;

    public GetMyEvaluationDetailHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<GetMyEvaluationDetailHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetMyEvaluationDetailResponse>> Handle(
        GetMyEvaluationDetailQuery request, CancellationToken cancellationToken)
    {
        bool isSuperAdmin = request.Role.Contains("SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // 1. Lấy thông tin Chu kỳ đánh giá kèm theo danh sách Tiêu chí
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle == null)
            return Result<GetMyEvaluationDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.NotFound), ResultErrorType.NotFound);

        Guid currentStudentId;

        // 2. Security Check: Sinh viên này có nhóm thực tập thuộc Term của Chu kỳ hay không?
        if (isSuperAdmin)
        {
            // Mock test for SuperAdmin
            var mockMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Include(m => m.InternshipGroup)
                .Where(m => m.InternshipGroup.PhaseId == cycle.PhaseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (mockMember == null) return Result<GetMyEvaluationDetailResponse>.Failure("Không có sinh viên nào trong Term này để test", ResultErrorType.Forbidden);
            currentStudentId = mockMember.StudentId;
        }
        else
        {
            currentStudentId = await _unitOfWork.Repository<Student>().Query()
                .Where(s => s.UserId == request.CurrentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentStudentId == Guid.Empty)
                return Result<GetMyEvaluationDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            var studentGroupMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Include(m => m.InternshipGroup)
                .Where(m => m.StudentId == currentStudentId && m.InternshipGroup.PhaseId == cycle.PhaseId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentGroupMember == null)
            {
                _logger.LogWarning("Access denied: User {UserId} wants to see Evaluation Detail of Cycle {CycleId} but does not belong to any valid group.", request.CurrentUserId, request.CycleId);
                return Result<GetMyEvaluationDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }
        }

        // 3. Lấy bản ghi đánh giá của chính sinh viên đó
        var evaluation = await _unitOfWork.Repository<Evaluation>().Query()
            .AsNoTracking()
            .Include(e => e.Evaluator)
            .Include(e => e.Details)
            .FirstOrDefaultAsync(e => e.CycleId == request.CycleId && e.StudentId == currentStudentId, cancellationToken);

        // 4. Khởi tạo Response
        var response = new GetMyEvaluationDetailResponse
        {
            CycleName = cycle.Name,
            CriteriaScores = new List<CriteriaScoreDto>()
        };

        // Xem điểm thật chỉ khi bài chấm (nếu có) được phép công bố (Published)
        bool isPublished = evaluation != null && evaluation.Status == EvaluationStatus.Published;

        if (isPublished)
        {
            response.EvaluationId = evaluation!.EvaluationId;
            response.EvaluatorName = evaluation.Evaluator?.FullName;
            // GradedAt lấy theo ngày cập nhật, nếu null thì là ngày tạo
            response.GradedAt = evaluation.UpdatedAt ?? evaluation.CreatedAt;
            response.TotalScore = evaluation.TotalScore;
            response.GeneralComment = evaluation.Note;
        }

        // 5. Kết hợp với Barem (Criteria) bất kể có điểm hay chưa
        // Học viên vẫn sẽ thấy được Rubric (Tên tiêu chí, điểm tối đa)
        foreach (var criteria in cycle.Criteria.OrderBy(c => c.CreatedAt)) // Tùy chọn sắp xếp theo CreatedAt hoặc SortOrder
        {
            var criteriaScore = new CriteriaScoreDto
            {
                CriteriaName = criteria.Name,
                MaxScore = criteria.MaxScore
            };

            if (isPublished)
            {
                var detail = evaluation!.Details.FirstOrDefault(d => d.CriteriaId == criteria.CriteriaId);
                if (detail != null)
                {
                    criteriaScore.Score = detail.Score;
                    criteriaScore.Comment = detail.Comment;
                }
            }

            response.CriteriaScores.Add(criteriaScore);
        }

        return Result<GetMyEvaluationDetailResponse>.Success(response);
    }
}
