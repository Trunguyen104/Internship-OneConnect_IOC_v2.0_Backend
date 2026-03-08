using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Evaluations.Queries.GetInternshipEvaluations;

public class GetInternshipEvaluationsHandler : IRequestHandler<GetInternshipEvaluationsQuery, Result<GetInternshipEvaluationsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public GetInternshipEvaluationsHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<GetInternshipEvaluationsResponse>> Handle(GetInternshipEvaluationsQuery request, CancellationToken cancellationToken)
    {
        // 1. Kiểm tra chu kỳ
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);
        if (cycle == null)
            return Result<GetInternshipEvaluationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.CycleNotFound),
                ResultErrorType.NotFound);

        // 2. Lấy tiêu chí của chu kỳ
        var criteriaList = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .Where(c => c.CycleId == request.CycleId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Lấy thông tin nhóm thực tập và danh sách sinh viên
        var internship = await _unitOfWork.Repository<InternshipGroup>().Query()
            .Include(i => i.Members)
            .ThenInclude(m => m.Student)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

        if (internship == null)
            return Result<GetInternshipEvaluationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationKey.InternshipNotFound),
                ResultErrorType.NotFound);

        // 4. Lấy danh sách điểm đã chấm (Draft, Submitted, Published)
        var evaluations = await _unitOfWork.Repository<Evaluation>().Query()
            .Include(e => e.Details)
            .Where(e => e.CycleId == request.CycleId && e.InternshipId == request.InternshipId)
            .ToListAsync(cancellationToken);

        // 5. Build Response
        var response = new GetInternshipEvaluationsResponse
        {
            CycleId = request.CycleId,
            InternshipId = request.InternshipId,
            Criteria = criteriaList.Select(c => new CriteriaDto
            {
                CriteriaId = c.CriteriaId,
                Name = c.Name,
                MaxScore = c.MaxScore,
                Weight = c.Weight
            }).ToList(),
            Students = new List<StudentEvaluationDto>()
        };

        // 5.1. Dữ liệu điểm của Group (nếu có - StudentId = null)
        // Hiện tại luồng này ưu tiên hiển thị sinh viên, nên có thể thiết kế tuỳ chọn.
        // Tạm thời loop qua tất cả thành viên trong nhóm.
        foreach (var member in internship.Members)
        {
            var studentEval = evaluations.FirstOrDefault(e => e.StudentId == member.StudentId);
            
            var studentDto = new StudentEvaluationDto
            {
                StudentId = member.StudentId,
                StudentCode = member.Student?.User?.UserCode ?? string.Empty,
                FullName = member.Student?.User?.FullName ?? string.Empty,
                IsEvaluated = studentEval != null,
                Status = studentEval?.Status.ToString(), // Nếu null sẽ hiển thị Pending bên frontend
                TotalScore = studentEval?.TotalScore,
                Note = studentEval?.Note,
                Details = new List<EvaluationDetailDto>()
            };

            if (studentEval != null)
            {
                studentDto.Details = studentEval.Details.Select(d => new EvaluationDetailDto
                {
                    CriteriaId = d.CriteriaId,
                    Score = d.Score,
                    Comment = d.Comment
                }).ToList();
            }

            response.Students.Add(studentDto);
        }

        // Sap xep danh sach sinh vien theo Ten (hoac MSSV)
        response.Students = response.Students.OrderBy(s => s.FullName).ToList();

        return Result<GetInternshipEvaluationsResponse>.Success(response);
    }
}
