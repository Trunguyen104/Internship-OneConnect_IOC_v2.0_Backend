using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm.GetStudentsByTermDTOs;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAssign.Queries.GetStudentsByTerm
{
    public class GetStudentsByTermHandler : IRequestHandler<GetStudentsByTermQuery, Result<PaginatedResult<GetStudentsByTermResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetStudentsByTermHandler> _logger;

        public GetStudentsByTermHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<GetStudentsByTermHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetStudentsByTermResponse>>> Handle(GetStudentsByTermQuery request, CancellationToken cancellationToken)
        {
            Guid currentUserId = Guid.Empty;
            if (!string.IsNullOrWhiteSpace(_currentUserService.UserId))
            {
                Guid.TryParse(_currentUserService.UserId, out currentUserId);
            }

            // Find term
            var term = await _unitOfWork.Repository<Term>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
            {
                return Result<PaginatedResult<GetStudentsByTermResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.NotFound),
                    ResultErrorType.NotFound);
            }

            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
            {
                return Result<PaginatedResult<GetStudentsByTermResponse>>.Failure(
                    "Bạn chỉ có thể truy cập danh sách sinh viên của trường đại học của bạn.",
                    ResultErrorType.Forbidden);
            }

            // Base query for student-term entries
            var studentTermQuery = _unitOfWork.Repository<StudentTerm>()
                .Query()
                .AsNoTracking()
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Where(st => st.TermId == request.TermId);

            var totalStudents = await studentTermQuery.CountAsync(cancellationToken);

            // Build students page with optional correlated subquery to fetch internship application status for this term/student
            var students = await studentTermQuery
                .OrderBy(st => st.Student.User.FullName)
                .Skip(((request.PageNumber ?? 1) - 1) * (request.PageSize ?? 10))
                .Take(request.PageSize ?? 10)
                .Select(st => new StudentDto
                {
                    StudentId = st.StudentId,
                    StudentName = st.Student.User.FullName,
                    ClassName = st.Student.ClassName ?? string.Empty,
                    Major = st.Student.Major ?? string.Empty,
                    PlacementStatus = _unitOfWork.Repository<StudentTerm>()
                        .Query()
                        .Where(a => a.TermId == request.TermId && a.StudentId == st.StudentId)
                        .Select(a => a.PlacementStatus)
                        .FirstOrDefault(),
                    EnterpriseName = _unitOfWork.Repository<StudentTerm>()
                        .Query()
                        .Include(x => x.Enterprise)
                        .Where(a => a.TermId == request.TermId && a.StudentId == st.StudentId)
                        .Select(a => a.Enterprise!.Name)
                        .FirstOrDefault(),
                    InternPhaseName = _unitOfWork.Repository<InternshipPhase>().Query()
                        .Where(p => p.PhaseId == _unitOfWork.Repository<InternshipApplication>().Query().Where(ia => ia.TermId == request.TermId && ia.StudentId == st.StudentId).Select(ia => ia.InternPhaseId).FirstOrDefault())
                        .Select(p => p.Name)
                        .FirstOrDefault(),
                    // correlated subquery: get the application status for this student in the requested term (or default 0 if none)
                    InternshipApplicationStatus = _unitOfWork.Repository<InternshipApplication>()
                        .Query()
                        .Where(a => a.TermId == request.TermId && a.StudentId == st.StudentId)
                        .Select(a => a.Status)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            // Create a single response containing term info and the current page of students
            var response = new GetStudentsByTermResponse
            {
                TermId = term.TermId,
                TermName = term.Name,
                Students = students
            };

            // Wrap the single response into a PaginatedResult so caller can be informed about total student count / paging
            // Note: Items contains one item (the response), while TotalCount represents total students for the term.
            var paginated = new PaginatedResult<GetStudentsByTermResponse>(new List<GetStudentsByTermResponse> { response }, totalStudents, request.PageNumber ?? 1, request.PageSize ?? 10);

            return Result<PaginatedResult<GetStudentsByTermResponse>>.Success(paginated);
        }
    }
}
